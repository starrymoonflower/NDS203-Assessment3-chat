using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.SQLite;
using System.Windows.Forms;

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

        // kicked user should not cause unexpected disconnect
        public bool kicked;

        //SQLite connection variable
        public SQLiteConnection dbConnection;

        public static TCPChatServer createInstance(int port, RichTextBox chatTextBox)
        {
            TCPChatServer tcp = null;
            //setup if port within range and valid chat box given
            if (port > 0 && port < 65535 && chatTextBox != null)
            {
                tcp = new TCPChatServer();
                tcp.port = port;
                tcp.chatTextBox = chatTextBox;
            }

            //return empty if user not enter useful details
            return tcp;
        }

        //METHOD: 
        public void SetupServer()
        {
            //chatTextBox.Text += "Setting up server...\n";
            AddToChat("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(0);
            //kick off thread to read connecting clients, when one connects, it'll call out AcceptCallback function
            serverSocket.BeginAccept(AcceptCallback, this);


            //setup database when server starts
            //create and open database connection
            dbConnection = new SQLiteConnection("Data Source=chat.db;Version=3;");
            dbConnection.Open();

            AddToChat("DB path: " + System.IO.Path.GetFullPath("chat.db"));

            // Create Users table if it doesn't exist
            string createTable = @"CREATE TABLE IF NOT EXISTS Users (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT UNIQUE,
                Password TEXT,
                Wins INTEGER DEFAULT 0,
                Losses INTEGER DEFAULT 0,
                Draws INTEGER DEFAULT 0
            
            );";

            SQLiteCommand cmd = new SQLiteCommand(createTable, dbConnection);
            cmd.ExecuteNonQuery();











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
                                    string msg = $"[Server - {serverName}]: {c.username} has been promoted to Moderator.";

                                    AddToChat(msg);

                                    // Send to everyone except this user
                                    SendToAll(msg, c);

                                    // personalised message for the target user
                                    byte[] data = Encoding.ASCII.GetBytes("[Server - " + serverName + "]: You have been promoted to Moderator.");
                                    c.socket.Send(data);
                                }
                                else
                                {
                                    // Client demoted (message for everyone)
                                    string msg = $"[Server - {serverName}]: {c.username} has been demoted from Moderator.";

                                    AddToChat(msg);

                                    // Send to all except this user
                                    SendToAll(msg, c);

                                    // personalised message for the target user
                                    byte[] data = Encoding.ASCII.GetBytes("[Server - " + serverName + "]: You have been demoted from Moderator.");
                                    c.socket.Send(data);
                                }

                                // We found the user and handled the command, so exit the method
                                return true;
                            }
                        }

                        // If loop finished and no matching username was found
                        AddToChat("Server: User not found: " + target);
                    }
                    else
                    { 
                        // If server typed !mod without a username
                        AddToChat("Usage: !mod [username]");
                    }

                    return true;

                case "!kick":
                    // Server can kick any connected user
                    if (param.Length > 1)
                    {
                        string target = param[1].Trim();

                        bool kicked = KickUser(target, "[Server - " + serverName + "]");
                        if (!kicked)
                        {
                            AddToChat("[Server - " +  serverName + "]: User not found: " + target);
                        }
                    }
                    else
                    {
                        AddToChat("[Server - " + serverName + "]: Usage: !kick [username]");
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
                    AddToChat("[Server - " + serverName + "]: " + GetConnectedUsers());
                    return true;

                case "!time":
                    AddToChat("[Server - " + serverName + "]: " + DateTime.Now.ToString("dddd, dd MMMM yyyy hh:mm tt"));
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
                    AddToChat("[Server - " + serverName + "]: A client disconnected unexpectedly.");
                }
                
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                currentClientSocket.socket.Close();
                clientSockets.Remove(currentClientSocket);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);

            string[] param = text.Split(' ');
            string command = param[0].ToLower();

            switch (command)
            {
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

                    byte[] whoMsg =Encoding.ASCII.GetBytes(users);
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
                    AddToChat("[Server - " + serverName + "]: " + exitingUser + " has left the chat.");
                    SendToAll("[Server - " + serverName + "]: " + exitingUser + " has left the chat.", null);
                    
                    return;
                
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
                            AddToChat("[Server - " + serverName + "]: User with name " + username + " connected!");

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
                            AddToChat("[Server - " + serverName + "]: Username already taken: " + username);
                        }
                    }
                    else
                    {
                        byte[] failMsg = Encoding.ASCII.GetBytes("Usage: !username [name]");
                        currentClientSocket.socket.Send(failMsg);
                    }
                    break;

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

                    default:
                    // normal message broadcast out to all clients
                    // If username not set yet show Unknown
                    // otherwise use the client's actual username
                    

                    // Prevent chatting until username has been accepted
                    if (currentClientSocket.usernameAccepted == false)
                    {
                        byte[] failMsg = Encoding.ASCII.GetBytes("Please choose a valid username first using !username [name].");
                        currentClientSocket.socket.Send(failMsg);
                        break;
                    }

                    // User the accepted username
                    string name = currentClientSocket.username;

                    string msg = name + ": " + text;

                    // Display the message on the server chat window too
                    AddToChat(msg);

                    // Send the message to all connected clients
                    SendToAll(msg, null);
                    break;

                case "!register":

                    // !register Bob password123
                    if (param.Length > 2)
                    {
                        string username = param[1].Trim();
                        string password = param[2].Trim();

                        bool registered = RegisterUser(username, password);

                        if (registered)
                        {
                            byte[] successMsg = Encoding.ASCII.GetBytes("Registration successful. " +
                                "You can now login using !login [username] [password].");
                            currentClientSocket.socket.Send(successMsg);
                        }
                        else
                        {
                            byte[] failMsg = Encoding.ASCII.GetBytes("Registration failed. Try registering with a different username.");
                            currentClientSocket.socket.Send(failMsg);

                        }
                    }
                  
                    else
                    {
                        byte[] failMsg = Encoding.ASCII.GetBytes("Usage: !register [username] [password]");
                        currentClientSocket.socket.Send(failMsg);
                    }
                    break;
            }

            //we just received a message from this socket, better keep an ear out with another thread for the next one
            currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentClientSocket);
        }

        public void SendToAll(string str, ClientSocket from)
       
        {
            foreach(ClientSocket c in clientSockets)
            {
                // if from is null, send to everyone
                // otherwise send to everyone except the sender
                if(from == null || !from.socket.Equals(c.socket))
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
                    string publicMsg = "[Server - " + serverName + "]: " + targetUser + " was kicked out by " + kickedBy;

                    // Show in server chat window
                    AddToChat(publicMsg);

                    // Find the user who kicked them
                    ClientSocket kicker = FindUser(kickedBy);

                    if (kicker != null)
                    {
                        // Personal message for the moderator who kicked the user
                        byte[] personalMsg = Encoding.ASCII.GetBytes(
                        "[Server - " + serverName + "]: " + targetUser + " was kicked out by you."
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
                if (!string.IsNullOrWhiteSpace (c.username))
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
                return "[Server - " + serverName + "]: No moderators assigned.";
            }

            return "[Server - " + serverName + "]: Moderators: " + string.Join(", ", moderators);
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

        // METHOD: Register a user
        public bool RegisterUser(string username, string password)
        {
            try
            {
                string sql = @"
                    INSERT INTO Users (Username, Password, Wins, Losses, Draws)
                    VALUES (@username, @password, 0, 0, 0);
                ";

                SQLiteCommand cmd = new SQLiteCommand(sql, dbConnection);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                int rows = cmd.ExecuteNonQuery();

                return rows > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
