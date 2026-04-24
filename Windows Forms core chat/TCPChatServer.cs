using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

//https://github.com/AbleOpus/NetworkingSamples/blob/master/MultiServer/Program.cs
namespace Windows_Forms_Chat
{
    public class TCPChatServer : TCPChatBase
    {
        
        public Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //connected clients
        public List<ClientSocket> clientSockets = new List<ClientSocket>();

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

        public void SetupServer()
        {
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
                                    string msg = $"Server: {c.username} has been promoted to Moderator.";

                                    AddToChat(msg);

                                    // Send to everyone except this user
                                    SendToAll(msg, c);

                                    // personalised message for the target user
                                    byte[] data = Encoding.ASCII.GetBytes("Server: You have been promoted to Moderator.");
                                    c.socket.Send(data);
                                }
                                else
                                {
                                    // Client demoted (message for everyone)
                                    string msg = $"Server: {c.username} has been demoted from Moderator.";

                                    AddToChat(msg);

                                    // Send to all except this user
                                    SendToAll(msg, c);

                                    // personalised message for the target user
                                    byte[] data = Encoding.ASCII.GetBytes("Server: You have been demoted from Moderator.");
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
                AddToChat("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                currentClientSocket.socket.Close();
                clientSockets.Remove(currentClientSocket);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);

            //AddToChat( text );

            //string[] param = text.ToLower().Split(' ');
            string[] param = text.Split(' ');
            string command = param[0].ToLower();


            //switch (param[0])
            switch (command)

            {
                case "!kick":
                    // Only moderators can kick
                    if (currentClientSocket.moderator == true)
                    {
                        // example !kick Bob
                        if (param.Length > 1)
                        {
                            string targetUser = param[1].Trim();

                            // Try to kick target user
                            bool kicked = KickUser(targetUser, currentClientSocket.username);

                            // If user was not found
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
                
                    // Client requested time
                case "!commands": 
                    byte[] data = Encoding.ASCII.GetBytes("Commands are !commands !about !who !whisper [username] [message] !time !exit !kick [username]");
                    currentClientSocket.socket.Send(data);
                    AddToChat("Commands sent to client");
                    break;

                // Server should send information back to the client about its creator, purpose and year of development
                case "!about":
                    byte[] data2 = Encoding.ASCII.GetBytes("This is the NDS203 Assessment2");
                    currentClientSocket.socket.Send(data2);

                    break;

                // When server recieves this message, its sends back messages containing the names of the
                // connected users to the client to be output to the chat window
                case "!who":
                    // Get a list of all connected usernames
                    string users = GetConnectedUsers();

                    // Convert the text into bytes so it can be sent through the socket
                    byte[] whoMsg =Encoding.ASCII.GetBytes(users);

                    // Send the list only to the client who asked
                    currentClientSocket.socket.Send(whoMsg);
                    break;

                case "!time":
                    // Create a message showing the server's current time
                    byte[] timeMsg = Encoding.ASCII.GetBytes("Server time: " + DateTime.Now.ToString());

                    // Send the time only to the client who asked
                    currentClientSocket.socket.Send(timeMsg);

                    break;


                // Client wants to exit gracefully
                case "!exit":
                    // Always Shutdown before closing
                    currentClientSocket.socket.Shutdown(SocketShutdown.Both);
                    currentClientSocket.socket.Close();
                    clientSockets.Remove(currentClientSocket);
                    AddToChat("Client disconnected");
                    return;
                /*case "!username": // !username [new_username] e.g !username Bob 
                    if (param.Length > 1)
                    {
                        string username = param[1].Trim();
                        currentClientSocket.username = username;


                        // Check if username is already in use or not taken and allow it
                        if (!CheckUsernameExists(username, currentClientSocket))
                        {

                            // If not in use, assign and report success
                            currentClientSocket.username = username;

                            // Tell server window
                            AddToChat("User with name " + username + " connected!");

                            // Tell the client it worked
                            byte[] successMsg = Encoding.ASCII.GetBytes("Username accepted");
                            currentClientSocket.socket.Send(successMsg);
                        }
                        else
                        {
                            // If is in use, report failure and disconnect client
                            byte[] failMsg = Encoding.ASCII.GetBytes("Username already taken");
                            currentClientSocket.socket.Send(failMsg);

                            // Disconnect client
                            currentClientSocket.socket.Close( );
                            clientSockets.Remove(currentClientSocket);

                            AddToChat("Duplicate username - client disconnected");
                            return;
                        }
                    }
                    */
                case "!username":
                    if (param.Length > 1)
                    {
                        // Read the username the client wants
                        string username = param[1].Trim();

                        // Check if another client is already using it
                        if (!CheckUsernameExists(username, currentClientSocket))
                        {
                            // Only save it AFTER validation succeeds
                            currentClientSocket.username = username;

                            // Tell the server window
                            AddToChat("User with name " + username + " connected!");

                            // Tell the client it worked
                            byte[] successMsg = Encoding.ASCII.GetBytes("Username accepted");
                            currentClientSocket.socket.Send(successMsg);
                        }
                        else
                        {
                            // Tell the client it failed
                            byte[] failMsg = Encoding.ASCII.GetBytes("Username already taken");
                            currentClientSocket.socket.Send(failMsg);

                            // Disconnect the client
                            currentClientSocket.socket.Close();
                            clientSockets.Remove(currentClientSocket);

                            AddToChat("Duplicate username - client disconnected");
                            return;
                        }
                    }
                    break;

                    // sends a message to a specified user directly or lets sender know
                    // it cannot find anyone by that username
                    case "!whisper": 
                    // Check command has username and message
                    if (param.Length > 2)
                    {
                        // The person receiving the whisper
                        string targetUser = param[1].Trim();
                        string message = param[2].Trim();

                        // Join all words after username into one message
                        // removes !whisper and the username from the message
                        string privateMessage = string.Join(" ", param, 2, param.Length - 2);

                        // Format message for receiver
                        string whisperMsg = "[Whisper from " + currentClientSocket.username + "]: " + privateMessage;

                        // Try to send whisper
                        bool sent = SendToUsername(targetUser, whisperMsg);

                        // If whisper was sent, tell sender
                        if (sent)
                        {
                            byte[] confirmMsg = Encoding.ASCII.GetBytes("[Whisper to " + targetUser + "]: " + privateMessage);
                            currentClientSocket.socket.Send(confirmMsg);
                        }
                        else
                        {
                            // If target username does not exist
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


                    /*
                    // Check that target User exist 

                    // Send the whisper message to target
                    string msg2 = "[Whisper] " + currentClientSocket.username + ": " + message;
                        SendToUsername(user, msg2, currentClientSocket);
                    }
                    else
                    {
                        // Invalid command
                    }
                    break;
                    */


                    default:
                    //normal message broadcast out to all clients

                    // If username not set yet, show "Unknown"
                    string name = string.IsNullOrEmpty(currentClientSocket.username)? "Unknown": currentClientSocket.username;
                    //send the person username who we've received the message from
                    string msg = name + ": " + text;

                    // Show the formatted message on the server chat window too
                    AddToChat(msg);

                    //string msg = currentClientSocket.username + ": " + text;
                    SendToAll(msg, null);
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


        public bool KickUser(string targetUser, string kickedBy)
        {
            // Loop backwards so removing from list is safer
            for (int i = clientSockets.Count - 1; i >= 0; i--)
            {
                ClientSocket c = clientSockets[i];

                // Skip clients without usernames
                if (string.IsNullOrWhiteSpace(c.username))
                    continue;

                // Find the user to kick
                if (c.username.Equals(targetUser, StringComparison.OrdinalIgnoreCase))
                {
                    // Tell kicked user
                    byte[] kickMsg = Encoding.ASCII.GetBytes("You have been kicked from the chat.");
                    c.socket.Send(kickMsg);

                    // Close their connection
                    c.socket.Close();

                    // Remove from connected clients list
                    clientSockets.RemoveAt(i);

                    // Tell everyone what happened
                    string msg = "Server: " + targetUser + " was kicked out by " + kickedBy;
                    AddToChat(msg);
                    SendToAll(msg, null);

                    return true;
                }
            }

            // User was not found
            return false;
        }

        /*
        public void SendToUsername(string user, string msg, ClientSocket from)
        {
            foreach (ClientSocket c in clientSockets)
            {
                // if from is null, send to everyone
                // otherwise send to everyone except the sender
                if (from == null || !from.socket.Equals(c))
                {
                    if (c.username.Equals(user))
                    {
                        byte[] data = Encoding.ASCII.GetBytes(msg);
                        c.socket.Send(data);

                    }
                }
            }
        }
        */




        /*public bool CheckUsernameExists(string user)
        {
            foreach(ClientSocket c in clientSockets)
            {
                // fix nullreference error
                if(c.username != null && c.username == user)
                {
                    return true;
                }
            }

            return false;
        }
        */

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

            // return usernames as one readable sentence
            return "Connected users: " + string.Join(",", usernames);
        }


    }
}
