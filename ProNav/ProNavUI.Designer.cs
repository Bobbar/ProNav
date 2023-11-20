namespace ProNav
{
    partial class ProNavUI
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
			SuspendLayout();
			// 
			// ProNavUI
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(1330, 775);
			Name = "ProNavUI";
			Text = "ProNav - Proportional Navigation";
			KeyDown += Form1_KeyDown;
			KeyPress += Form1_KeyPress;
			KeyUp += Form1_KeyUp;
			MouseDown += Form1_MouseDown;
			MouseUp += Form1_MouseUp;
			ResumeLayout(false);
		}

		#endregion
	}
}