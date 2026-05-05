using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows_Forms_Chat;

namespace Windows_Forms_Chat
{
    public partial class LoginRegisterForm : Form
    {
        public LoginRegisterForm()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void LoginRegisterForm_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(prefilledUsername))
            {
                usernameTextBox.Text = prefilledUsername;
                passwordTextBox.Focus();
            }
        }

        public TCPChatClient client;

        public string prefilledUsername = "";

        private void loginButton_Click(object sender, EventArgs e)
        {
            string username = usernameTextBox.Text.Trim();
            string password = passwordTextBox.Text.Trim();

            if (username == "" || password == "")
            {
                MessageBox.Show("Please enter username and password");
                return;
            }

            client.pendingUsername = username;
            client.SendString("!login " + username + " " + password);
        }

        private void registerButton_Click(object sender, EventArgs e)
        {
            string username = usernameTextBox.Text.Trim();
            string password = passwordTextBox.Text.Trim();

            if (username == "" || password == "")
            {
                MessageBox.Show("Please enter username and password");
                return;
            }

            client.pendingUsername = username;
            client.SendString("!register " + username + " " + password);
        }
    }
}
