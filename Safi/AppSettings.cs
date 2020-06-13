using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using ASCOM.DriverAccess;

namespace Safi
{
    /// <summary>
    /// Class to read and write all the properties used by the application to/from the registry
    /// </summary>
    class AppSettings : INotifyPropertyChanged
    {
        #region Declarations
        public event PropertyChangedEventHandler PropertyChanged;

        private double m_top;                       // Main window position (top)
        private double m_left;                      // Main window position (left)
        private bool m_ontop;                       // True if the window should be on top (default = 0)

        private double m_topNudge;                  // Nudge window position (top)
        private double m_leftNudge;                 // Nudge window position (left)
        private bool m_ontopNudge;                  // True if the nudge window should be on top (default = 0)

        private double m_topGraph;                  // Graph window position (top)
        private double m_leftGraph;                 // Graph window position (left)
        private bool m_ontopGraph;                  // True if the graph window should be on top (default = 0)
        private double m_widthGraph;                // Graph window width (it's resizable)
        private double m_heightGraph;               // Graph window height (it's resizable)

        private bool m_simulator;                   // True if using the MaxIm DL camera simulator
        private string m_focuserID;                 // Focuser COM ProgID
        private string m_focuserName;               // Friendly device name
        private short m_binFullFrame;               // Binning for full frame images (default = 2)
        private short m_binSubFrame;                // Binning for sub-frames (default = 1)
        private int m_moveInOutDefault;             // Default value to jog the Focuser in/out manually (default = 100)
        private short m_filterToUse;                // The filter number to use for location and focus exposures (default = 0)
        private int m_maxIn;                        // The outtermost position we should move the focuser. N.B. MaxIn must be equal to MaxOut
        private int m_maxOut;                       // The outtermost position we should move the focuser. N.B. MaxIn must be equal to MaxOut
        private int m_stepSize;                     // The amount to move the focuser by between images when auto-focusing (default = 100)
        private int m_stepSizeNearFocus;            // The amount to move the focuser by between images when close to focus (default = 10)
        private int m_stepSizeNearFocusMultiplier;  // When near to focus, the amount (n * m_stepSizeNearFocus) to start/end focusing
        private int m_nImagesPerDataPoint;          // The number of images to take at each position. The HFD/FWHM values will be averaged
        private int m_nImagesPerDataPointNearFocus; // The number of images to take at each position near focus. The HFD/FWHM values will be averaged
        private int m_nRetryOnAutoFocusFail;        // The number of times to retry if auto-focus fails
        private double m_exposureDurationFullFrame; // Length of exposure when locating a star at full-frame
        private double m_exposureDurationSubFrame;  // Length of exposure when focusing a star using a sub-frame
        private short m_subFrameSize;               // Size (in pixels) of the sub-frame rectangle (x/y are uniform)
        private long m_minMaxPixel;                 // The smallest value acceptable as a MaxPixel (bright star) value

        private ArrayList m_focuserList;            // List of available Focuser devices (not written to the registry)
        private double m_maxHFD = 30.0;             // The maximum valid HFD value (not written to the registry)
        private double m_minHFD = 0;                // The minimum valid HFD value (not written to the registry)
        #endregion

        #region Properties
        public int StepSizeNearFocusMultiplier
        {
            get
            {
                return m_stepSizeNearFocusMultiplier;
            }
            set
            {
                m_stepSizeNearFocusMultiplier = value;
                this.OnPropertyChanged("StepSizeNearFocusMultiplier");
            }
        }

        public long MinMaxPixel
        {
            get
            {
                return m_minMaxPixel;
            }
            set
            {
                m_minMaxPixel = value;
                this.OnPropertyChanged("MinMaxPixel");
            }
        }

        public short SubFrameSize
        {
            get
            {
                return m_subFrameSize;
            }
            set
            {
                m_subFrameSize = value;
                this.OnPropertyChanged("SubFrameSize");
            }
        }

        public double ExposureDurationFullFrame
        {
            get
            {
                return m_exposureDurationFullFrame;
            }
            set
            {
                m_exposureDurationFullFrame = value;
                this.OnPropertyChanged("ExposureDurationFullFrame");
            }
        }

        public double ExposureDurationSubFrame
        {
            get
            {
                return m_exposureDurationSubFrame;
            }
            set
            {
                m_exposureDurationSubFrame = value;
                this.OnPropertyChanged("ExposureDurationSubFrame");
            }
        }

        public double MaxHFD
        {
            get
            {
                return m_maxHFD;
            }
            set
            {
                m_maxHFD = value;
                this.OnPropertyChanged("MaxHFD");
            }
        }

        public double MinHFD
        {
            get
            {
                return m_minHFD;
            }
            set
            {
                m_minHFD = value;
                this.OnPropertyChanged("MinHFD");
            }
        }

        public double WidthGraph
        {
            get
            {
                return m_widthGraph;
            }
            set
            {
                m_widthGraph = value;
                this.OnPropertyChanged("WidthGraph");
            }
        }

        public double HeightGraph
        {
            get
            {
                return m_heightGraph;
            }
            set
            {
                m_heightGraph = value;
                this.OnPropertyChanged("HeightGraph");
            }
        }

        public double TopNudge
        {
            get
            {
                return m_topNudge;
            }
            set
            {
                m_topNudge = value;
                this.OnPropertyChanged("TopNudge");
            }
        }

        public double LeftNudge
        {
            get
            {
                return m_leftNudge;
            }
            set
            {
                m_leftNudge = value;
                this.OnPropertyChanged("LeftNudge");
            }
        }

        public bool OntopNudge
        {
            get
            {
                return m_ontopNudge;
            }
            set
            {
                m_ontopNudge = value;
                this.OnPropertyChanged("OntopNudge");
            }
        }

        public double TopGraph
        {
            get
            {
                return m_topGraph;
            }
            set
            {
                m_topGraph = value;
                this.OnPropertyChanged("TopGraph");
            }
        }

        public double LeftGraph
        {
            get
            {
                return m_leftGraph;
            }
            set
            {
                m_leftGraph = value;
                this.OnPropertyChanged("LeftGraph");
            }
        }

        public bool OntopGraph
        {
            get
            {
                return m_ontopGraph;
            }
            set
            {
                m_ontopGraph = value;
                this.OnPropertyChanged("OntopGraph");
            }
        }

        public bool Simulator
        {
            get
            {
                return m_simulator;
            }
            set
            {
                m_simulator = value;
                this.OnPropertyChanged("Simulator");
            }
        }

        public string FocuserID
        {
            get
            {
                return m_focuserID;
            }
            set
            {
                m_focuserID = value;
                this.OnPropertyChanged("FocuserID");
            }
        }

        public string FocuserName
        {
            get
            {
                return m_focuserName;
            }
            set
            {
                m_focuserName = value;
                this.OnPropertyChanged("FocuserName");
            }
        }

        public double Top
        {
            get
            {
                return m_top;
            }
            set
            {
                m_top = value;
                this.OnPropertyChanged("Top");
            }
        }

        public double Left
        {
            get
            {
                return m_left;
            }
            set
            {
                m_left = value;
                this.OnPropertyChanged("Left");
            }
        }

        public short BinFullFrame
        {
            get
            {
                return m_binFullFrame;
            }
            set
            {
                m_binFullFrame = value;
                this.OnPropertyChanged("BinFullFrame");
            }
        }

        public short BinSubFrame
        {
            get
            {
                return m_binSubFrame;
            }
            set
            {
                m_binSubFrame = value;
                this.OnPropertyChanged("BinSubFrame");
            }
        }

        public int MoveInOutDefault
        {
            get
            {
                return m_moveInOutDefault;
            }
            set
            {
                m_moveInOutDefault = value;
                this.OnPropertyChanged("MoveInOutDefault");
            }
        }

        public short FilterToUse
        {
            get
            {
                return m_filterToUse;
            }
            set
            {
                m_filterToUse = value;
                this.OnPropertyChanged("FilterToUse");
            }
        }

        public bool Ontop
        {
            get
            {
                return m_ontop;
            }
            set
            {
                m_ontop = value;
                this.OnPropertyChanged("Ontop");
            }
        }

        public int MaxOut
        {
            get
            {
                return m_maxOut;
            }
            set
            {
                m_maxOut = value;
                this.OnPropertyChanged("MaxOut");
            }
        }

        public int MaxIn
        {
            get
            {
                return m_maxIn;
            }
            set
            {
                m_maxIn = value;
                this.OnPropertyChanged("MaxIn");
            }
        }

        public int StepSize
        {
            get
            {
                return m_stepSize;
            }
            set
            {
                m_stepSize = value;
                this.OnPropertyChanged("StepSize");
            }
        }

        public int StepSizeNearFocus
        {
            get
            {
                return m_stepSizeNearFocus;
            }
            set
            {
                m_stepSizeNearFocus = value;
                this.OnPropertyChanged("StepSizeNearFocus");
            }
        }

        public int NImagesPerDataPoint
        {
            get
            {
                return m_nImagesPerDataPoint;
            }
            set
            {
                m_nImagesPerDataPoint = value;
                this.OnPropertyChanged("NImagesPerDataPoint");
            }
        }

        public int NImagesPerDataPointNearFocus
        {
            get
            {
                return m_nImagesPerDataPointNearFocus;
            }
            set
            {
                m_nImagesPerDataPointNearFocus = value;
                this.OnPropertyChanged("NImagesPerDataPointNearFocus");
            }
        }

        public int NRetryOnAutoFocusFail
        {
            get
            {
                return m_nRetryOnAutoFocusFail;
            }
            set
            {
                m_nRetryOnAutoFocusFail = value;
                this.OnPropertyChanged("NRetryOnAutoFocusFail");
            }
        }

        public ArrayList FocuserList
        {
            get
            {
                return m_focuserList;
            }
            set
            {
                m_focuserList = value;
                this.OnPropertyChanged("FocuserList");
            }
        }
        #endregion

        #region Construction
        public AppSettings()
        {
            CheckSettings();
        }
        #endregion

        #region Public Methods
        public void WriteLog(string[] log)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SAFI";
            if(!Directory.Exists(path))
                Directory.CreateDirectory(path);

            using(StreamWriter outfile = new StreamWriter(path + @"\log.txt"))
            {
                foreach(string line in log)
                    outfile.WriteLine(line);
            }
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Read/Write Registry Methods
        public bool ReadConfig()
        {
            Microsoft.Win32.RegistryKey key;
            try
            {
                key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software", false).OpenSubKey("SAFI", false);
                if(key == null)
                    CheckSettings();
                else
                {
                    m_simulator = bool.Parse(key.GetValue("Simulator").ToString());
                    m_top = double.Parse(key.GetValue("Top").ToString());
                    m_left = double.Parse(key.GetValue("Left").ToString());
                    m_ontop = bool.Parse(key.GetValue("OnTop").ToString());

                    m_topNudge = double.Parse(key.GetValue("TopNudge").ToString());
                    m_leftNudge = double.Parse(key.GetValue("LeftNudge").ToString());
                    m_ontopNudge = bool.Parse(key.GetValue("OnTopNudge").ToString());

                    m_topGraph = double.Parse(key.GetValue("TopGraph").ToString());
                    m_leftGraph = double.Parse(key.GetValue("LeftGraph").ToString());
                    m_ontopGraph = bool.Parse(key.GetValue("OnTopGraph").ToString());
                    m_widthGraph = double.Parse(key.GetValue("WidthGraph").ToString());
                    m_heightGraph = double.Parse(key.GetValue("HeightGraph").ToString());
                    
                    m_focuserID = key.GetValue("FocuserProgID") as string;
                    m_focuserName = key.GetValue("Name") as string;
                    m_binFullFrame = short.Parse(key.GetValue("BinFullFrame").ToString());
                    m_binSubFrame = short.Parse(key.GetValue("BinSubFrame").ToString());
                    m_moveInOutDefault = int.Parse(key.GetValue("MoveInOutDefault").ToString());
                    m_filterToUse = short.Parse(key.GetValue("FilterToUse").ToString());
                    m_maxIn = int.Parse(key.GetValue("MaxIn").ToString());
                    m_maxOut = int.Parse(key.GetValue("MaxOut").ToString());
                    m_stepSize = int.Parse(key.GetValue("StepSize").ToString());
                    m_stepSizeNearFocus = int.Parse(key.GetValue("StepSizeNearFocus").ToString());
                    m_stepSizeNearFocusMultiplier = int.Parse(key.GetValue("StepSizeNearFocusMultiplier").ToString());
                    m_nImagesPerDataPoint = int.Parse(key.GetValue("NImagesPerDataPoint").ToString());
                    m_nImagesPerDataPointNearFocus = int.Parse(key.GetValue("NImagesPerDataPointNearFocus").ToString());
                    m_nRetryOnAutoFocusFail = int.Parse(key.GetValue("NRetryOnAutoFocusFail").ToString());
                    m_exposureDurationFullFrame = double.Parse(key.GetValue("ExposureDurationFullFrame").ToString());
                    m_exposureDurationSubFrame = double.Parse(key.GetValue("ExposureDurationSubFrame").ToString());
                    m_subFrameSize = short.Parse(key.GetValue("SubFrameSize").ToString());
                    m_minMaxPixel = short.Parse(key.GetValue("MinMaxPixel").ToString());

                    key.Close();
                }

                // Get a list of key (friendly device name)/value (device prog ID) pairs for the Focuser devices on this system
                // We'll display the friendly device name and map it to the prog ID
                ASCOM.Utilities.Profile profile = new Profile(true);
                if(profile != null)
                    m_focuserList = profile.RegisteredDevices("Focuser");
            }
            catch
            {
                // Set default values as we can't read the registry
                m_simulator = false;
                m_top = 0;
                m_left = 0;
                m_ontop = false;
                m_topNudge = 0;
                m_leftNudge = 0;
                m_ontopNudge = false;
                m_topGraph = 0;
                m_leftGraph = 0;
                m_ontopGraph = false;
                m_widthGraph = 400;
                m_heightGraph = 400;
                m_focuserID = "";
                m_focuserName = "";
                m_top = 0;
                m_left = 0;
                m_ontop = false;
                m_binFullFrame = 2;
                m_binSubFrame = 1;
                m_moveInOutDefault = 100;
                m_filterToUse = 0;
                m_maxIn = 700;
                m_maxOut = 700;
                m_stepSize = 100;
                m_stepSizeNearFocus = 10;
                m_stepSizeNearFocusMultiplier = 4;
                m_nImagesPerDataPoint = 1;
                m_nImagesPerDataPointNearFocus = 2;
                m_nRetryOnAutoFocusFail = 3;
                m_exposureDurationFullFrame = 2.5;
                m_exposureDurationSubFrame = 1.0;
                m_subFrameSize = 100;
                m_minMaxPixel = 150;

                return false;
            }

            return true;
        }

        public bool WriteConfig()
        {
            Microsoft.Win32.RegistryKey key;
            try
            {
                key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("SAFI", true);
                if(key == null)
                    CheckSettings();

                key.SetValue("Top", m_top);
                key.SetValue("Left", m_left);
                key.SetValue("OnTop", m_ontop);

                key.SetValue("TopNudge", m_topNudge);
                key.SetValue("LeftNudge", m_leftNudge);
                key.SetValue("OnTopNudge", m_ontopNudge);

                key.SetValue("TopGraph", m_topGraph);
                key.SetValue("LeftGraph", m_leftGraph);
                key.SetValue("OnTopGraph", m_ontopGraph);
                key.SetValue("WidthGraph", m_widthGraph);
                key.SetValue("HeightGraph", m_heightGraph);

                key.SetValue("Simulator", m_simulator);
                key.SetValue("Name", m_focuserName);            
                key.SetValue("FocuserProgID", m_focuserID);     
                key.SetValue("BinFullFrame", m_binFullFrame);
                key.SetValue("BinSubFrame", m_binSubFrame);
                key.SetValue("MoveInOutDefault", m_moveInOutDefault);
                key.SetValue("FilterToUse", m_filterToUse);
                key.SetValue("MaxIn", m_maxIn);
                key.SetValue("MaxOut", m_maxOut);
                key.SetValue("StepSize", m_stepSize);
                key.SetValue("StepSizeNearFocus", m_stepSizeNearFocus);
                key.SetValue("StepSizeNearFocusMultiplier", m_stepSizeNearFocusMultiplier);
                key.SetValue("NImagesPerDataPoint", m_nImagesPerDataPoint);
                key.SetValue("NImagesPerDataPointNearFocus", m_nImagesPerDataPointNearFocus);
                key.SetValue("NRetryOnAutoFocusFail", m_nRetryOnAutoFocusFail);
                key.SetValue("ExposureDurationFullFrame", m_exposureDurationFullFrame);
                key.SetValue("ExposureDurationSubFrame", m_exposureDurationSubFrame);
                key.SetValue("SubFrameSize", m_subFrameSize);
                key.SetValue("MinMaxPixel", m_minMaxPixel);

                key.Close();
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void CheckSettings()
        {
            Microsoft.Win32.RegistryKey key;
            try
            {
                key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("SAFI", true);
                if(key == null)
                    key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software", true).CreateSubKey("SAFI");

                if(key.GetValue("Top") == null)
                    key.SetValue("Top", 0);

                if(key.GetValue("Left") == null)
                    key.SetValue("Left", 0);

                if(key.GetValue("OnTop") == null)
                    key.SetValue("OnTop", false);

                if(key.GetValue("TopNudge") == null)
                    key.SetValue("TopNudge", 0);

                if(key.GetValue("LeftNudge") == null)
                    key.SetValue("LeftNudge", 0);

                if(key.GetValue("OnTopNudge") == null)
                    key.SetValue("OnTopNudge", false);

                if(key.GetValue("TopGraph") == null)
                    key.SetValue("TopGraph", 0);

                if(key.GetValue("LeftGraph") == null)
                    key.SetValue("LeftGraph", 0);

                if(key.GetValue("OnTopGraph") == null)
                    key.SetValue("OnTopGraph", false);

                if(key.GetValue("WidthGraph") == null)
                    key.SetValue("WidthGraph", 400);

                if(key.GetValue("HeightGraph") == null)
                    key.SetValue("HeightGraph", 400);
                
                if(key.GetValue("Simulator") == null)
                    key.SetValue("Simulator", false);

                if(key.GetValue("FocuserProgID") == null)
                    key.SetValue("FocuserProgID", "");

                if(key.GetValue("Name") == null)
                    key.SetValue("Name", "");

                if(key.GetValue("BinFullFrame") == null)
                    key.SetValue("BinFullFrame", 2);

                if(key.GetValue("BinSubFrame") == null)
                    key.SetValue("BinSubFrame", 1);

                if(key.GetValue("MoveInOutDefault") == null)
                    key.SetValue("MoveInOutDefault", 100);

                if(key.GetValue("FilterToUse") == null)
                    key.SetValue("FilterToUse", 0);

                if(key.GetValue("MaxIn") == null)
                    key.SetValue("MaxIn", 700);

                if(key.GetValue("MaxOut") == null)
                    key.SetValue("MaxOut", 700);
                
                if(key.GetValue("StepSize") == null)
                    key.SetValue("StepSize", 100);

                if(key.GetValue("StepSizeNearFocus") == null)
                    key.SetValue("StepSizeNearFocus", 10);

                if(key.GetValue("StepSizeNearFocusMultiplier") == null)
                    key.SetValue("StepSizeNearFocusMultiplier", 4);

                if(key.GetValue("NImagesPerDataPoint") == null)
                    key.SetValue("NImagesPerDataPoint", 1);

                if(key.GetValue("NImagesPerDataPointNearFocus") == null)
                    key.SetValue("NImagesPerDataPointNearFocus", 2);

                if(key.GetValue("NRetryOnAutoFocusFail") == null)
                    key.SetValue("NRetryOnAutoFocusFail", 3);

                if(key.GetValue("ExposureDurationFullFrame") == null)
                    key.SetValue("ExposureDurationFullFrame", 2.5);

                if(key.GetValue("ExposureDurationSubFrame") == null)
                    key.SetValue("ExposureDurationSubFrame", 1);

                if(key.GetValue("SubFrameSize") == null)
                    key.SetValue("SubFrameSize", 100);

                if(key.GetValue("MinMaxPixel") == null)
                    key.SetValue("MinMaxPixel", 150);

                key.Close();
            }
            catch
            {
            }
        }
        #endregion
    }
}