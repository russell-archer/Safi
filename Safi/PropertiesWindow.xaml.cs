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
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        public MainWindow m_mainWnd;  // Set by caller
        private Brush m_brushTextBoxBorderDefault;
        public PropertiesWindow()
        {
            InitializeComponent();
            m_brushTextBoxBorderDefault = textBoxMaxInOut.BorderBrush;  // Save for later use in validation
        }

        public void Init()
        {
            if(m_mainWnd != null)
            {
                // Setup bindings...
                if(m_mainWnd.MaxIm != null)
                    comboBoxFilter.DataContext = m_mainWnd.MaxIm;   // XAML binding: IsEnabled="{Binding Path=MaximAndCameraConnected}"
                else
                    comboBoxFilter.IsEnabled = false;

                this.Top = (m_mainWnd.Top + (m_mainWnd.Height / 2)) - (this.Height / 2);
                this.Left = (m_mainWnd.Left + (m_mainWnd.Width / 2)) - (this.Width / 2);

                comboBoxBinSubFrame.SelectedIndex = 0;
                comboBoxBinFullFrame.SelectedIndex = m_mainWnd.AppSettings.BinFullFrame-1;
                textBoxMaxInOut.Text = m_mainWnd.AppSettings.MaxIn.ToString(); 
                textBoxStepSize.Text = m_mainWnd.AppSettings.StepSize.ToString();
                textBoxStepSizeNearFocus.Text = m_mainWnd.AppSettings.StepSizeNearFocus.ToString();
                textBoxStepSizeNearFocusMultiplier.Text = m_mainWnd.AppSettings.StepSizeNearFocusMultiplier.ToString();
                textBoxNImagesPerDataPoint.Text = m_mainWnd.AppSettings.NImagesPerDataPoint.ToString();
                textBoxNImagesPerDataPointNearFocus.Text = m_mainWnd.AppSettings.NImagesPerDataPointNearFocus.ToString();
                textBoxNRetries.Text = m_mainWnd.AppSettings.NRetryOnAutoFocusFail.ToString();
                textBoxExpFullFrame.Text = m_mainWnd.AppSettings.ExposureDurationFullFrame.ToString();
                textBoxExpSubFrame.Text = m_mainWnd.AppSettings.ExposureDurationSubFrame.ToString();
                comboBoxSubFrameSize.SelectedIndex = ((m_mainWnd.AppSettings.SubFrameSize / 50) - 1);  // It will be 50, 100, 150 or 200
                textBoxMinMaxPixel.Text = m_mainWnd.AppSettings.MinMaxPixel.ToString();

                if(m_mainWnd.MaxIm != null && m_mainWnd.MaxIm.MaximConnected && m_mainWnd.MaxIm.FilterNames != null)
                {
                    foreach(string filter in m_mainWnd.MaxIm.FilterNames)
                        comboBoxFilter.Items.Add(filter);

                    comboBoxFilter.SelectedIndex = (int)m_mainWnd.AppSettings.FilterToUse;
                }
            }
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int maxInOut = int.Parse(textBoxMaxInOut.Text);
                int stepSize = int.Parse(textBoxStepSize.Text);
                int stepSizeNearFocus = int.Parse(textBoxStepSizeNearFocus.Text);
                int stepSizeNearFocusMultiplier = int.Parse(textBoxStepSizeNearFocusMultiplier.Text);

                short filter = 0;
                if(m_mainWnd.MaxIm != null && m_mainWnd.MaxIm.MaximConnected)
                    filter = (short)comboBoxFilter.SelectedIndex;

                int nImages = int.Parse(textBoxNImagesPerDataPoint.Text);
                int nImagesNearFocus = int.Parse(textBoxNImagesPerDataPointNearFocus.Text);
                int nRetries = int.Parse(textBoxNRetries.Text);
                double expFull = double.Parse(textBoxExpFullFrame.Text);
                double expSub = double.Parse(textBoxExpSubFrame.Text);
                long minMaxPixel = long.Parse(textBoxMinMaxPixel.Text);

                if(maxInOut < 100 || maxInOut > 10000)
                {
                    MessageBoxWindow.Show(this, "Validation Error", "Value must be in the range 100..10,000");
                    textBoxMaxInOut.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));  // Red brush
                    return;
                }
                else
                {
                    textBoxMaxInOut.BorderBrush = m_brushTextBoxBorderDefault;
                }

                if(stepSize < 10 || stepSize > 1000)
                {
                    MessageBoxWindow.Show(this, "Validation Error", "Value must be in the range 10..1,000");
                    textBoxStepSize.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));  // Red brush
                    return;
                }
                else
                {
                    textBoxStepSizeNearFocus.BorderBrush = m_brushTextBoxBorderDefault;
                }

                if(stepSizeNearFocus < 1 || stepSizeNearFocus > 100)
                {
                    MessageBoxWindow.Show(this, "Validation Error", "Value must be in the range 1..100");
                    textBoxStepSizeNearFocus.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));  // Red brush
                    return;
                }
                else
                {
                    textBoxStepSizeNearFocus.BorderBrush = m_brushTextBoxBorderDefault;
                }

                if(stepSizeNearFocusMultiplier < 1 || stepSizeNearFocusMultiplier > 10)
                {
                    MessageBoxWindow.Show(this, "Validation Error", "Value must be in the range 1..10");
                    textBoxStepSizeNearFocusMultiplier.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));  // Red brush
                    return;
                }
                else
                {
                    textBoxStepSizeNearFocusMultiplier.BorderBrush = m_brushTextBoxBorderDefault;
                }

                if(nImages < 1 || nImages > 5)
                {
                    MessageBoxWindow.Show(this, "Validation Error", "Value must be in the range 1..5");
                    textBoxNImagesPerDataPoint.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));  // Red brush
                    return;
                }
                else
                {
                    textBoxNImagesPerDataPoint.BorderBrush = m_brushTextBoxBorderDefault;
                }

                if(nImagesNearFocus < 1 || nImagesNearFocus > 5)
                {
                    MessageBoxWindow.Show(this, "Validation Error", "Value must be in the range 1..5");
                    textBoxNImagesPerDataPointNearFocus.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));  // Red brush
                    return;
                }
                else
                {
                    textBoxNImagesPerDataPointNearFocus.BorderBrush = m_brushTextBoxBorderDefault;
                }

                if(nRetries < 1 || nRetries > 5)
                {
                    MessageBoxWindow.Show(this, "Validation Error", "Value must be in the range 1..5");
                    textBoxNRetries.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));  // Red brush
                    return;
                }
                else
                {
                    textBoxNRetries.BorderBrush = m_brushTextBoxBorderDefault;
                }

                if(expFull < 0.1 || expFull > 10)
                {
                    MessageBoxWindow.Show(this, "Validation Error", "Value must be in the range 0.1..10");
                    textBoxExpFullFrame.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));  // Red brush
                    return;
                }
                else
                {
                    textBoxExpFullFrame.BorderBrush = m_brushTextBoxBorderDefault;
                }

                if(expSub < 1 || expSub > 10)
                {
                    MessageBoxWindow.Show(this, "Validation Error", "Value must be in the range 1..10");
                    textBoxExpSubFrame.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));  // Red brush
                    return;
                }
                else
                {
                    textBoxExpSubFrame.BorderBrush = m_brushTextBoxBorderDefault;
                }

                if(minMaxPixel < 100 || minMaxPixel > Math.Pow(2.0, 16.0))
                {
                    MessageBoxWindow.Show(this, "Validation Error", "Value must be in the range 100..2^16");
                    textBoxMinMaxPixel.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));  // Red brush
                    return;
                }
                else
                {
                    textBoxMinMaxPixel.BorderBrush = m_brushTextBoxBorderDefault;
                }

                m_mainWnd.AppSettings.BinFullFrame = (short)(comboBoxBinFullFrame.SelectedIndex + 1);
                m_mainWnd.AppSettings.MaxIn = maxInOut;
                m_mainWnd.AppSettings.MaxOut = maxInOut;
                m_mainWnd.AppSettings.StepSize = stepSize;
                m_mainWnd.AppSettings.StepSizeNearFocus = stepSizeNearFocus;
                m_mainWnd.AppSettings.StepSizeNearFocusMultiplier = stepSizeNearFocusMultiplier;

                if(m_mainWnd.MaxIm.MaximConnected)
                    m_mainWnd.AppSettings.FilterToUse = filter;

                m_mainWnd.AppSettings.NImagesPerDataPoint = nImages;
                m_mainWnd.AppSettings.NImagesPerDataPointNearFocus = nImagesNearFocus;
                m_mainWnd.AppSettings.NRetryOnAutoFocusFail = nRetries;
                m_mainWnd.AppSettings.ExposureDurationFullFrame = expFull;
                m_mainWnd.AppSettings.ExposureDurationSubFrame = expSub;
                m_mainWnd.AppSettings.SubFrameSize = ((short)((comboBoxSubFrameSize.SelectedIndex + 1) * 50));
                m_mainWnd.AppSettings.MinMaxPixel = minMaxPixel;

                m_mainWnd.AppSettings.WriteConfig();  // Write and re-read the settings
                m_mainWnd.AppSettings.ReadConfig();
            }
            catch
            {
                MessageBoxWindow.Show(this, "", "Error saving properties");
            }

            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonDefaults_Click(object sender, RoutedEventArgs e)
        {
            if(m_mainWnd.MaxIm != null && m_mainWnd.MaxIm.MaximConnected)
                comboBoxFilter.SelectedIndex = 0;

            comboBoxBinFullFrame.SelectedIndex = 1;
            textBoxMaxInOut.Text = "1000";
            textBoxStepSize.Text = "100";
            textBoxStepSizeNearFocus.Text = "10";
            textBoxStepSizeNearFocusMultiplier.Text = "4";
            textBoxNImagesPerDataPoint.Text = "1";
            textBoxNImagesPerDataPointNearFocus.Text = "2";
            textBoxNRetries.Text = "3";
            textBoxExpFullFrame.Text = "2.5";
            textBoxExpSubFrame.Text = "1";
            comboBoxSubFrameSize.SelectedIndex = 1;
            textBoxMinMaxPixel.Text = "150";
        }
    }
}
