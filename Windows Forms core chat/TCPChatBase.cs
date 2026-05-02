using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;


namespace Windows_Forms_Chat
{

    public class TCPChatBase
    {
        public RichTextBox chatTextBox;
        public int port;

        public TicTacToe ticTacToe;

        public void SetChat(string str)
        {
            chatTextBox.Invoke((Action)delegate
            {
                chatTextBox.Text = str;
                chatTextBox.AppendText(Environment.NewLine);
            });
        }
        public void AddToChat(string str)
        {
            //dumb https://iandotnet.wordpress.com/tag/multithreading-how-to-update-textbox-on-gui-from-another-thread/
            chatTextBox.Invoke((Action)delegate
            {
                // Find the colon that separates username and message
                int colonIndex = str.IndexOf(':');

                if (colonIndex > 0)
                {
                    // Username before colon
                    string usernameText = str.Substring(0, colonIndex + 1);
                    string messageText = str.Substring(colonIndex + 1);

                    // Set username colour blue
                    chatTextBox.SelectionColor = Color.Blue;
                    chatTextBox.AppendText(usernameText);

                    // Set message colour black
                    chatTextBox.SelectionColor = Color.Black;
                    chatTextBox.AppendText(messageText);
                }
                else
                {
                    // if no colon just print normally
                    chatTextBox.SelectionColor = Color.Black;
                    chatTextBox.AppendText(str);
                }

                chatTextBox.AppendText(Environment.NewLine);

                chatTextBox.SelectionStart = chatTextBox.Text.Length;
                chatTextBox.ScrollToCaret();

            });
        }
    }
}
