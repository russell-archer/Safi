using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Safi
{
    /// <summary>
    /// Utility class to display a dialog box centered on top of the specified window.
    /// Use the single public static method to show the window.
    /// Example usage: MessageBoxWindow.Show(this, "Test", "Test Msg");
    /// </summary>
    public partial class MessageBoxWindow : Window
    {
        private void Show(Window owner, string title, string msg, bool showDialog)
        {
            this.Top = (owner.Top + (owner.Height / 2)) -  (this.Height / 2);
            this.Left = (owner.Left + (owner.Width / 2)) - (this.Width / 2);

            if(!string.IsNullOrEmpty(title))
                this.Title = title;
            else
                this.Title = "SAFI";

            this.textBoxMsg.Text = msg;

            if(showDialog)
                this.ShowDialog();
            else
                this.Show();
        }

        public MessageBoxWindow()
        {
            InitializeComponent();
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public static void Show(Window owner, string title, string msg)
        {
            MessageBoxWindow tmpWnd = new MessageBoxWindow();
            tmpWnd.Show(owner, title, msg, true);
        }
    }
}
