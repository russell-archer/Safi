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
using System.ComponentModel;

namespace Safi
{
    /// <summary>
    /// Interaction logic for Nudge.xaml
    /// </summary>
    public partial class Nudge : Window, INotifyPropertyChanged
    {
        private MainWindow m_mainWnd;
        private int m_moveSize;
        public event PropertyChangedEventHandler PropertyChanged;

        public Nudge()
        {
            this.Topmost = true;
            InitializeComponent();
        }

        public void Init(MainWindow mainWnd)
        {
            m_mainWnd = mainWnd;

            this.Top = m_mainWnd.AppSettings.TopNudge;
            this.Left = m_mainWnd.AppSettings.LeftNudge;
            this.Topmost = m_mainWnd.AppSettings.OntopNudge;
            this.m_moveSize = mainWnd.AppSettings.MoveInOutDefault;

            // Data binding...
            textBoxMoveSize.DataContext = this;
        }

        public int MoveSize
        {
            get
            {
                return m_moveSize;
            }

            set
            {
                m_moveSize = value;
                this.OnPropertyChanged("MoveSize");
            }
        }

        private void buttonZero_Click(object sender, RoutedEventArgs e)
        {
            m_mainWnd.Focuser.Position = 0;
        }

        private void buttonMaxIn_Click(object sender, RoutedEventArgs e)
        {
            int relMove = m_mainWnd.AppSettings.MaxIn - m_mainWnd.Focuser.Position;
            m_mainWnd.Focuser.MoveFocuser(relMove);
        }

        private void buttonMoveIn_Click(object sender, RoutedEventArgs e)
        {
            m_mainWnd.Focuser.MoveFocuser(MoveSize, false);
        }

        private void buttonMoveOut_Click(object sender, RoutedEventArgs e)
        {
            m_mainWnd.Focuser.MoveFocuser(-MoveSize, false);
        }

        private void buttonMaxOut_Click(object sender, RoutedEventArgs e)
        {
            int relMove = -(m_mainWnd.AppSettings.MaxOut + m_mainWnd.Focuser.Position);
            m_mainWnd.Focuser.MoveFocuser(relMove);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_mainWnd.AppSettings.LeftNudge = this.Left;
            m_mainWnd.AppSettings.OntopNudge = this.Topmost;
            m_mainWnd.AppSettings.TopNudge = this.Top;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            // Raise property change events from this window
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void buttonGotoZero_Click(object sender, RoutedEventArgs e)
        {
            m_mainWnd.Focuser.MoveToZero();
        }
    }
}
