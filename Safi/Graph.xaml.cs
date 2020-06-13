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
using System.Windows.Controls.DataVisualization.Charting;

namespace Safi
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class AutoFocusGraph : Window
    {
        private MainWindow m_mainWnd;  // Set by caller to Init()
        private List<KeyValuePair<int, double>> m_dataSource;  // Set by caller to DataSource property

        #region Properties
        public List<KeyValuePair<int, double>> DataSource
        {
            get
            {
                return m_dataSource;
            }
            set
            {
                m_dataSource = value;
                ((LineSeries)autoFocusLineGraph.Series[0]).ItemsSource = m_dataSource;
            }
        }

        public LinearAxis XAxis { get { return this.xAxis; } }
        public LinearAxis YAxis { get { return this.yAxis; } }

        #endregion

        public AutoFocusGraph()
        {
            InitializeComponent();
        }

        public void Init(MainWindow mainWnd)
        {
            m_mainWnd = mainWnd;

            this.Top = m_mainWnd.AppSettings.TopGraph;
            this.Left = m_mainWnd.AppSettings.LeftGraph;
            this.Topmost = m_mainWnd.AppSettings.OntopGraph;
            this.Width = m_mainWnd.AppSettings.WidthGraph;
            this.Height = m_mainWnd.AppSettings.HeightGraph;

            xAxis.Maximum = m_mainWnd.AppSettings.MaxIn;
            xAxis.Minimum = -(m_mainWnd.AppSettings.MaxOut);
            YAxis.Maximum = m_mainWnd.AppSettings.MaxHFD;
            YAxis.Minimum = m_mainWnd.AppSettings.MinHFD;
        }

        public void RefreshData()
        {
            ((LineSeries)autoFocusLineGraph.Series[0]).Refresh();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_mainWnd.AppSettings.LeftGraph = this.Left;
            m_mainWnd.AppSettings.OntopGraph = this.Topmost;
            m_mainWnd.AppSettings.TopGraph = this.Top;
            m_mainWnd.AppSettings.WidthGraph = this.Width;
            m_mainWnd.AppSettings.HeightGraph = this.Height;
        }
    }
}
