namespace RSignTestForm
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.BtnSend = new System.Windows.Forms.Button();
            this.BtnGetStatus = new System.Windows.Forms.Button();
            this.TxtEnvelopeDisplayCode = new System.Windows.Forms.TextBox();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.BtnCancel = new System.Windows.Forms.Button();
            this.BtnSendPdfs = new System.Windows.Forms.Button();
            this.TxtEnvelopeId = new System.Windows.Forms.TextBox();
            this.LblEnvelopeDisplayCode = new System.Windows.Forms.Label();
            this.LblEnvelopeId = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // BtnSend
            // 
            this.BtnSend.Location = new System.Drawing.Point(357, 41);
            this.BtnSend.Name = "BtnSend";
            this.BtnSend.Size = new System.Drawing.Size(75, 23);
            this.BtnSend.TabIndex = 1;
            this.BtnSend.Text = "Send Bytes";
            this.BtnSend.UseVisualStyleBackColor = true;
            this.BtnSend.Click += new System.EventHandler(this.BtnSend_Click);
            // 
            // BtnGetStatus
            // 
            this.BtnGetStatus.Location = new System.Drawing.Point(357, 70);
            this.BtnGetStatus.Name = "BtnGetStatus";
            this.BtnGetStatus.Size = new System.Drawing.Size(75, 23);
            this.BtnGetStatus.TabIndex = 2;
            this.BtnGetStatus.Text = "Get Status";
            this.BtnGetStatus.UseVisualStyleBackColor = true;
            this.BtnGetStatus.Click += new System.EventHandler(this.BtnGetStatus_Click);
            // 
            // TxtEnvelopeDisplayCode
            // 
            this.TxtEnvelopeDisplayCode.Location = new System.Drawing.Point(128, 72);
            this.TxtEnvelopeDisplayCode.Name = "TxtEnvelopeDisplayCode";
            this.TxtEnvelopeDisplayCode.Size = new System.Drawing.Size(223, 20);
            this.TxtEnvelopeDisplayCode.TabIndex = 3;
            // 
            // txtStatus
            // 
            this.txtStatus.Location = new System.Drawing.Point(435, 12);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.Size = new System.Drawing.Size(323, 173);
            this.txtStatus.TabIndex = 4;
            // 
            // BtnCancel
            // 
            this.BtnCancel.Location = new System.Drawing.Point(357, 99);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(75, 23);
            this.BtnCancel.TabIndex = 5;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.UseVisualStyleBackColor = true;
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // BtnSendPdfs
            // 
            this.BtnSendPdfs.Location = new System.Drawing.Point(357, 12);
            this.BtnSendPdfs.Name = "BtnSendPdfs";
            this.BtnSendPdfs.Size = new System.Drawing.Size(75, 23);
            this.BtnSendPdfs.TabIndex = 6;
            this.BtnSendPdfs.Text = "Send Pdf";
            this.BtnSendPdfs.UseVisualStyleBackColor = true;
            this.BtnSendPdfs.Click += new System.EventHandler(this.BtnSendPdfs_Click);
            // 
            // TxtEnvelopeId
            // 
            this.TxtEnvelopeId.Location = new System.Drawing.Point(128, 101);
            this.TxtEnvelopeId.Name = "TxtEnvelopeId";
            this.TxtEnvelopeId.Size = new System.Drawing.Size(223, 20);
            this.TxtEnvelopeId.TabIndex = 7;
            // 
            // LblEnvelopeDisplayCode
            // 
            this.LblEnvelopeDisplayCode.AutoSize = true;
            this.LblEnvelopeDisplayCode.Location = new System.Drawing.Point(5, 75);
            this.LblEnvelopeDisplayCode.Name = "LblEnvelopeDisplayCode";
            this.LblEnvelopeDisplayCode.Size = new System.Drawing.Size(120, 13);
            this.LblEnvelopeDisplayCode.TabIndex = 8;
            this.LblEnvelopeDisplayCode.Text = "Envelope Display Code:";
            // 
            // LblEnvelopeId
            // 
            this.LblEnvelopeId.AutoSize = true;
            this.LblEnvelopeId.Location = new System.Drawing.Point(5, 104);
            this.LblEnvelopeId.Name = "LblEnvelopeId";
            this.LblEnvelopeId.Size = new System.Drawing.Size(67, 13);
            this.LblEnvelopeId.TabIndex = 9;
            this.LblEnvelopeId.Text = "Envelope Id:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(770, 414);
            this.Controls.Add(this.LblEnvelopeId);
            this.Controls.Add(this.LblEnvelopeDisplayCode);
            this.Controls.Add(this.TxtEnvelopeId);
            this.Controls.Add(this.BtnSendPdfs);
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.TxtEnvelopeDisplayCode);
            this.Controls.Add(this.BtnGetStatus);
            this.Controls.Add(this.BtnSend);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button BtnSend;
        private System.Windows.Forms.Button BtnGetStatus;
        private System.Windows.Forms.TextBox TxtEnvelopeDisplayCode;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Button BtnCancel;
        private System.Windows.Forms.Button BtnSendPdfs;
        private System.Windows.Forms.TextBox TxtEnvelopeId;
        private System.Windows.Forms.Label LblEnvelopeDisplayCode;
        private System.Windows.Forms.Label LblEnvelopeId;
    }
}

