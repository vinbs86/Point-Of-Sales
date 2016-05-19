﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Point_Of_Sales
{
    public partial class FormPOS : Form
    {
        clsFunctions sFunctions = new clsFunctions();

        public static FormPOS publicFormPOS;

        private static FormPOS sForm = null;
        public static FormPOS Instance()
        {
            if (sForm == null) { sForm = new FormPOS(); }

            return sForm;
        }

        MySqlDataAdapter daFormPOSList = new MySqlDataAdapter();
        MySqlCommand cmdAddInvoice;
        DataSet dsFormPOSList = new DataSet();

        int PosX, PosY;
        ListViewItem li;

        public FormPOS()
        {
            InitializeComponent();
        }

        private void FormPOS_Load(object sender, EventArgs e)
        {
            daFormPOSList = new MySqlDataAdapter("", clsConnection.CN);

            cmdAddInvoice = new MySqlCommand("INSERT INTO tblsales( invoiceno , productautoid, unitprice, quantity, subtotal, cash, changecash, dateadded, totalamount)" +
                                               "VALUES (@getInvoice,@getProductID,@getUnitPrice,@getQuantity,@getSubTotal,@getCash,@getChange,@getDateAdded,@getTotalAmount)", clsConnection.CN);

            NewInvoice();

            cmdAddInvoice.Parameters.Add("@getInvoice", MySqlDbType.VarChar);
            cmdAddInvoice.Parameters.Add("@getProductID", MySqlDbType.Int16);
            cmdAddInvoice.Parameters.Add("@getUnitPrice", MySqlDbType.Decimal);
            cmdAddInvoice.Parameters.Add("@getQuantity", MySqlDbType.Int16);
            cmdAddInvoice.Parameters.Add("@getSubTotal", MySqlDbType.Decimal);
            cmdAddInvoice.Parameters.Add("@getCash", MySqlDbType.Decimal);
            cmdAddInvoice.Parameters.Add("@getChange", MySqlDbType.Decimal);
            cmdAddInvoice.Parameters.Add("@getDateAdded", MySqlDbType.Date);
            cmdAddInvoice.Parameters.Add("@getTotalAmount", MySqlDbType.Decimal);

            publicFormPOS = this;
        }

        void NewInvoice()
        {     
            GenerateInvoice();
        }

        void GenerateInvoice()
        {
            lblInvoice.Text = "INV-" + clsFunctions.GenerateCD("SELECT MAX(autoid) FROM tblsales", "tblsales") + "/" + DateTime.Now.Millisecond.ToString();
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            NewInvoice();
            Reset();
            lvPOS.Items.Clear();
        }


        private void txtProductCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            SetProductPOS("SELECT productcode, productname, unitprice, sellingprice, stock, autoid FROM tblproduct WHERE productcode='" + txtProductCode.Text + "'");
        }

        private void SetProductPOS(string sSQL)
        {
            try
            {
                long totalRow = 0;

                daFormPOSList.SelectCommand.CommandText = sSQL;

                dsFormPOSList.Clear();
                daFormPOSList.Fill(dsFormPOSList, "tblproduct");

                totalRow = dsFormPOSList.Tables["tblproduct"].Rows.Count - 1;

                txtProductName.Text = dsFormPOSList.Tables["tblproduct"].Rows[0].ItemArray.GetValue(1).ToString();
                txtPrice.Text = dsFormPOSList.Tables["tblproduct"].Rows[0].ItemArray.GetValue(2).ToString();
                txtQTY.Text = "1";
                txtProductID.Text = dsFormPOSList.Tables["tblproduct"].Rows[0].ItemArray.GetValue(5).ToString();

                txtSubTotal.Text = (decimal.Parse(txtQTY.Text) * decimal.Parse(txtPrice.Text)).ToString();

                txtQTY.Focus();
                

            }
            catch (Exception ex) {}
        }

        void Reset()
        {
            txtProductCode.Focus();

            txtProductCode.Text = string.Empty;
            txtProductName.Text = string.Empty;
            txtPrice.Text = string.Empty;
            txtQTY.Text = string.Empty;
            txtSubTotal.Text = string.Empty;
            txtProductID.Text = string.Empty;


        }

        private void AddProductList()
        {

            if (lvPOS.FindItemWithText(txtProductCode.Text) != null)
            {
                MessageBox.Show("Produk yang anda pilih sudah tercantum.", clsVariables.sMSGBOX, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Reset();
                return;
            }

            ListViewItem item = new ListViewItem(txtProductCode.Text, 1);
            item.SubItems.Add(txtProductName.Text);
            item.SubItems.Add(txtPrice.Text);
            item.SubItems.Add(txtQTY.Text);
            item.SubItems.Add(txtSubTotal.Text);
            item.SubItems.Add(txtProductID.Text);

            lvPOS.Items.Add(item);

            Reset();

            SumTotalAmount();

            if (decimal.Parse(lblCash.Text) > 0)
            {
                SumCashFinish(lblCash.Text);
            }

        }

        void SumTotalAmount()
        {
            decimal sTotalAmount = 0;

            for (int x=0; x < lvPOS.Items.Count;x++)
            {
                sTotalAmount += decimal.Parse(lvPOS.Items[x].SubItems[4].Text);
            }

            lblTotalAmount.Text = sTotalAmount.ToString();
            lblTotal.Text = lblTotalAmount.Text;
        }

        private void txtQTY_TextChanged(object sender, EventArgs e)
        {
            if(txtQTY.Text != string.Empty)
                txtSubTotal.Text = (decimal.Parse(txtQTY.Text) * decimal.Parse(txtPrice.Text)).ToString();
        }

        private void txtQTY_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddProductList();
                
            }
               
        }

        public void SumCashFinish(string cash)
        {
            lblCash.Text = cash;

            lblChange.Text = (decimal.Parse(cash) - decimal.Parse(lblTotalAmount.Text)).ToString();

        }

        private void lvPOS_MouseUp(object sender, MouseEventArgs e)
        {
            if (lvPOS.Items.Count != 0)
            {
                if (li == null)
                    return;

                int subItemSelected = 3;
                int nStart = PosX;
                int spos = 0;
                int epos = lvPOS.Columns[1].Width;
                for (int i = 0; i < lvPOS.Columns.Count; i++)
                {
                    if (nStart > spos && nStart < epos)
                    {
                        subItemSelected = i;
                        break;
                    }

                    spos = epos;
                    epos += lvPOS.Columns[i].Width;
                }

                string value = li.SubItems[3].Text;

                if (InputBox("[ " + li.SubItems[0].Text + " ] " + li.SubItems[1].Text, "QTY [ " + li.SubItems[1].Text + " ] :", ref value) == DialogResult.OK)
                {
                    li.SubItems[3].Text = value;
                    li.SubItems[4].Text = (decimal.Parse(li.SubItems[3].Text) * decimal.Parse(li.SubItems[2].Text)).ToString();
                    SumTotalAmount();
                }

            }
                
        }

        private void lvPOS_MouseDown(object sender, MouseEventArgs e)
        {
            li = lvPOS.GetItemAt(e.X, e.Y);
            PosX = e.X;
            PosY = e.Y;
        }

        private void btnBayar_Click(object sender, EventArgs e)
        {
            FormCash sForm = new FormCash();
            sForm.ShowDialog();
        }

        private void txtProductCode_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Reset();
            lvPOS.Items.Clear();
        }


        private void btnSave_Click(object sender, EventArgs e)
        {
            if (lvPOS.Items.Count > 0)
            {
                for (int i = 0; i < lvPOS.Items.Count; i++)
                {
                    cmdAddInvoice.Parameters["@getInvoice"].Value = lblInvoice.Text;
                    cmdAddInvoice.Parameters["@getProductID"].Value = int.Parse(lvPOS.Items[i].SubItems[5].Text);
                    cmdAddInvoice.Parameters["@getUnitPrice"].Value = decimal.Parse(lvPOS.Items[i].SubItems[2].Text);
                    cmdAddInvoice.Parameters["@getQuantity"].Value = int.Parse(lvPOS.Items[i].SubItems[3].Text);
                    cmdAddInvoice.Parameters["@getSubTotal"].Value = decimal.Parse(lvPOS.Items[i].SubItems[4].Text);
                    cmdAddInvoice.Parameters["@getCash"].Value = decimal.Parse(lblCash.Text);
                    cmdAddInvoice.Parameters["@getChange"].Value =  decimal.Parse(lblChange.Text); 
                    cmdAddInvoice.Parameters["@getDateAdded"].Value = DateTime.Now;
                    cmdAddInvoice.Parameters["@getTotalAmount"].Value = decimal.Parse(lblTotalAmount.Text);

                    cmdAddInvoice.ExecuteNonQuery();
                }

                Reset();
                lvPOS.Items.Clear();
                lblTotalAmount.Text = "0";
                lblTotal.Text = "0";
                lblCash.Text = "0";
                lblChange.Text = "0";

                NewInvoice();
            }
        }

        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }
    }

}