using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Data.SQLite;


namespace Windows_Forms_Chat
{   
    public enum ClientState
    {
        // define three value or state 
        LOGIN,
        CHATTING,
        PLAYING
    }

    // Represents a single connected client on the server side 
    // Stores info about the client - socket connection, username, moderator status
    // and data buffer for receiving messages. 
    public class ClientSocket
    {
        //add other attributes to this, e.g username, what state the client is in etc

        // login state - a client will default to that when log in
        public ClientState state = ClientState.LOGIN;
        public bool usernameAccepted = false;
        public bool kicked;
        public bool moderator;
        public string username;
        public Socket socket;
        public const int BUFFER_SIZE = 2048;
        public byte[] buffer = new byte[BUFFER_SIZE];
    }
}
 