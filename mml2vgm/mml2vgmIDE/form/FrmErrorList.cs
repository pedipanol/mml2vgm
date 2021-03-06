﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace mml2vgmIDE
{
    public partial class FrmErrorList : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        public Action parentUpdate = null;
        public Action<string, long, bool> parentJumpDocument = null;

        public FrmErrorList(Setting setting)
        {
            InitializeComponent();
            this.dataGridView1.BackgroundColor = Color.FromArgb(setting.ColorScheme.ErrorList_BackColor);
            this.dataGridView1.DefaultCellStyle.BackColor = Color.FromArgb(setting.ColorScheme.ErrorList_BackColor);
            this.dataGridView1.ForeColor = Color.FromArgb(setting.ColorScheme.ErrorList_ForeColor);
        }
        protected override string GetPersistString()
        {
            return this.Name;
        }

        private void FrmErrorList_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            parentUpdate?.Invoke();

        }

        private void DataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int rowIndex = e.RowIndex;
            JumpDocument(rowIndex, false);
        }

        private void DataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int rowIndex = e.RowIndex;
            JumpDocument(rowIndex, true);
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count<1 || dataGridView1.SelectedRows[0].Index < 0) return;
            int rowIndex = dataGridView1.SelectedRows[0].Index;
            JumpDocument(rowIndex, false);
        }

        private void JumpDocument(int rowIndex, bool wantFocus)
        {
            if (rowIndex < 0 || rowIndex > dataGridView1.Rows.Count - 1) return;
            string f = dataGridView1.Rows[rowIndex].Cells[1].Value.ToString();
            string l = dataGridView1.Rows[rowIndex].Cells[2].Value.ToString();
            long n = 0;

            if (long.TryParse(l, out n))
            {
                parentJumpDocument?.Invoke(f, n, wantFocus);
            }
        }

        private void FrmErrorList_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
        }

        private void FrmErrorList_Load(object sender, EventArgs e)
        {

        }
    }
}
