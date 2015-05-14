namespace WindowsFormsExample
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
            this.txtQuery = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lstBox = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // txtQuery
            // 
            this.txtQuery.Location = new System.Drawing.Point(112, 13);
            this.txtQuery.Name = "txtQuery";
            this.txtQuery.Size = new System.Drawing.Size(337, 20);
            this.txtQuery.TabIndex = 0;
            this.txtQuery.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtQuery_KeyUp);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Start typing here : ";
            // 
            // lstBox
            // 
            this.lstBox.FormattingEnabled = true;
            this.lstBox.Location = new System.Drawing.Point(16, 39);
            this.lstBox.Name = "lstBox";
            this.lstBox.Size = new System.Drawing.Size(433, 277);
            this.lstBox.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(637, 357);
            this.Controls.Add(this.lstBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtQuery);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtQuery;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lstBox;
    }
}

