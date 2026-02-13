namespace ETAB_Automation
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.btnStartETABS = new System.Windows.Forms.Button();
            this.btnImportCAD = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.grpActions = new System.Windows.Forms.GroupBox();
            this.grpActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStartETABS
            // 
            this.btnStartETABS.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnStartETABS.Location = new System.Drawing.Point(30, 40);
            this.btnStartETABS.Name = "btnStartETABS";
            this.btnStartETABS.Size = new System.Drawing.Size(300, 50);
            this.btnStartETABS.TabIndex = 0;
            this.btnStartETABS.Text = "Connect to ETABS";
            this.btnStartETABS.UseVisualStyleBackColor = true;
            this.btnStartETABS.Click += new System.EventHandler(this.btnStartETABS_Click);
            // 
            // btnImportCAD
            // 
            this.btnImportCAD.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnImportCAD.Location = new System.Drawing.Point(30, 110);
            this.btnImportCAD.Name = "btnImportCAD";
            this.btnImportCAD.Size = new System.Drawing.Size(300, 50);
            this.btnImportCAD.TabIndex = 1;
            this.btnImportCAD.Text = "Import CAD && Configure Building";
            this.btnImportCAD.UseVisualStyleBackColor = true;
            this.btnImportCAD.Click += new System.EventHandler(this.btnImportCAD_Click);
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.IndianRed;
            this.btnExit.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnExit.ForeColor = System.Drawing.Color.White;
            this.btnExit.Location = new System.Drawing.Point(30, 180);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(300, 50);
            this.btnExit.TabIndex = 2;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(30, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(290, 25);
            this.lblTitle.TabIndex = 2;
            this.lblTitle.Text = "ETABS CAD Import Automation";
            // 
            // grpActions
            // 
            this.grpActions.Controls.Add(this.btnStartETABS);
            this.grpActions.Controls.Add(this.btnImportCAD);
            this.grpActions.Controls.Add(this.btnExit);
            this.grpActions.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.grpActions.Location = new System.Drawing.Point(30, 60);
            this.grpActions.Name = "grpActions";
            this.grpActions.Size = new System.Drawing.Size(360, 260);
            this.grpActions.TabIndex = 3;
            this.grpActions.TabStop = false;
            this.grpActions.Text = "Actions";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(420, 350);
            this.Controls.Add(this.grpActions);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ETABS CAD Automation";
            this.grpActions.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnStartETABS;
        private System.Windows.Forms.Button btnImportCAD;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.GroupBox grpActions;
    }
}