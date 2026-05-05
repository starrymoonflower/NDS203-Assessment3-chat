using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

//reference: https://github.com/AbleOpus/NetworkingSamples/blob/master/MultiClient/Program.cs
namespace Windows_Forms_Chat
{
    public class TCPChatClient : TCPChatBase
    {
        //public static TCPChatClient tcpChatClient;
        public Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public ClientSocket clientSocket = new ClientSocket();

        public int serverPort;
        public string serverIP;

        public ClientState myState = ClientState.LOGIN;

        // track if client is intentionally exiting
        public bool isExiting = false;

        // track username on client
        public string pendingUsername;


        public static TCPChatClient CreateInstance(int port, int serverPort, string serverIP, RichTextBox chatTextBox, TicTacToe ttt)
        {
            TCPChatClient tcp = null;
            //if port values are valid and ip worth attempting to join
            if (port > 0 && port < 65535 && 
                serverPort > 0 && serverPort < 65535 && 
                serverIP.Length > 0 &&
                chatTextBox != null)
            {
                tcp = new TCPChatClient();
                tcp.port = port;
                tcp.serverPort = serverPort;
                tcp.serverIP = serverIP;
                tcp.chatTextBox = chatTextBox;
                tcp.clientSocket.socket = tcp.socket;
                tcp.ticTacToe = ttt;
            }

            return tcp;
        }

        public void ConnectToServer()
        {
            int attempts = 0;

            // while not connected attempt to connect
            while (!socket.Connected)
            {
                try
                {
                    attempts++;
                    SetChat("Connection attempt " + attempts);
                    // Change IPAddress.Loopback to a remote IP to connect to a remote host.
                    socket.Connect(serverIP, serverPort);
                }
                catch (SocketException)
                {
                    chatTextBox.Text = "";
                }
            }

            //Console.Clear();
            AddToChat("Connected");
            //keep open thread for receiving data
            clientSocket.socket.BeginReceive(clientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, clientSocket);
        }

        public void SendString(string text)
        {
            try
            {
                // Do not send if socket is closed or disconnected
                if (socket == null || !socket.Connected)
                {
                    AddToChat("You are disconnected and cannot send messages.");
                    return;
                }

                if (text.ToLower() == "!exit")
                {
                    isExiting = true;
                }

                if (text.ToLower().StartsWith("!username "))
                {
                    pendingUsername = text.Substring(10).Trim();
                }

                byte[] buffer = Encoding.ASCII.GetBytes(text);
                socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }
            catch (ObjectDisposedException)
            {
                AddToChat("You are disconnected and cannot send messages.");
            }
            catch (SocketException)
            {
                AddToChat("Connection lost. You cannot send messages.");
            }
        }
        
        public void ReceiveCallback(IAsyncResult AR)
        {
            ClientSocket currentClientSocket = (ClientSocket)AR.AsyncState;

            int received;

            try
            {
                received = currentClientSocket.socket.EndReceive(AR);

                // let users know if the server drops out
                if (received == 0)
                {
                    if (!isExiting)
                    {
                        AddToChat("Server disconnected.");
                    }
                    currentClientSocket.socket.Close();
                    return;
                }
            }
            catch (SocketException)
            {
                AddToChat("Connection lost. Server may have disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                currentClientSocket.socket.Close();
                return;
            }
            //read bytes from packet
            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            
            //convert to string so we can work with it
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine("Received Text: " + text);

            string[] param = text.ToLower().Split(' ');

            switch (param[0])
            {
                case "!state":
                    if (param[1].Equals("0"))
                    {
                        myState = ClientState.LOGIN;
                    }
                    else if (param[1].Equals("1"))
                    {
                        myState = ClientState.CHATTING;
                    }
                    else if (param[1].Equals("2"))
                    {
                        myState = ClientState.PLAYING;
                    }
                    AddToChat("State Updated to: " + myState.ToString());
                    break;

                case "!player1":
                    AddToChat("Joined Tic-Tac-Toe as Player 1 (cross)");
                    ticTacToe.playerTileType = TileType.cross;
                    ticTacToe.playerName = "Player1";

                    // Clear and reset board as soon as player joins
                    ticTacToe.ResetBoard();

                    // Player 1 will only be allowed to move when server sends !yourturn
                    ticTacToe.myTurn = false;

                    break;

                case "!player2":
                    AddToChat("Joined Tic-Tac-Toe as Player 2 (naught)");
                    ticTacToe.playerTileType = TileType.naught;
                    ticTacToe.playerName = "Player2";

                    // Clear and reset board as soon as player joins
                    ticTacToe.ResetBoard();

                    // Player 2 must wait for Player 1 to move first
                    ticTacToe.myTurn = false;

                    break;
                case "!yourturn":
                    AddToChat("It's your turn, " + ticTacToe.playerName + "!");
                    ticTacToe.myTurn = true;

                    // Lets Player know its their turn visually
                    ticTacToe.HighlightAvailableTiles();
                    break;
                case "!otherturn":
                    AddToChat("It's the Opponent's turn");
                    break;
                case "!board":
                    string boardState = param[1];
                    AddToChat("Board Update: " + boardState);
                    ticTacToe.StringToGrid(boardState);
                    break;
                default:
                    //text is from server but could have been broadcast from the other clients
                    AddToChat(text);
                    break;

            }

            // Detect successful login
            if (text.StartsWith("Login successful"))
            {
                // Update the main window title with the logged-in username
                chatTextBox.Invoke((Action)delegate
                {
                    Form parentForm = chatTextBox.FindForm();

                    if (parentForm != null)
                    {
                        parentForm.Text = "Client: " + pendingUsername;
                    }
                });

                // Only close the login/register form after LOGIN succeeds
                CloseLoginForm();
            }

            // Detect successful registration from server
            if (text.StartsWith("Registration successful"))
            {
                // Keep the login/register form open so the user can proceed to login

                chatTextBox.BeginInvoke((Action)delegate
                {
                    // Loop through open forms to find the login/register form
                    foreach (Form form in Application.OpenForms)
                    {
                        if (form.Name == "LoginRegisterForm")
                        {
                            // Show confirmation message owned by login form so it stays in front
                            MessageBox.Show(
                                form,
                                "Registration successful. Please login with your username and password.",
                                "Registration Successful",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.None
                            );

                            // Bring login form to front after message closes
                            form.WindowState = FormWindowState.Normal;
                            form.Activate();
                            form.BringToFront();

                            // Get reference to username and password textboxes
                            TextBox userBox = form.Controls.Find("usernameTextBox", true).FirstOrDefault() as TextBox;
                            TextBox passBox = form.Controls.Find("passwordTextBox", true).FirstOrDefault() as TextBox;

                            // Clear username and password fields after registration
                            if (userBox != null)
                            {
                                userBox.Clear();
                            }

                            if (passBox != null)
                            {
                                passBox.Clear();
                            }

                            // Set Cursor back to username field for next input
                            userBox?.Focus();

                            break;
                        }
                    }
                });
            
            }

            // After receiving any message, return focus to the main input textbox 
            // so user can continue typing without clicking manually
            chatTextBox.Invoke((Action)delegate
            {
                Form parentForm = chatTextBox.FindForm();

                if (parentForm != null)
                {
                    Control typeBox = parentForm.Controls["TypeTextBox"];
                    if (typeBox != null)
                    {
                        typeBox.Focus();
                    }
                }
            });

            // Detect when server confirms username has been accepted 
            // Update the main window title to reflect the logged-in user
            if (text.StartsWith("Username accepted"))
            {
                chatTextBox.Invoke((Action)delegate
                {
                    Form parentForm = chatTextBox.FindForm();

                    if (parentForm != null)
                    {
                        parentForm.Text = "Client: " + pendingUsername;
                    }
                });
            }

            // Handle login failure or invalid 
            // Bring login/register form to front and prompt user to retry
            if (text.StartsWith("Login failed") || text.StartsWith("Invalid username or password"))
            {
                chatTextBox.BeginInvoke((Action)delegate
                {
                    foreach (Form form in Application.OpenForms)
                    {
                        if (form.Name == "LoginRegisterForm")
                        {
                            // Show error message attached to login form
                            MessageBox.Show(
                                form,
                                text,
                                "Login Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.None
                            );

                            // Bring login form back to front for retry
                            form.WindowState = FormWindowState.Normal;
                            form.Activate();
                            form.BringToFront();

                            // Clear password field and focus for re-entry
                            TextBox passBox = form.Controls.Find("passwordTextBox", true).FirstOrDefault() as TextBox;

                            if (passBox != null)
                            {
                                passBox.Clear();
                                passBox.Focus();
                            }
                            break;
                        }
                    }
                });
            }
            else if (text.StartsWith("Registration failed"))
            {
                chatTextBox.BeginInvoke((Action)delegate
                {
                    foreach (Form form in Application.OpenForms)
                    {
                        if (form.Name == "LoginRegisterForm")
                        {
                            MessageBox.Show(
                                form,
                                text,
                                "Registration Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.None
                            );

                            form.Activate();
                            form.BringToFront();

                            break;
                        }
                    }
                });
            }

            if (text.Contains("You have been kicked"))
            {
                socket.Close();
                return;
            }

            //we just received a message from this socket, better keep an ear out with another thread for the next one
            currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentClientSocket);
        }
       

        // Close popup method
        private void CloseLoginForm()
        {
            chatTextBox.Invoke((Action)delegate
            {
                Form parentForm = chatTextBox.FindForm();

                foreach (Form form in Application.OpenForms)
                {
                    if (form.Name == "LoginRegisterForm")
                    {
                        form.Close();
                        break;
                    }
                }
            });
        }

        public void SendMoveAttemptToServer(int i)
        {
            SendString("!move " + i.ToString());
        }
        public void Close()
        {
            socket.Close();
        }


    }



}
