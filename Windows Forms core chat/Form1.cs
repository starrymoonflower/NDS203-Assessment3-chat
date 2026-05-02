using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows_Forms_Chat;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;


//https://www.youtube.com/watch?v=xgLRe7QV6QI&ab_channel=HazardEditHazardEdit
namespace Windows_Forms_Chat
{
    public partial class Form1 : Form
    {
        public TicTacToe ticTacToe = new TicTacToe();
        TCPChatServer server = null;
        TCPChatClient client = null;

        public Form1()
        {
            InitializeComponent();

        }

        public bool CanHostOrJoin()
        {
            if (server == null && client == null)
                return true;
            else
                return false;
        }

        // METHOD: HOST / START SERVER
        // This runs when the user clicks the "Host" button
        private void HostButton_Click(object sender, EventArgs e)
        {
            // Only allow hosting if not already hosting or connected as a client
            if (CanHostOrJoin())
            {
                try
                {
                    // Read the port number entered by the user 
                    int port = int.Parse(MyPortTextBox.Text);

                    // Create a new server instance using the port and chat display box
                    server = TCPChatServer.createInstance(port, ChatTextBox, ticTacToe);

                    // If server failed to create (invalid port etc), throw an error
                    if (server == null)
                        //thrown exceptions should exit the try and land in next catch
                        throw new Exception("Incorrect port value!");

                    // Set window title for server
                    this.Text = "Server";

                    // Start the server (begin listening for client connections)
                    server.SetupServer();

                    // Disable TicTacToe buttons for server
                    foreach (Button btn in ticTacToe.buttons)
                    {
                        btn.Enabled = false;
                        btn.BackColor = Color.Lavender;
                    }
                }

                catch (Exception ex)
                {
                    // If anything goes wrong, display error in chat box
                    ChatTextBox.Text += "Error: " + ex;

                    // Move to new line for readability
                    ChatTextBox.AppendText(Environment.NewLine);
                }
            }

        }

        // METHOD: CLIENT JOINS THE CHAT SERVER
        private void JoinButton_Click(object sender, EventArgs e)
        {
            if (CanHostOrJoin())
            {
                try
                {
                    // Read port numbers
                    int port = int.Parse(MyPortTextBox.Text);
                    int serverPort = int.Parse(serverPortTextBox.Text);

                    // Create client
                    client = TCPChatClient.CreateInstance(port, serverPort, ServerIPTextBox.Text, ChatTextBox, ticTacToe);

                    if (client == null)
                        throw new Exception("Incorrect port value!");

                    // Connect to server
                    client.ConnectToServer();

                    // Open login/register popup
                    LoginRegisterForm loginForm = new LoginRegisterForm();
                    loginForm.client = client;
                    loginForm.Show();
                }
                catch (Exception ex)
                {
                    client = null;
                    ChatTextBox.Text += "Error: " + ex;
                    ChatTextBox.AppendText(Environment.NewLine);
                }
            }
        }
        

        // METHOD: SERVER SENDS A MESSAGE
        private void SendButton_Click(object sender, EventArgs e)
        {
            // Get message from textbox
            string message = TypeTextBox.Text.Trim();

            // Don't send empty messages
            if (message == "")
                return;

            // Send message
            if (client != null)
                client.SendString(message);
            else if (server != null)
            {
                // Check if it's a server command first
                if (server.LocalCommand(TypeTextBox.Text) == false)
                {
                    // Display server name as "Server/username"
                    string serverDisplayName = "Server";

                    // Send message to all clients 
                    server.SendToAll(serverDisplayName + ": " + message, null);

                    // Also show in server chat box
                    server.AddToChat(serverDisplayName + ": " + message);
                }
            }

            // Clear the textbox after sending
            TypeTextBox.Clear();

            // Keep cursor ready for typing
            TypeTextBox.Focus();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //On form loaded
            ticTacToe.buttons.Add(button1);
            ticTacToe.buttons.Add(button2);
            ticTacToe.buttons.Add(button3);
            ticTacToe.buttons.Add(button4);
            ticTacToe.buttons.Add(button5);
            ticTacToe.buttons.Add(button6);
            ticTacToe.buttons.Add(button7);
            ticTacToe.buttons.Add(button8);
            ticTacToe.buttons.Add(button9);

            ChatTextBox.Font = new Font("Consolas", 10);
            TypeTextBox.Font = new Font("Consolas", 10);
        }

        private void AttemptMove(int i)
        {
            // If running as server, block move
            if (server != null)
            {
                ChatTextBox.AppendText(server.serverName + "Game actions are only available to clients" + Environment.NewLine);
                return;
            }

            if (ticTacToe.myTurn)
            {   
                client.SendMoveAttemptToServer(i);
                ticTacToe.myTurn = false;
            }
            else
            {
                ChatTextBox.AppendText("It is not your turn yet." + Environment.NewLine);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AttemptMove(0);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AttemptMove(1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            AttemptMove(2);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            AttemptMove(3);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            AttemptMove(4);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            AttemptMove(5);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            AttemptMove(6);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            AttemptMove(7);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            AttemptMove(8);
        }

        private void ChatTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void TypeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // If user presses Enter
            if (e.KeyCode == Keys.Enter)
            {
                // Prevent "ding" sound or newline
                e.SuppressKeyPress = true;

                // Trigger send button logic
                SendButton.PerformClick();
            }
        }

        private void usernameTextbox_TextChanged(object sender, EventArgs e)
        {

        }

    }
}
