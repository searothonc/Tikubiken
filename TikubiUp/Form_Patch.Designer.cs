
namespace Tikubiken
{
    partial class Form_Patch
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_Patch));
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.button_start = new System.Windows.Forms.Button();
            this.label_message = new System.Windows.Forms.Label();
            this.label_Progress = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // progressBar
            // 
            resources.ApplyResources(this.progressBar, "progressBar");
            this.progressBar.Name = "progressBar";
            // 
            // button_start
            // 
            resources.ApplyResources(this.button_start, "button_start");
            this.button_start.Name = "button_start";
            this.button_start.UseVisualStyleBackColor = true;
            // 
            // label_message
            // 
            resources.ApplyResources(this.label_message, "label_message");
            this.label_message.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label_message.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label_message.Name = "label_message";
            // 
            // label_Progress
            // 
            resources.ApplyResources(this.label_Progress, "label_Progress");
            this.label_Progress.Name = "label_Progress";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::TikubiUp.Properties.Resources.cover_update;
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // Form_Patch
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label_Progress);
            this.Controls.Add(this.label_message);
            this.Controls.Add(this.button_start);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form_Patch";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button button_start;
        private System.Windows.Forms.Label label_message;
        private System.Windows.Forms.Label label_Progress;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}

