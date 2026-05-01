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

                    // Set server name from textbox (e.g Bob)
                    //string name = usernameTextbox.Text.Trim();

                    // If empty, default to "Server", Otherwise use the entered name
                    //server.serverName = "Server";

                    // Set window title for server
                    this.Text = "Server";

                    // Start the server (begin listening for client connections)
                    server.SetupServer();
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
        //private void JoinButton_Click(object sender, EventArgs e)
        //{
        //    if (CanHostOrJoin())
        //    {
        //        try
        //        { 
        //            // Get username from textbox first before connecting
        //            // and remove spaces at start/end
        //            string username = usernameTextbox.Text.Trim();

        //            // Validate username
        //            if (username == "")
        //            {
        //                MessageBox.Show("Please enter a username");
        //                return;
        //            }

        //            // Check username length
        //            if (username.Length < 3)
        //            {
        //                MessageBox.Show("Username must be at least 3 characters");
        //                return;
        //            }

        //            // Prevent spaces in username
        //            if (username.Contains(" "))
        //            {
        //                MessageBox.Show("Username cannot contain spaces");
        //                return;
        //            }

        //            // Allow only letters and numbers
        //            if (!username.All(char.IsLetterOrDigit))
        //            {
        //                MessageBox.Show("Username must contain only letters and numbers");
        //                return;
        //            }

        //            // Prevent clients using reserved system/server names
        //            if (username.Equals("server", StringComparison.OrdinalIgnoreCase) ||
        //                username.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
        //                username.Equals("moderator", StringComparison.OrdinalIgnoreCase) ||
        //                username.Equals("mod", StringComparison.OrdinalIgnoreCase) ||
        //                username.Equals("system", StringComparison.OrdinalIgnoreCase))
        //            {
        //                MessageBox.Show("This username is reserved.");
        //                return;
        //            }

        //            // Read port numbers and create client only after username is valid
        //            int port = int.Parse(MyPortTextBox.Text);
        //            int serverPort = int.Parse(serverPortTextBox.Text);

        //            client = TCPChatClient.CreateInstance(port, serverPort, ServerIPTextBox.Text, ChatTextBox);

        //            if (client == null)
        //                throw new Exception("Incorrect port value!");

        //            client.ConnectToServer();

        //            LoginRegisterForm loginForm = new LoginRegisterForm();
        //            loginForm.client = client;
        //            loginForm.Show();

        //            // Store username locally until server confirms it
        //            client.pendingUsername = username;

        //            // Send username to server
        //            client.SendString("!username " + username);

        //            // Show username in the window title
        //            // this.Text = $"Client: {username}";
        //        }
        //        catch (Exception ex)
        //        {
        //            client = null;
        //            ChatTextBox.Text += "Error: " + ex;
        //            ChatTextBox.AppendText(Environment.NewLine);
        //        }
        //    }
        //}

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
        }

        private void AttemptMove(int i)
        {
            if (ticTacToe.myTurn)
            {   
                client.SendMoveAttemptToServer(i);
                ticTacToe.myTurn = false;




                /*bool validMove = ticTacToe.SetTile(i, ticTacToe.playerTileType);
                if (validMove)
                {
                    //tell server about it
                    //ticTacToe.myTurn = false;//call this too when ready with server
                }
                //example, do something similar from server
                GameState gs = ticTacToe.GetGameState();
                if (gs == GameState.crossWins)
                {
                    ChatTextBox.AppendText("X wins!");
                    ChatTextBox.AppendText(Environment.NewLine);
                    ticTacToe.ResetBoard();
                }
                if (gs == GameState.naughtWins)
                {
                    ChatTextBox.AppendText(") wins!");
                    ChatTextBox.AppendText(Environment.NewLine);
                    ticTacToe.ResetBoard();
                }
                if (gs == GameState.draw)
                {
                    ChatTextBox.AppendText("Draw!");
                    ChatTextBox.AppendText(Environment.NewLine);
                    ticTacToe.ResetBoard();
                }*/
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
