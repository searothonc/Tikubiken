
namespace TikubiDiff
{
    partial class Form_Diff
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Diff));
            this.label_source = new System.Windows.Forms.Label();
            this.textBox_source = new System.Windows.Forms.TextBox();
            this.button_source = new System.Windows.Forms.Button();
            this.label_output = new System.Windows.Forms.Label();
            this.textBox_output = new System.Windows.Forms.TextBox();
            this.button_output = new System.Windows.Forms.Button();
            this.label_Progress = new System.Windows.Forms.Label();
            this.button_start = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.label_log = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label_source
            // 
            this.label_source.AutoSize = true;
            this.label_source.Location = new System.Drawing.Point(8, 8);
            this.label_source.Name = "label_source";
            this.label_source.Size = new System.Drawing.Size(70, 15);
            this.label_source.TabIndex = 0;
            this.label_source.Text = "&Source XML";
            // 
            // textBox_source
            // 
            this.textBox_source.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_source.Location = new System.Drawing.Point(8, 24);
            this.textBox_source.Name = "textBox_source";
            this.textBox_source.Size = new System.Drawing.Size(400, 23);
            this.textBox_source.TabIndex = 1;
            // 
            // button_source
            // 
            this.button_source.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_source.Location = new System.Drawing.Point(416, 24);
            this.button_source.Name = "button_source";
            this.button_source.Size = new System.Drawing.Size(72, 24);
            this.button_source.TabIndex = 2;
            this.button_source.Text = "File";
            this.button_source.UseVisualStyleBackColor = true;
            this.button_source.Click += new System.EventHandler(this.button_source_Click);
            // 
            // label_output
            // 
            this.label_output.AutoSize = true;
            this.label_output.Location = new System.Drawing.Point(8, 56);
            this.label_output.Name = "label_output";
            this.label_output.Size = new System.Drawing.Size(45, 15);
            this.label_output.TabIndex = 3;
            this.label_output.Text = "&Output";
            // 
            // textBox_output
            // 
            this.textBox_output.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_output.Location = new System.Drawing.Point(8, 72);
            this.textBox_output.Name = "textBox_output";
            this.textBox_output.Size = new System.Drawing.Size(400, 23);
            this.textBox_output.TabIndex = 4;
            this.textBox_output.Text = "updator.exe";
            // 
            // button_output
            // 
            this.button_output.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_output.Location = new System.Drawing.Point(416, 72);
            this.button_output.Name = "button_output";
            this.button_output.Size = new System.Drawing.Size(72, 24);
            this.button_output.TabIndex = 5;
            this.button_output.Text = "File";
            this.button_output.UseVisualStyleBackColor = true;
            // 
            // label_Progress
            // 
            this.label_Progress.AutoSize = true;
            this.label_Progress.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label_Progress.Location = new System.Drawing.Point(8, 144);
            this.label_Progress.Name = "label_Progress";
            this.label_Progress.Size = new System.Drawing.Size(52, 15);
            this.label_Progress.TabIndex = 8;
            this.label_Progress.Text = "Progress";
            // 
            // button_start
            // 
            this.button_start.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.button_start.Enabled = false;
            this.button_start.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.button_start.Location = new System.Drawing.Point(216, 112);
            this.button_start.Name = "button_start";
            this.button_start.Size = new System.Drawing.Size(80, 24);
            this.button_start.TabIndex = 7;
            this.button_start.Text = "S&tart";
            this.button_start.UseVisualStyleBackColor = true;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.progressBar.Location = new System.Drawing.Point(8, 160);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(480, 23);
            this.progressBar.TabIndex = 6;
            // 
            // label_log
            // 
            this.label_log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label_log.BackColor = System.Drawing.SystemColors.Window;
            this.label_log.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label_log.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label_log.Location = new System.Drawing.Point(8, 200);
            this.label_log.Name = "label_log";
            this.label_log.Padding = new System.Windows.Forms.Padding(4);
            this.label_log.Size = new System.Drawing.Size(480, 72);
            this.label_log.TabIndex = 9;
            this.label_log.Text = "log";
            // 
            // Form_Diff
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(496, 281);
            this.Controls.Add(this.label_log);
            this.Controls.Add(this.label_Progress);
            this.Controls.Add(this.button_start);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.button_output);
            this.Controls.Add(this.textBox_output);
            this.Controls.Add(this.label_output);
            this.Controls.Add(this.button_source);
            this.Controls.Add(this.textBox_source);
            this.Controls.Add(this.label_source);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form_Diff";
            this.Text = "Tikubiken";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form_Diff_FormClosed);
            this.Load += new System.EventHandler(this.Form_Diff_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_source;
        private System.Windows.Forms.TextBox textBox_source;
        private System.Windows.Forms.Button button_source;
        private System.Windows.Forms.Label label_output;
        private System.Windows.Forms.TextBox textBox_output;
        private System.Windows.Forms.Button button_output;
        private System.Windows.Forms.Label label_Progress;
        private System.Windows.Forms.Button button_start;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label label_log;
    }
}

