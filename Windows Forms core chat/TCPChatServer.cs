using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Net.Sockets;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows_Forms_Chat;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

//https://github.com/AbleOpus/NetworkingSamples/blob/master/MultiServer/Program.cs
namespace Windows_Forms_Chat
{
    public class TCPChatServer : TCPChatBase
    {
        // variable name for server
        public string serverName = "Server";

        public Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //connected clients
        public List<ClientSocket> clientSockets = new List<ClientSocket>();

        public string player1 = null;
        public string player2 = null;
        // which player's turn it is
        public string currentTurn = null;

        // kicked user should not cause unexpected disconnect
        public bool kicked;

        public static TCPChatServer createInstance(int port, RichTextBox chatTextBox, TicTacToe ttt)
        {
            TCPChatServer tcp = null;
            //setup if port within range and valid chat box given
            if (port > 0 && port < 65535 && chatTextBox != null)
            {
                tcp = new TCPChatServer();
                tcp.port = port;
                tcp.chatTextBox = chatTextBox;
                tcp.ticTacToe = ttt;
            }

            //return empty if user not enter useful details
            return tcp;
        }

        //METHOD: 
        public void SetupServer()
        {
            DatabaseAccess.StartupDatabase();
            
            //chatTextBox.Text += "Setting up server...\n";
            AddToChat("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(0);
            
            //kick off thread to read connecting clients, when one connects, it'll call out AcceptCallback function
            serverSocket.BeginAccept(AcceptCallback, this);

            //chatTextBox.Text += "Server setup complete" +Environment.NewLine;
            AddToChat("Server setup complete");
        }

        public void CloseAllSockets()
        {
            foreach (ClientSocket clientSocket in clientSockets)
            {
                clientSocket.socket.Shutdown(SocketShutdown.Both);
                clientSocket.socket.Close();
            }
            clientSockets.Clear();
            serverSocket.Close();
        }

        public void AcceptCallback(IAsyncResult AR)
        {
            Socket joiningSocket;

            try
            {
                joiningSocket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
                return;
            }

            ClientSocket newClientSocket = new ClientSocket();
            newClientSocket.socket = joiningSocket;

            clientSockets.Add(newClientSocket);
            
            //start a thread to listen out for this new joining socket. Therefore there is a thread open for each client
            joiningSocket.BeginReceive(newClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, newClientSocket);
            AddToChat("Client connected, waiting for request...");

            //we finished this accept thread, better kick off another so more people can join
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        public bool LocalCommand(string text)
        {
            // Split server message into command parts
            string[] param = text.Split(' ');

            // Get command in lowercase
            string command = param[0].ToLower();

            switch (command)
            {
                case "!join":
                    AddToChat(serverName + ": The server cannot join the Tic-Tac-Toe game. Please use a client.");
                    return true;

                case "!mod":
                    // Example !mod Bob
                    if (param.Length > 1)
                    {
                        string target = param[1].Trim();

                        // Loop through all connected clients
                        foreach (ClientSocket c in clientSockets)
                        {
                            // Check username safely and ignore uppercase/lowercase differences
                            if (!string.IsNullOrWhiteSpace(c.username) && c.username.Equals(target, StringComparison.OrdinalIgnoreCase))
                            {
                                // Toggle moderator true/false
                                c.moderator = !c.moderator;

                                // Check if user is now a moderator
                                if (c.moderator == true)
                                {
                                    // client promoted (message for everyone)
                                    string msg = serverName + ": " + c.username + " has been promoted to Moderator.";

                                    AddToChat(msg);

                                    // Send to everyone except this user
                                    SendToAll(msg, c);

                                    // personalised message for the target user
                                    byte[] data = Encoding.ASCII.GetBytes(serverName + ": You have been promoted to Moderator.");
                                    c.socket.Send(data);
                                }
                                else
                                {
                                    // Client demoted (message for everyone)
                                    string msg = serverName + ": " + c.username + " has been demoted from Moderator.";

                                    AddToChat(msg);

                                    // Send to all except this user
                                    SendToAll(msg, c);

                                    // personalised message for the target user
                                    byte[] data = Encoding.ASCII.GetBytes(serverName + ": You have been demoted from Moderator.");
                                    c.socket.Send(data);
                                }

                                // We found the user and handled the command, so exit the method
                                return true;
                            }
                        }

                        // If loop finished and no matching username was found
                        AddToChat(serverName + ": User not found: " + target);
                    }
                    else
                    {
                        // If server typed !mod without a username
                        AddToChat(serverName + ": Usage: !mod [username]");
                    }

                    return true;

                case "!kick":
                    // Server can kick any connected user
                    if (param.Length > 1)
                    {
                        string target = param[1].Trim();

                        bool kicked = KickUser(target, serverName);
                        if (!kicked)
                        {
                            AddToChat(serverName + ": User not found: " + target);
                        }
                    }
                    else
                    {
                        AddToChat(serverName + ": Usage: !kick[username]");
                    }
                    return true;

                case "!mods":
                    // Server checks list of current moderators
                    AddToChat(GetModerators());
                    return true;

                case "!commands":
                    // show server command list
                    AddToChat(
                        "Available commands:\n" +
                        "!commands - show command list\n" +
                        "!who - list connected users\n" +
                        "!time - show server date/time\n" +
                        "!kick [username] - moderators only");
                    return true;

                case "!who":
                    AddToChat(serverName + ": " + GetConnectedUsers());
                    return true;

                case "!time":
                    AddToChat(serverName + ": " + DateTime.Now.ToString("dddd, dd MMMM yyyy hh:mm tt"));
                    return true;

                default:
                    return false;
            }
        }

        public void ReceiveCallback(IAsyncResult AR)
        {
            ClientSocket currentClientSocket = (ClientSocket)AR.AsyncState;

            int received;

            try
            {
                received = currentClientSocket.socket.EndReceive(AR);
            }
            catch (SocketException)
            {
                if (!currentClientSocket.kicked)
                {
                    // stops server saying "unexpectantly" when kicked was intentional
                    AddToChat("A client forcefully disconnected.");
                }

                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                currentClientSocket.socket.Close();
                clientSockets.Remove(currentClientSocket);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);

            //AddToChat(currentClientSocket.username + ": " + text);
            if (!text.StartsWith("!login") && !text.StartsWith("!register"))
            {
                string displayName = string.IsNullOrWhiteSpace(currentClientSocket.username)
                    ? "Unknown"
                    : currentClientSocket.username;

                AddToChat(displayName + ": " + text);
            }

            string[] param = text.Split(' ');
            string command = param[0].ToLower();

            switch (command)
            {
                // Handles client username registration
                case "!username":
                    if (param.Length > 1)
                    {
                        string username = param[1].Trim();

                        // Check if username already exists
                        if (!CheckUsernameExists(username, currentClientSocket))
                        {
                            // Only save it after validation succeeds
                            currentClientSocket.username = username;
                            currentClientSocket.usernameAccepted = true;

                            // Tell the server
                            AddToChat(serverName + ": User with name " + username + " connected!");

                            // Tell the client it worked
                            byte[] successMsg = Encoding.ASCII.GetBytes("Username accepted. You are now connected to the chat.");
                            currentClientSocket.socket.Send(successMsg);
                        }
                        else
                        {
                            // Username already taken - allow retry
                            currentClientSocket.usernameAccepted = false;

                            byte[] failMsg = Encoding.ASCII.GetBytes("Username already taken. Please choose another one using !username [name] ");
                            currentClientSocket.socket.Send(failMsg);

                            // Allow the user to try another username
                            AddToChat(serverName + ": Username already taken: " + username);
                        }
                    }
                    else
                    {
                        byte[] failMsg = Encoding.ASCII.GetBytes("Usage: !username [name]");
                        currentClientSocket.socket.Send(failMsg);
                    }
                    break;

                // Sends a list of available commands to user who ask  
                case "!commands":

                    string commandList =
                        "Available commands:\n" +
                        "!commands - show command list\n" +
                        "!about - about this app\n" +
                        "!who - list connected users\n" +
                        "!whisper [username] [message] - send a private message\n" +
                        "!time - show server date/time\n" +
                        "!exit - leave the chat\n" +
                        "!kick [username] - moderators only";

                    byte[] data = Encoding.ASCII.GetBytes(commandList);
                    currentClientSocket.socket.Send(data);
                    AddToChat("Commands sent to client");
                    break;

                // Server should send information back to the client about its creator, purpose and year of development
                case "!about":

                    string about =
                        "NDS203 TCP Chat Application\n" +
                        "Created by: Chun Cheum\n" +
                        "Info: A TCP client/server chat app\n" +
                        "Year: 2026";

                    byte[] data2 = Encoding.ASCII.GetBytes(about);
                    currentClientSocket.socket.Send(data2);

                    break;

                // When the server receives the !who command
                // it sends back a list of all connected usernames to the requesting client
                case "!who":

                    string users = GetConnectedUsers();

                    byte[] whoMsg = Encoding.ASCII.GetBytes(users);
                    currentClientSocket.socket.Send(whoMsg);
                    break;

                // Create a message showing the server's current time
                case "!time":

                    string timeText = "Server date/time: " + DateTime.Now.ToString("dddd, dd MMMM yyyy hh:mm tt");
                    byte[] timeMsg = Encoding.ASCII.GetBytes(timeText);

                    // Send the time only to the client who asked
                    currentClientSocket.socket.Send(timeMsg);
                    break;


                // Client wants to exit gracefully
                case "!exit":
                    // Store username before removing client
                    string exitingUser = string.IsNullOrWhiteSpace(currentClientSocket.username) ? "A client"
                        : currentClientSocket.username;

                    try
                    {
                        // Tell the client they are being disconnected
                        byte[] exitMsg = Encoding.ASCII.GetBytes("You have left the chat");
                        currentClientSocket.socket.Send(exitMsg);

                        // Safely close the socket
                        currentClientSocket.socket.Shutdown(SocketShutdown.Both);
                        currentClientSocket.socket.Close();
                    }
                    catch
                    {
                        // Ignore errors if socket already closed
                    }

                    // Remove client from connected list
                    clientSockets.Remove(currentClientSocket);

                    // Notify server and remaining clients
                    string msg = serverName + ": " + exitingUser + " has left the chat.";

                    AddToChat(msg);
                    SendToAll(msg, null);

                    //AddToChat(serverName + ": " + exitingUser + " has left the chat.");
                    //SendToAll(serverName + ": " + exitingUser + " has left the chat.", null);

                    return;

                // Sends a private message to a specified user
                case "!whisper":

                    if (param.Length > 2)
                    {
                        // Username of the person receiving the whisper
                        string targetUser = param[1].Trim();

                        // removes "!whisper" and the username from the message
                        string privateMessage = string.Join(" ", param, 2, param.Length - 2).Trim();

                        // Make sure the message is not empty
                        if (privateMessage == "")
                        {
                            byte[] emptyMsg = Encoding.ASCII.GetBytes("Usage: !whisper [username] [message]");
                            currentClientSocket.socket.Send(emptyMsg);
                            break;
                        }

                        // Whisper message for receiver
                        string whisperMsg = "[Whisper from " + currentClientSocket.username + "]: " + privateMessage;

                        // Try to send whisper to the targer user
                        bool sent = SendToUsername(targetUser, whisperMsg);

                        // If whisper was sent, tell sender
                        if (sent)
                        {
                            // confirm to sender that the whisper was sent
                            byte[] confirmMsg = Encoding.ASCII.GetBytes("[Whisper to " + targetUser + "]: " + privateMessage);
                            currentClientSocket.socket.Send(confirmMsg);
                        }
                        else
                        {
                            // tell sender if target user was not found
                            byte[] failMsg = Encoding.ASCII.GetBytes("User not found: " + targetUser);
                            currentClientSocket.socket.Send(failMsg);
                        }
                    }

                    else
                    {
                        // If command is typed incorrectly
                        byte[] failMsg = Encoding.ASCII.GetBytes("Usage: !whisper [username] [message]");
                        currentClientSocket.socket.Send(failMsg);
                    }
                    break;

                case "!kick":
                    // Only moderators can remove a user from the chat
                    if (currentClientSocket.moderator == true)
                    {
                        // example !kick Bob
                        if (param.Length > 1)
                        {
                            string targetUser = param[1].Trim();

                            // Try to kick target user
                            bool kicked = KickUser(targetUser, currentClientSocket.username);

                            // Notify if target user was not found
                            if (!kicked)
                            {
                                byte[] failMsg = Encoding.ASCII.GetBytes("User not found: " + targetUser);
                                currentClientSocket.socket.Send(failMsg);
                            }
                        }
                        else
                        {
                            // If command is missing username
                            byte[] failMsg = Encoding.ASCII.GetBytes("Usage: !kick [username]");
                            currentClientSocket.socket.Send(failMsg);
                        }
                    }
                    else
                    {
                        // If a user tries to kick
                        byte[] failMsg = Encoding.ASCII.GetBytes("You are not a moderator.");
                        currentClientSocket.socket.Send(failMsg);
                    }
                    break;

                case "!login":

                    // Already logged in, can't login again
                    if (currentClientSocket.state != ClientState.LOGIN)
                    {
                        SendString("You are already logged in. You can start chatting or use !join to start a game.", currentClientSocket);
                        break;
                    }

                    // Example: !login Bob password123
                    if (param.Length > 2)
                    {
                        // Get username and password from the command
                        string username = param[1].Trim();
                        string password = param[2].Trim();

                        // Check database for matching username and password
                        bool loggedIn = DatabaseAccess.DoesUserExist(username, password);

                        if (loggedIn)
                        {
                            // Save username to this client
                            currentClientSocket.username = username;

                            // Move client from LOGIN state to CHATTING state
                            currentClientSocket.usernameAccepted = true;

                            // Change server-side state and notify client
                            // CurrentClientSocket.state = ClientState.CHATTING;
                            UpdateClientState(currentClientSocket, ClientState.CHATTING);

                            // Tell client login worked
                            byte[] successMsg = Encoding.ASCII.GetBytes(
                                "Login successful. Welcome " + username + "!"
                            );
                            currentClientSocket.socket.Send(successMsg);

                            // Show login on server window
                            AddToChat(username + " logged in.");
                        }
                        else
                        {
                            // Tell client login failed
                            byte[] failMsg = Encoding.ASCII.GetBytes(
                                "Invalid username or password. Please try again."
                            );
                            currentClientSocket.socket.Send(failMsg);
                        }
                    }
                    else
                    {
                        // Missing username or password
                        byte[] failMsg = Encoding.ASCII.GetBytes(
                            "Please enter a username and password in the login window."
                        );
                        currentClientSocket.socket.Send(failMsg);
                    }
                    break;

                case "!register":
                    // Example: !register Bob password123
                    if (param.Length > 2)
                    {
                        // Get username and password from the command
                        string username = param[1].Trim();
                        string password = param[2].Trim();

                        // Try to add the new user to the database
                        bool registered = DatabaseAccess.AddUser(username, password);

                        if (registered)
                        {
                            //// Tell client registration worked
                            //byte[] successMsg = Encoding.ASCII.GetBytes(
                            //    "Registration successful. You can now login using !login [username] [password]."
                            //);

                            byte[] successMsg = Encoding.ASCII.GetBytes(
                            "Registration successful. Please login with your username and password."
                            );

                            currentClientSocket.socket.Send(successMsg);


                            // Show registration on server window
                            AddToChat("New user registered: " + username);
                        }
                        else
                        {
                            // Tell client registration failed, usually because username already exists
                            byte[] failMsg = Encoding.ASCII.GetBytes(
                                "Registration failed. Username may already exist."
                            );

                            currentClientSocket.socket.Send(failMsg);
                        }
                    }
                    else
                    {
                        // Command was missing username or password
                        byte[] failMsg = Encoding.ASCII.GetBytes(
                            "Please enter a username and password to login."
                        );

                        currentClientSocket.socket.Send(failMsg);
                    }
                    break;

                case "!join":
                   
                    // Is client in chatting state?
                    if (currentClientSocket.state != ClientState.CHATTING)
                    {
                        byte[] data9 = Encoding.ASCII.GetBytes("You must be logged in before joining the game.");
                        currentClientSocket.socket.Send(data9);
                        break;
                    }

                    // Check if a player position is available
                    // if player 1 and player 2 is not assigned
                    if (player1 != null && player2 != null)
                    {
                        //to stop a third player joining when two players already in game
                        byte[] data10 = Encoding.ASCII.GetBytes("2 players alreay joined");
                        currentClientSocket.socket.Send(data10);
                        break;
                    }
                    else if (player1 == null)
                    {
                        // Join client as player 1
                        player1 = currentClientSocket.username;
                        UpdateClientState(currentClientSocket, ClientState.PLAYING);
                        byte[] data11 = Encoding.ASCII.GetBytes("!player1");
                        currentClientSocket.socket.Send(data11);
                    }
                    else if (player2 == null)
                    {
                        // Join client as player 2
                        player2 = currentClientSocket.username;
                        UpdateClientState(currentClientSocket, ClientState.PLAYING);
                        byte[] data12 = Encoding.ASCII.GetBytes("!player2");
                        currentClientSocket.socket.Send(data12);
                    }

                    // Start a game if both player 1 and player 2 are assigned
                    if (player1 != null && player2 != null)
                    {
                        currentTurn = player1; // Player 1 starts
                        ClearTicTacToe(currentClientSocket);
                        // To ensure each commands come through separately
                        // can you implement a more robust solution to this ?
                        Task.Delay(200).ContinueWith(t => SendToAll("GAME START!! " + player1 + " (cross) vs. " + player2 + " (naught)", null));
                        Task.Delay(300).ContinueWith(t => SetPlayerTurn(currentTurn, currentClientSocket));
                    }
                    

                    break;

                case "!scores":
                    string scores = DatabaseAccess.GetScores();
                    SendString(scores, currentClientSocket);

                    break;

                case "!move":
                    if (param.Length == 2)
                    {
                        int tile = int.Parse(param[1]);
                        TileType type = TileType.blank;
                        if (currentClientSocket.username.Equals(player1))
                        {
                            type = TileType.cross;
                            AddToChat("Player 1 (cross) attempts move at " + tile + "!");
                        }
                        else if (currentClientSocket.username.Equals(player2))
                        {
                            type = TileType.naught;
                            AddToChat("Player 2 (naught) attempts move at " + tile + "!");
                        }
                        else
                        {
                            // If client attempting move is not player, abort
                            break;
                        }
                        bool validmove = ticTacToe.SetTile(tile, type);
                        if (validmove)
                        {
                            // 1. Update the game board
                            // 2. Send updated board to all clients
                            string gameboard = ticTacToe.GridToString();
                            SendToAll("!board " + gameboard, null);
                            // 3. Check if game is over...
                            GameState gs = ticTacToe.GetGameState();
                            if (gs == GameState.playing)
                            {
                                // 4. If not over, begin next player's turn
                                if (currentTurn.Equals(player1))
                                {
                                    currentTurn = player2;
                                    Task.Delay(100).ContinueWith(t => SetPlayerTurn(player2, currentClientSocket));
                                }
                                else
                                {
                                    currentTurn = player1;
                                    Task.Delay(100).ContinueWith(t => SetPlayerTurn(player1, currentClientSocket));
                                }
                            }
                            else
                            {
                                // Check who won or if it's a draw
                                if (gs == GameState.crossWins)
                                {
                                    // Delay slightly so message order is clean across clients
                                    Task.Delay(50).ContinueWith(t =>
                                    {
                                        SendToUsername(player1, "Yayy! You've won!");
                                        SendToUsername(player2, "You've lost. " + player1 + " won the game.");
                                        SendToAll("GAME END " + player1 + " (cross) wins!", null);
                                    });
                                   

                                    // Update database:
                                    // Database: cross user won
                                    // Database: naught user lost
                                    DatabaseAccess.UserWon(player1);
                                    DatabaseAccess.UserLost(player2);
                                }
                                else if (gs == GameState.naughtWins)
                                {
                                    Task.Delay(50).ContinueWith(t =>
                                    {
                                        SendToUsername(player2, "Yayy! You've won!");
                                        SendToUsername(player1, "You've lost. " + player2 + " won the game.");
                                        SendToAll("GAME END " + player2 + " (cross) wins!", null);
                                    });
                                   
                                    // Update database: Player 2 won, Player 1 lost
                                    // Database: naught user won
                                    // Database: cross user lost
                                    DatabaseAccess.UserWon(player2);
                                    DatabaseAccess.UserLost(player1);
                                }
                                else if (gs == GameState.draw)
                                {
                                    Task.Delay(50).ContinueWith(t =>
                                    {
                                        SendToUsername(player1, "Game ended in a draw");
                                        SendToUsername(player2, "Game ended in a draw");
                                        SendToAll("GAME END: It's a draw!", null);
                                    });

                                    // Update database: Both players get a draw
                                    // Database: naught user draw
                                    // Database: cross user draw
                                    DatabaseAccess.UserDrew(player1);
                                    DatabaseAccess.UserDrew(player2);
                                }
                                // Reset game state and send players back to chatting state
                                // End the game and return players to chatting state
                                // Delay state change so result message appears first
                                Task.Delay(150).ContinueWith(t => EndTicTacToe());

                                // TODO: Update Database with scores
                                //DatabaseAccess.UserWon(player1);
                            }

                        }
                        else
                        {
                            AddToChat("Move is Invalid. Try again.");
                            // 1. Report invalid move
                            SendString("Invalid Move. Try agian.", currentClientSocket);
                            // 2. Repeat player's turn
                            SendString("!yourturn", currentClientSocket);
                        }
                    }
                    else
                    {
                        // Incorrect number of parameter.
                        SendString("Usage: !move [0-8]", currentClientSocket);
                    }
                    break;
                default:

                    // Not logged in
                    if (currentClientSocket.state == ClientState.LOGIN)
                    {
                        SendString("Please login before chatting.", currentClientSocket);
                        break;
                    }

                    // Playing a game, can't chat
                    if (currentClientSocket.state == ClientState.PLAYING)
                    {
                        SendString("You are currently in a game. Please finish the game before chatting.", currentClientSocket);
                        break;
                    }

                    // normal message broadcast out to all clients
                    string chatmsg = currentClientSocket.username + ": " + text;

                    SendToAll(chatmsg, null);
                    AddToChat(chatmsg);
                    break;
            }

            //we just received a message from this socket, better keep an ear out with another thread for the next one
            currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentClientSocket);
        }

        public void EndTicTacToe()
        {
            foreach (ClientSocket c in clientSockets)
            {
                if (c.username.Equals(player1) || c.username.Equals(player2))
                {
                    UpdateClientState(c, ClientState.CHATTING);
                }
            }
            currentTurn = null;
            player1 = null;
            player2 = null;
        }

        public void SendToAll(string str, ClientSocket from)

        {
            foreach (ClientSocket c in clientSockets)
            {
                // if from is null, send to everyone
                // otherwise send to everyone except the sender
                if (from == null || !from.socket.Equals(c.socket))
                {
                    byte[] data = Encoding.ASCII.GetBytes(str);
                    c.socket.Send(data);
                }
            }
        }

        public bool SendToUsername(string user, string msg)
        {
            // Loop through connected clients
            foreach (ClientSocket c in clientSockets)
            {
                // Skip clients without usernames
                if (string.IsNullOrWhiteSpace(c.username))
                    continue;

                // Check if this is the target user
                if (c.username.Equals(user, StringComparison.OrdinalIgnoreCase))
                {
                    // Convert message to bytes
                    byte[] data = Encoding.ASCII.GetBytes(msg);

                    // Send message to that one user only
                    c.socket.Send(data);

                    // Return true because message was sent
                    return true;
                }
            }

            // Return false if no user was found
            return false;
        }

        // METHOD: Kicks a selected user from the chat
        public bool KickUser(string targetUser, string kickedBy)
        {
            // Loop backwards removing from list 
            for (int i = clientSockets.Count - 1; i >= 0; i--)
            {
                ClientSocket c = clientSockets[i];

                // Skip clients without usernames
                if (string.IsNullOrWhiteSpace(c.username))
                    continue;

                // Find the user to kick
                if (c.username.Equals(targetUser, StringComparison.OrdinalIgnoreCase))
                {
                    c.kicked = true;

                    // Tell kicked user
                    byte[] kickMsg = Encoding.ASCII.GetBytes("You have been kicked from the chat.");
                    c.socket.Send(kickMsg);

                    // Close their connection
                    c.socket.Close();

                    // Remove from connected clients list
                    clientSockets.RemoveAt(i);

                    // Message for server and other clients
                    string publicMsg = serverName + ": " + targetUser + " was kicked out by " + kickedBy;

                    // Show in server chat window
                    AddToChat(publicMsg);

                    // Find the user who kicked them
                    ClientSocket kicker = FindUser(kickedBy);

                    if (kicker != null)
                    {
                        // Personal message for the moderator who kicked the user
                        byte[] personalMsg = Encoding.ASCII.GetBytes(
                        serverName + ": " + targetUser + " was kicked out by you."
                        );

                        kicker.socket.Send(personalMsg);

                        // Send message to everyone except the kicker
                        SendToAll(publicMsg, kicker);
                    }
                    else
                    {
                        // if server performed the kick, send message to all 
                        SendToAll(publicMsg, null);
                    }

                    return true;
                }
            }

            // User was not found
            return false;
        }

        public bool CheckUsernameExists(string user, ClientSocket currentClientSocket)
        {
            foreach (ClientSocket c in clientSockets)
            {
                // Skip the client currently asking for this username
                if (c == currentClientSocket)
                    continue;

                // Ignore empty usernames
                if (string.IsNullOrWhiteSpace(c.username))
                    continue;

                // Compare usernames without caring about upper/lower case
                if (c.username.Equals(user, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public string GetConnectedUsers()
        {
            // Create a list to store usernames
            List<string> usernames = new List<string>();

            // Loop through every connected client
            foreach (ClientSocket c in clientSockets)
            {
                // Only add clients that have a username
                if (!string.IsNullOrWhiteSpace(c.username))
                {
                    usernames.Add(c.username);
                }
            }

            if (usernames.Count == 0)
                return "No users connected.";

            // return usernames as one readable sentence
            return "Connected users:\n- " + string.Join("\n- ", usernames);
        }

        public string GetModerators()
        {
            List<string> moderators = new List<string>();

            foreach (ClientSocket c in clientSockets)
            {
                if (c.moderator == true && !string.IsNullOrWhiteSpace(c.username))
                {
                    moderators.Add(c.username);
                }
            }

            if (moderators.Count == 0)
            {
                return serverName + ": No moderators assigned.";
            }

            return serverName + ": Moderators: " + string.Join(", ", moderators);
        }

        public ClientSocket FindUser(string username)
        {
            foreach (ClientSocket c in clientSockets)
            {
                if (!string.IsNullOrWhiteSpace(c.username) &&
                    c.username.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    return c;
                }
            }
            return null;
        }

        public void SetPlayerTurn(string username, ClientSocket currentClientSocket)
        {
            SendToUsername(username, "!yourturn");
        }

        public void UpdateClientState(ClientSocket client, ClientState state)
        {
            client.state = state;
            byte[] data8 = Encoding.ASCII.GetBytes("!state " + (int)client.state);
            client.socket.Send(data8);
        }

        public void SendString(string str, ClientSocket currentClientSocket)
        {
            byte[] data = Encoding.ASCII.GetBytes(str);
            currentClientSocket.socket.Send(data);
        }



        public void ClearTicTacToe(ClientSocket currentClientSocket)
        {
            ticTacToe.ResetBoard();
            Task.Delay(25).ContinueWith(t => SendToAll("!board " + ticTacToe.GridToString(), currentClientSocket));
        }

        public void ServerMessage(string msg, ClientSocket exclude = null)
        {
            string formatted = serverName + ": " + msg;

            AddToChat(formatted);
            SendToAll(formatted, exclude);
        }

    }
}
