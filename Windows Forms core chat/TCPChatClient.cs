using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data.SQLite;

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

        //// store username before server accepts
        //public string pendingUsername = "";

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
                    break;

                case "!player2":
                    AddToChat("Joined Tic-Tac-Toe as Player 2 (naught)");
                    ticTacToe.playerTileType = TileType.naught;
                    ticTacToe.playerName = "Player2";
                    break;
                case "!yourturn":
                    AddToChat("It's your turn" + ticTacToe.playerName);
                    ticTacToe.myTurn = true;
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

            // Detect successful registration
            if (text.StartsWith("Registration successful"))
            {
                // Keep the form open so the user can now press Login
                MessageBox.Show("Registration successful. Please login with your username and password.");

                chatTextBox.Invoke((Action)delegate
                {
                    foreach (Form form in Application.OpenForms)
                    {
                        if (form.Name == "LoginRegisterForm")
                        {
                            // Clear the textboxes
                            TextBox userBox = form.Controls["usernameTextBox"] as TextBox;
                            TextBox passBox = form.Controls["passwordTextBox"] as TextBox;

                            if (userBox != null) userBox.Clear();
                            if (passBox != null) passBox.Clear();

                            // Put cursor back in username box
                            userBox?.Focus();

                            break;
                        }
                    }
                });
            
            }





            //// Detect login/register success
            //if (text.StartsWith("Login successful") || text.StartsWith("Registration successful"))
            //{
            //    // Extract username (we already sent it from popup)
            //    chatTextBox.Invoke((Action)delegate
            //    {
            //        Form parentForm = chatTextBox.FindForm();

            //        if (parentForm != null)
            //        {
            //            parentForm.Text = "Client: " + pendingUsername;
            //        }
            //    });

            //    // Close login popup
            //    CloseLoginForm();
            //}
            //if (text.StartsWith("Registration successful"))
            //{
            //    MessageBox.Show("Registration successful. Please login w ur username and password.");
            //}


            //text is from server but could have been broadcast from the other clients
            //AddToChat( text );

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

            if (text.StartsWith("Login failed") || text.StartsWith("Registration failed"))
            {
                MessageBox.Show(text);
            }

            if (text.Contains("You have been kicked"))
            {
                socket.Close();
                return;
            }

            //we just received a message from this socket, better keep an ear out with another thread for the next one
            currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentClientSocket);
        }
        //public void Close()
        //{
        //    socket.Close();
        //}

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
