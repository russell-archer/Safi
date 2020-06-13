using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Safi
{
    /// <summary>
    /// This class encapsulates the MaxIm DL application interface
    /// </summary>
    public class MaxImDL : INotifyPropertyChanged
    {
        #region Declarations
        public event PropertyChangedEventHandler PropertyChanged;

        private MaxIm.Application m_maxim = null;
        private MaxIm.CCDCamera m_ccd = null;
        private MainWindow m_mainWnd;
        private string[] m_filterNames;     // MaxIm's list of filter names
        private static short LightExposure = 1;
        #endregion

        #region Construction
        private MaxImDL() 
        {
        }

        public MaxImDL(MainWindow mainWnd)
        {
            this.m_mainWnd = mainWnd;
        }
        #endregion

        #region Properties
        public MaxIm.Application Maxim  { get { return this.m_maxim; } }
        public MaxIm.CCDCamera Camera   { get { return this.m_ccd; } }
        public MainWindow MainWnd       { get { return this.m_mainWnd; } set { m_mainWnd = value; } }
        public string[] FilterNames     { get { return m_filterNames; } }

        public bool CameraConnected
        {
            get
            {
                if(this.MaximConnected && m_ccd != null)
                    return m_ccd.LinkEnabled;
                else
                    return false;
            }

            set
            {
                if(this.MaximConnected && m_ccd != null)
                    m_ccd.LinkEnabled = value;

                if(value == true)
                {
                    MainWnd.AddLogItem("CCD Camera " + m_ccd.CameraName + " connected");

                    if(m_ccd.HasFilterWheel)
                    {
                        object[] o = m_ccd.FilterNames;  // Can't use the filter names directly as they're in a variant (BSTR array)
                        m_filterNames = new string[o.GetLength(0)];
                        for(int i = 0; i < o.GetLength(0); i++)
                            m_filterNames[i] = (string)o[i];
                    }
                }
                this.OnPropertyChanged("CameraConnected");
            }
        }

        public bool MaximConnected
        {
            get
            {
                if(m_maxim == null)
                    return false;  // MaxIm is not running
                else
                {
                    try
                    {
                        // Check that MaxIm really is still connected by trying to access the version property
                        float version = m_maxim.Version;
                        return true;  // MaxIm is running
                    }
                    catch
                    {
                        // There was a null-ptr exception accessing MaxIm - it's been closed
                        m_maxim = null;
                        m_ccd = null;
                        return false;
                    }
                }
            }

            set
            {
                if(value == true)
                {
                    if(m_maxim == null)
                        m_maxim = new MaxIm.Application();

                    if(m_ccd == null)
                        m_ccd = new MaxIm.CCDCamera();
                }

                MainWnd.AddLogItem("MaxIm DL " + m_maxim.Version.ToString() + " connected");
                this.OnPropertyChanged("MaximConnected");
            }
        }

        public bool MaximAndCameraConnected
        {
            get
            {
                return MaximConnected && CameraConnected;
            }

            set
            {
                MaximConnected = value;
                CameraConnected = value;

                this.OnPropertyChanged("MaximAndCameraConnected");
            }
        }
        #endregion

        #region Public Methods
        public bool InitCamera()
        {
            try
            {
                m_ccd.AbortExposure();  // Stop any current imaging

                if(!m_ccd.AutoDownload)
                    m_ccd.StartDownload();  // Clear any previous image

                MainWnd.AddLogItem("MaxIm DL CCD Camera initialized");
                if(m_ccd.HasFilterWheel)
                {
                    // Do we need to change the filter?
                    if(m_ccd.Filter != m_mainWnd.AppSettings.FilterToUse)
                    {
                        MainWnd.AddLogItem("Changing from " + m_filterNames[m_ccd.Filter] + " to " + m_filterNames[m_mainWnd.AppSettings.FilterToUse]);
                        m_ccd.Filter = m_mainWnd.AppSettings.FilterToUse;
                    }
                    MainWnd.AddLogItem("Using " + m_filterNames[m_ccd.Filter] + " filter");
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool FullFrame(short bin)
        {
            // Prior to calling this method a call must have been made to InitCamera()
            m_ccd.BinX = bin;
            m_ccd.BinY = bin;
            m_ccd.SetFullFrame();  // Clear any sub-frame

            MainWnd.AddLogItem("Full-frame at bin " + bin.ToString() + " (locate star)");
            if(!m_ccd.Expose(m_mainWnd.AppSettings.ExposureDurationFullFrame, LightExposure, m_ccd.Filter))
                return false;

            return true;
        }

        public bool SubFrame(short x, short y)
        {
            m_ccd.BinX = 1;
            m_ccd.BinY = 1;

            m_ccd.StartX = (short)(x - ((short)MainWnd.AppSettings.SubFrameSize / 2));  // Take the X coordinate of the brightest star and subtract an offset
            m_ccd.StartY = (short)(y - ((short)MainWnd.AppSettings.SubFrameSize / 2));  // Take the Y coordinate of the brightest star and subtract an offset
            m_ccd.NumX = MainWnd.AppSettings.SubFrameSize;  // Width of sub-frame
            m_ccd.NumY = MainWnd.AppSettings.SubFrameSize;  // Height of sub-frame

            MainWnd.AddLogItem("Sub-frame image at bin 1");

            if(!m_ccd.Expose(m_mainWnd.AppSettings.ExposureDurationSubFrame, LightExposure, m_ccd.Filter))
                return false;

            return true;
        }

        public bool CheckSubFrameBounds(short x, short y)
        {
            if(x + (m_mainWnd.AppSettings.SubFrameSize / 2) > this.Camera.Document.XSize)
                return false;

            if(x - (m_mainWnd.AppSettings.SubFrameSize / 2) < 0)
                return false;

            if(y + (m_mainWnd.AppSettings.SubFrameSize / 2) > this.Camera.Document.YSize)
                return false;

            if(y - (m_mainWnd.AppSettings.SubFrameSize / 2) < 0)
                return false;

            return true;
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
