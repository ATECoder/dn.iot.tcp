namespace cc.isr.Iot.Tcp.Client.WinForms.Concept
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if ( disposing && (components != null) )
            {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region " windows form designer generated code "

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.InstrumentLabel = new System.Windows.Forms.Label();
            this.WelcomeLabel = new System.Windows.Forms.Label();
            this.CounterBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // InstrumentLabel
            // 
            this.InstrumentLabel.AutoSize = true;
            this.InstrumentLabel.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.InstrumentLabel.Location = new System.Drawing.Point(22, 18);
            this.InstrumentLabel.Name = "InstrumentLabel";
            this.InstrumentLabel.Size = new System.Drawing.Size(190, 45);
            this.InstrumentLabel.TabIndex = 0;
            this.InstrumentLabel.Text = "Hello World";
            // 
            // WelcomeLabel
            // 
            this.WelcomeLabel.AutoSize = true;
            this.WelcomeLabel.Location = new System.Drawing.Point(32, 156);
            this.WelcomeLabel.Name = "WelcomeLabel";
            this.WelcomeLabel.Size = new System.Drawing.Size(150, 15);
            this.WelcomeLabel.TabIndex = 1;
            this.WelcomeLabel.Text = "Welcome to .NET WPF App";
            // 
            // CounterBtn
            // 
            this.CounterBtn.Location = new System.Drawing.Point(32, 82);
            this.CounterBtn.Name = "CounterBtn";
            this.CounterBtn.Size = new System.Drawing.Size(150, 23);
            this.CounterBtn.TabIndex = 2;
            this.CounterBtn.Text = "Click Me";
            this.CounterBtn.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.CounterBtn);
            this.Controls.Add(this.WelcomeLabel);
            this.Controls.Add(this.InstrumentLabel);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label InstrumentLabel;
        private Label WelcomeLabel;
        private Button CounterBtn;
    }
}
