namespace Windows_Forms_Chat
{
    partial class LoginRegisterForm
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
            label1 = new System.Windows.Forms.Label();
            usernameTextBox = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            passwordTextBox = new System.Windows.Forms.TextBox();
            loginButton = new System.Windows.Forms.Button();
            registerButton = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label1.Location = new System.Drawing.Point(104, 50);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(63, 14);
            label1.TabIndex = 0;
            label1.Text = "Username";
            label1.Click += label1_Click;
            // 
            // usernameTextBox
            // 
            usernameTextBox.BackColor = System.Drawing.Color.White;
            usernameTextBox.ForeColor = System.Drawing.SystemColors.WindowText;
            usernameTextBox.Location = new System.Drawing.Point(104, 68);
            usernameTextBox.Name = "usernameTextBox";
            usernameTextBox.Size = new System.Drawing.Size(100, 23);
            usernameTextBox.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label2.Location = new System.Drawing.Point(104, 123);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(63, 14);
            label2.TabIndex = 2;
            label2.Text = "Password";
            // 
            // passwordTextBox
            // 
            passwordTextBox.BackColor = System.Drawing.Color.White;
            passwordTextBox.Location = new System.Drawing.Point(104, 141);
            passwordTextBox.Name = "passwordTextBox";
            passwordTextBox.Size = new System.Drawing.Size(100, 23);
            passwordTextBox.TabIndex = 3;
            passwordTextBox.UseSystemPasswordChar = true;
            // 
            // loginButton
            // 
            loginButton.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            loginButton.Location = new System.Drawing.Point(115, 210);
            loginButton.Name = "loginButton";
            loginButton.Size = new System.Drawing.Size(75, 23);
            loginButton.TabIndex = 4;
            loginButton.Text = "Login";
            loginButton.UseVisualStyleBackColor = true;
            loginButton.Click += loginButton_Click;
            // 
            // registerButton
            // 
            registerButton.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            registerButton.Location = new System.Drawing.Point(115, 239);
            registerButton.Name = "registerButton";
            registerButton.Size = new System.Drawing.Size(75, 23);
            registerButton.TabIndex = 5;
            registerButton.Text = "Register";
            registerButton.UseVisualStyleBackColor = true;
            registerButton.Click += registerButton_Click;
            // 
            // LoginRegisterForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(314, 318);
            Controls.Add(registerButton);
            Controls.Add(loginButton);
            Controls.Add(passwordTextBox);
            Controls.Add(label2);
            Controls.Add(usernameTextBox);
            Controls.Add(label1);
            Name = "LoginRegisterForm";
            Text = "LoginRegisterForm";
            Load += LoginRegisterForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox usernameTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.Button loginButton;
        private System.Windows.Forms.Button registerButton;
    }
}