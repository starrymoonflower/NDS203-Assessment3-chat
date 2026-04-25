using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{

    public class TCPChatBase
    {
        public RichTextBox chatTextBox;
        public int port;
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
                chatTextBox.SelectionColor = Color.Black;
                chatTextBox.AppendText(str);
                chatTextBox.AppendText(Environment.NewLine);
            });

        }
    }
}
