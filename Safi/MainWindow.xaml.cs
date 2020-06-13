using System;
using System.Collections;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using Microsoft.Win32;
//using ASCOM.DeviceInterface;
//using ASCOM.Utilities;
//using ASCOM.DriverAccess;

namespace Safi
{
    /// <summary>
    /// Simple Auto-Focus (SAF) Main Window
    /// </summary>
    public partial class MainWindow : Window,  INotifyPropertyChanged
    {
        #region Declarations
        public event PropertyChangedEventHandler PropertyChanged;

        public enum WorkFlow                                // Declaration of the varoius auto-focus workflow states
        {
            NoActivity,                                     // No imaging is taking place
            Start,                                          // Start the auto-focus process
            LocateStar,                                     // Take a full-frame image to locate a suitable star
            LocateStarImageReady,                           // Wait until the the LocateStar image is ready
            FrameStar,                                      // Take a sub-frame image to frame a suitable star
            FrameStarImageReady,                            // Wait until the the FrameStar image is ready  
            MoveToStart,                                    // Moving the focuser to the start (MaxIn) position
            MoveToStartFocuserReady,                        // Wait until the focuser stops moving from the current position to MaxIn
            FocusOut,                                       // Take a series of focus images with the focuser headed outwards
            FocusOutImageReady,                             // Get an image taken with the focuser headed outwards
            FocusOutNextPosition,                           // Move to the next position in the direction MaxOut
            FocusOutNextPositionFocuserReady,               // Wait until the focuser stops moving
            MoveToPBFPOffset,                               // Move to provisional BFP + (2 * StepSizeNearFocus)
            MoveToPBFPOffsetFocuserReady,                   // Wait until the focuser stops moving from the provisional BFP
            FocusOutPBFP,                                   // Take a series of near-focus images with the focuser headed outwards
            FocusOutPBFPImageReady,                         // Get a near-focus image with the focuser headed outwards
            FocusOutPBFPNextPosition,                       // Move to the next near-focus position in the direction MaxOut
            FocusOutPBFPNextPositionFocuserReady,           // Wait until the focuser stops moving in the near-focus region
            MoveToBFP,                                      // Goto the calculated best focus point (BFP)
            MoveToBFPFocuserReady,                          // Wait until the focuser stops moving and arrives at the BFP
            DrawDragRect                                    // Test process: Draw a rectangle around the brightest star
        };

        private MaxImDL m_maxim;                                    // Encapsulates the MaxIm DL application interface
        private Focus m_focus;                                      // Encapsulates the ASCOM Focuser driver
        private AppSettings m_appSettings;                          // Encapsulates all persistent settings used by this app
        private Nudge m_nudge;                                      // The focuser "nudge" window
        private AutoFocusGraph m_graph;                             // The auto-focus data point plot window

        private bool m_autofocus = false;                           // True if we're in the middle of doing an auto-focus, false otherwise
        private DispatcherTimer m_timer;                            // Timer used to poll the Focuser, etc.
        private WorkFlow AutoFocusWorkFlow;                         // The state of the imaging/focusing workflow
        private Dictionary<string, string> SystemStatus;            // MaxIm's CCD status codes, plus additional info (like focuser moving, etc.)
        private string m_currentSystemStatus;                       // Text message used to display the current status
        private List<FocusPoint> m_focusDataPoints;                 // A list of data points containing info about focus a various focuser positions
        private FocusPoint m_pbfp = null;                           // Provisional best focus point
        private FocusPoint m_bfp = null;                            // Best focus point
        private int m_pbfpOffsetEndPos;                             // Holds the end position value when imaging between provision BFP offset points
        private int m_currentAutoFocusTry;                          // The number of times we've tried to auto-focus
        private int m_currentImageCount;                            // The number of images we've taken for the current data point
        private int m_currentImageNearFocusCount;                   // The number of images we've taken for the current near-focus data point
        private double[] m_simulatorDataPoints;                     // Series of simulated data points for use with the MaxIm CCD simulator
        private List<KeyValuePair<int, double>> m_focusPlotPoints;  // List of plot points used for the focus graph
        private short m_x, m_y;                                     // Coordinates of star
        private short m_xStart, m_yStart;

        private delegate void BlockingOperation(string filename); 
        #endregion

        #region Properties
        internal AppSettings AppSettings                        { get { return m_appSettings; } }
        internal MaxImDL MaxIm                                  { get { return m_maxim; } }
        internal Focus Focuser                                  { get { return m_focus; } }
        public List<KeyValuePair<int, double>> FocusPlotPoints  { get { return m_focusPlotPoints; } }
        public string AutoFocusButtonText                       { get { if(AutoFocusInProgress) return "Stop"; else return "Start"; } }
        public bool AutoFocusButtonEnabled
        {
            get
            {
                if(m_maxim != null && m_focus != null)
                    return MaxIm.MaximAndCameraConnected && Focuser.FocuserConnected;
                else
                    return false;
            }
        }

        public bool AutoFocusInProgress
        {
            get
            {
                return m_autofocus;
            }
            set
            {
                m_autofocus = value;
                this.OnPropertyChanged("AutoFocusInProgress");
                this.OnPropertyChanged("AutoFocusButtonText");
                this.OnPropertyChanged("AutoFocusButtonColor");
            }
        }

        public string CurrentSystemStatus
        {
            get
            {
                return m_currentSystemStatus;
            }
            set
            {
                m_currentSystemStatus = value;
                this.OnPropertyChanged("CurrentSystemStatus");
            }
        }
        #endregion

        #region Construction
        public MainWindow()
        {
            InitializeComponent();
            
            // Create the settings object and read our settings from the registry
            m_appSettings = new AppSettings();
            AppSettings.ReadConfig();

            // Create the MaxIm DL object and hook into the property changed event
            m_maxim = new MaxImDL(this);
            m_maxim.PropertyChanged += new PropertyChangedEventHandler(m_maxim_PropertyChanged);

            // Create the focuser object and hook into the property changed event
            m_focus = new Focus(AppSettings.FocuserID, AppSettings.MaxIn, AppSettings.MaxOut);
            m_focus.PropertyChanged += new PropertyChangedEventHandler(m_focus_PropertyChanged);

            // Set the window state
            this.Topmost = AppSettings.Ontop;
            this.Left = AppSettings.Left;
            this.Top = AppSettings.Top;

            // Establish data bindings...
            textBoxFocuser.DataContext = AppSettings;           // XAML binding: Text="{Binding Path=FocuserName}"
            buttonFocuserSelect.DataContext = Focuser;          // XAML binding: IsEnabled="{Binding Path=FocuserNotConnected}" 
            checkBoxMaximRunning.DataContext = MaxIm;           // XAML binding: IsChecked="{Binding Path=MaximConnected}"
            checkBoxCamera.DataContext = MaxIm;                 // XAML binding: IsChecked="{Binding Path=CameraConnected}"
            checkBoxFocuserConnected.DataContext = Focuser;     // XAML binding: IsChecked="{Binding Path=FocuserConnected}"
            checkBoxOnTop.DataContext = AppSettings;            // XAML binding: IsChecked="{Binding Path=Ontop}"
            checkBoxSimulator.DataContext = AppSettings;        // XAML binding: IsChecked="{Binding Path=Simulator}"
            buttonFocus.DataContext = this;                     // XAML binding: Content="{Binding Path=AutoFocusButtonText}", Background="{Binding Path=AutoFocusButtonColor}"
            textBoxHFD.DataContext = Focuser;                   // XAML binding: Text="{Binding Path=HFD, Mode=OneWay, FallbackValue=-}"
            textBoxFWHM.DataContext = Focuser;                  // XAML binding: Text="{Binding Path=FWHM, Mode=OneWay, FallbackValue=-}"
            textBoxPosition.DataContext = Focuser;              // XAML binding: Text="{Binding Path=Position, Mode=OneWay, FallbackValue=-}"
            textBoxStatus.DataContext = this;                   // XAML binding: Text="{Binding Path=CurrentSystemStatus, Mode=OneWay, FallbackValue=-}"

            // Setup a timer. This will be used for polling the state of various operations.
            // Here we use the new WPF DispatcherTimer class because it makes use of the Dispatcher, thus 
            // allow us to update the UI (using a normal timer puts in on a separate thread from the UI
            // See the following reference article: http://msdn.microsoft.com/en-us/magazine/cc163328.aspx
            m_timer = new DispatcherTimer();
            m_timer.Interval = TimeSpan.FromMilliseconds(1000);
            m_timer.Tick += new EventHandler(m_timer_Tick);            
            m_timer.IsEnabled = true;

            AutoFocusWorkFlow = WorkFlow.NoActivity;
            m_currentAutoFocusTry = 0;
            m_currentImageCount = 0;
            m_currentImageNearFocusCount = 0;

            SystemStatus = new Dictionary<string, string>();
            SystemStatus.Add("csNoCamera", "Disconnected");                 //  (0) : Camera is not connected
            SystemStatus.Add("csError", "Camera Error");                    //  (1) : Camera is reporting an error
            SystemStatus.Add("csIdle", "Idle");                             //  (2) : Camera is connected but inactive
            SystemStatus.Add("csExposing", "Exposing");                     //  (3) : Camera is exposing a light image
            SystemStatus.Add("csReading", "Reading");                       //  (4) : Camera is reading an image from the sensor array
            SystemStatus.Add("csDownloading", "Downloading");               //  (5) : Camera is downloading an image to the computer
            SystemStatus.Add("csFlushing", "Flusing_Sensor");               //  (6) : Camera is flushing the sensor array
            SystemStatus.Add("csWaitTrigger", "Waiting for Trigger");       //  (7) : Camera is waiting for a trigger signal
            SystemStatus.Add("csWaiting", "Waiting for MaxIm");             //  (8) : Camera Control Window is waiting for MaxIm DL to be ready to accept an image
            SystemStatus.Add("csDelay", "Delay");                           //  (9) : Camera Control is waiting for it to be time to acquire next image
            SystemStatus.Add("csExposingAutoDark", "Exposing AutoDark");    //  (10) : Camera is exposing a dark needed by Simple Auto Dark
            SystemStatus.Add("csExposingBias", "Exposing Bias");            //  (11) : Camera is exposing a Bias frame
            SystemStatus.Add("csExposingDark", "Exposing Dark");            //  (12) : Camera is exposing a Dark frame
            SystemStatus.Add("csExposingFlat", "Exposing_Flat");            //  (13) : Camera is exposing a Flat frame
            SystemStatus.Add("csFilterWheelMoving", "Filter Wheel Moving"); //  (15) : Camera Control window is waiting for filter wheel or focuser
            SystemStatus.Add("csWaitingForManualShutter", "Manual Shutter");//  (20) : Camera Control Window is waiting for operator to cover or uncover the telescope
            SystemStatus.Add("csExposingRed", "Exposing Red");              //  (24) : Camera is exposing a red image (MaxIm+ only)
            SystemStatus.Add("csExposingGreen", "Exposing Green");          //  (25) : Camera is exposing a green image (MaxIm+ only)
            SystemStatus.Add("csExposingBlue", "Exposing Blue");            //  (26) : Camera is exposing a blue image (MaxIm+ only)
            SystemStatus.Add("csExposingVideo", "Exposing Video");          //  (27) : Camera is exposing a video clip
            SystemStatus.Add("csFocuserMoving", "Focuser Moving");          //  (28) : Custom (non-MaxIm status)
            SystemStatus.Add("csNoActivity", "-");                          //  (29) : Custom (nothing happening)
        }
        #endregion

        #region Public Methods
        public void AddLogItem(string msg)
        {
            msg = DateTime.Now.ToLongTimeString() + " : " + msg;
            listBoxFocusMonitorLog.Items.Add(msg);
            listBoxFocusMonitorLog.ScrollIntoView(msg);
        }

        #endregion

        #region Private Methods
        void m_timer_Tick(object sender, EventArgs e)
        {
            if(m_focus != null)
            {
                if(m_focus.FocuserConnected)
                {
                    // Update the status display
                    if(m_focus.Focuser.IsMoving == true)
                        CurrentSystemStatus = SystemStatus["csFocuserMoving"];
                    else if(m_maxim != null && m_maxim.CameraConnected)
                        CurrentSystemStatus = SystemStatus[m_maxim.Camera.CameraStatus.ToString()];
                    else
                        CurrentSystemStatus = SystemStatus["csNoActivity"];
                }
            }

            WorkFlowProcessing();  // Process the current state of auto-focus
        }

        private void WorkFlowProcessing()
        {
            int[] dragRect;
            long maxPixel;

            // 1. Goto MaxIn
            // 2. Take images from MaxIn ... MaxOut -> gives provisional best focus position (BFP)
            // 3. Goto BFP + (2 * StepSizeNearFocus) -> i.e. past BFP in the direction of MaxIn
            // 4. Take images from 3. to BFP - (2 * StepSizeNearFocus) -> i.e. from 3. to an equal point the other side, in the direction of MaxOut
            // 5. Goto confirmed BFP
            switch(AutoFocusWorkFlow)
            {
                case WorkFlow.Start:
                    m_currentAutoFocusTry++;

                    if(m_currentAutoFocusTry > AppSettings.NRetryOnAutoFocusFail)
                    {
                        AddLogItem("Number of auto-focus retries exceeds limit");
                        AddLogItem("Aborting auto-focus");
                        AutoFocusWorkFlow = WorkFlow.NoActivity;
                        buttonFocus_Click(this, null);  // Reset the auto-focus start/stop button
                        return;
                    }
                    else if (m_currentAutoFocusTry > 1)
                        AddLogItem("Auto-focus retry (" + m_currentAutoFocusTry.ToString() + ")");

                    if(!m_maxim.InitCamera())
                    {
                        AddLogItem("Camera initialization failed");
                        AddLogItem("Retrying auto-focus");
                        AutoFocusWorkFlow = WorkFlow.Start;
                        return;
                    }
                    else
                        AddLogItem("Camera initialization OK");

                    m_focusDataPoints = new List<FocusPoint>();  // Create a list to hold focus data points
                    m_focusPlotPoints = new List<KeyValuePair<int, double>>();  // And a list of graph plot points

                    // Setup the x and y axis ranges
                    if(m_graph != null)
                    {
                        m_graph.DataSource = m_focusPlotPoints;
                        m_graph.XAxis.Maximum = AppSettings.MaxIn;
                        m_graph.XAxis.Minimum = -(AppSettings.MaxOut);
                        m_graph.YAxis.Maximum = AppSettings.MaxHFD;
                        m_graph.YAxis.Minimum = AppSettings.MinHFD;
                    }
                    AutoFocusWorkFlow = WorkFlow.LocateStar;
                    break;

                case WorkFlow.LocateStar:
                    if(!m_maxim.FullFrame(AppSettings.BinFullFrame))  // Take an image at (by default) bin 2
                    {
                        AddLogItem("Star location image failed");
                        AddLogItem("Retrying auto-focus");
                        AutoFocusWorkFlow = WorkFlow.Start;
                        return;
                    }

                    AutoFocusWorkFlow = WorkFlow.LocateStarImageReady;
                    break;

                case WorkFlow.LocateStarImageReady:
                    if(m_maxim.Camera.ImageReady)
                    {
                        AddLogItem("Locating suitable focus star");

                        maxPixel = (long)m_maxim.Camera.MaxPixel;
                        if(maxPixel < AppSettings.MinMaxPixel)
                        {
                            AddLogItem("Bright star not detected");
                            AutoFocusWorkFlow = WorkFlow.Start;
                            break;
                        }

                        if(AppSettings.Simulator)
                        {
                            if(Focuser.Position == 0)
                                Focuser.HFD = m_simulatorDataPoints[0];
                            else
                                Focuser.HFD = m_simulatorDataPoints[Math.Abs(Focuser.Position) / AppSettings.StepSize];
                            Focuser.FWHM = Focuser.HFD - 0.75;
                        }
                        else
                        {
                            Focuser.HFD = Math.Round(m_maxim.Camera.HalfFluxDiameter, 2);
                            Focuser.FWHM = Math.Round(m_maxim.Camera.FWHM, 2);
                        }

                        m_x = m_maxim.Camera.MaxPixelX;  // Get the X coordinate of the brightest star
                        m_y = m_maxim.Camera.MaxPixelY;  // Get the Y coordinate of the brightest star 

                        if(!MaxIm.CheckSubFrameBounds(m_x, m_y))
                        {
                            AddLogItem("Star is too close to image edge");
                            AddLogItem("Center star or decrease sub-frame size");
                            AddLogItem("Aborting auto-focus");
                            AutoFocusWorkFlow = WorkFlow.NoActivity;
                            buttonFocus_Click(this, null);  // Reset the auto-focus start/stop button
                            return;
                        }

                        // Draw a rectangle around the brightest star
                        dragRect = new int[] 
                        { 
                            m_x - (AppSettings.SubFrameSize / 2), 
                            m_y - (AppSettings.SubFrameSize / 2), 
                            m_x + (AppSettings.SubFrameSize / 2), 
                            m_y + (AppSettings.SubFrameSize / 2) 
                        };

                        MaxIm.Camera.Document.DragRectangle = dragRect;

                        // Full-frame bin (LocateStar) will either be 1x1 or 2x2, sub-frames are fixed at bin 1x1
                        // If the full-frame > bin, scale up the coordinates for our bin 1 sub-frames
                        if(AppSettings.BinFullFrame > AppSettings.BinSubFrame)  
                        {
                            m_x *= 2;
                            m_y *= 2;
                            
                            m_xStart = m_x;  // These x,y values will be used as the main reference point
                            m_yStart = m_y;  // x/y values from MaxPixelX/Y will be used as offsets
                            AddLogItem("Star found at " + m_xStart.ToString() + "," + m_yStart.ToString());
                        }

                        AutoFocusWorkFlow = WorkFlow.FrameStar;  // Now take a sub-frame image at (by default) bin 1
                    }
                    break;

                case WorkFlow.FrameStar:
                    if(!m_maxim.SubFrame(m_x, m_y))
                    {
                        AddLogItem("Star frame image failed");
                        AddLogItem("Retrying auto-focus");
                        AutoFocusWorkFlow = WorkFlow.Start;
                        return;
                    }

                    AutoFocusWorkFlow = WorkFlow.FrameStarImageReady;
                    break;

                case WorkFlow.FrameStarImageReady:
                    if(m_maxim.Camera.ImageReady)
                    {
                        AddLogItem("Framing star");

                        maxPixel = (long)m_maxim.Camera.MaxPixel;
                        if(maxPixel < AppSettings.MinMaxPixel)
                        {
                            AddLogItem("Star not detected");
                            AutoFocusWorkFlow = WorkFlow.Start;
                            break;
                        }

                        if(AppSettings.Simulator)
                        {
                            if(Focuser.Position == 0)
                                Focuser.HFD = m_simulatorDataPoints[0];
                            else
                                Focuser.HFD = m_simulatorDataPoints[Math.Abs(Focuser.Position) / AppSettings.StepSize];
                            Focuser.FWHM = Focuser.HFD - 0.75;
                        }
                        else
                        {
                            Focuser.HFD = Math.Round(m_maxim.Camera.HalfFluxDiameter, 2);
                            Focuser.FWHM = Math.Round(m_maxim.Camera.FWHM, 2);
                        }

                        AutoFocusWorkFlow = WorkFlow.MoveToStart;
                    }
                    break;

                case WorkFlow.MoveToStart:
                    AddLogItem("Moving focuser to start position");
                    m_focus.Focuser.Move(AppSettings.MaxIn);  // Move in by the maximum amount allowable, relative from the current (zero) position
                    Focuser.Position = AppSettings.MaxIn;

                    AutoFocusWorkFlow = WorkFlow.MoveToStartFocuserReady;
                    break;

                case WorkFlow.MoveToStartFocuserReady:
                    if(!m_focus.Focuser.IsMoving)  // Wait for focuser to finish moving
                    {
                        AddLogItem("Focuser is at Max-In position (" + Focuser.Position.ToString() + ")");
                        
                        // We now start taking a series of images while moving the focuser from the 
                        // max-in to the max-out focuser position. This gives us a provisional BFP

                        AutoFocusWorkFlow = WorkFlow.FocusOut;  
                    }
                    break;

                case WorkFlow.FocusOut: 
                    // Take a series of images while moving from MaxIn to MaxOut position
                    if(Focuser.Position < -(AppSettings.MaxOut))
                    {
                        Focuser.Position = -(AppSettings.MaxOut);
                        AddLogItem("Focuser is at Max-Out position");

                        // Get the provisional best focus position (PBFP) from the series of data points we collected
                        m_pbfp = GetBFP();
                        if(m_pbfp == null)
                        {
                            MessageBoxWindow.Show(this, "", "No provisional best focus point found");
                            MessageBoxWindow.Show(this, "", "Aborting auto-focus attempt");
                            AutoFocusWorkFlow = WorkFlow.Start;
                            return;
                        }

                        // We now have a provisional BFP. Goto BFP + (2 * StepSizeNearFocus) 
                        AddLogItem("PBFP (HFD: " + m_pbfp.Hfd.ToString() + ") at pos. " + m_pbfp.FocuserPosition.ToString());

                        AutoFocusWorkFlow = WorkFlow.MoveToPBFPOffset;
                        break;
                    }
                    else
                    {
                        AddLogItem("Taking image at position " + Focuser.Position.ToString());
                        if(!m_maxim.SubFrame(m_x, m_y))
                        {
                            AddLogItem("Sub-frame image failed");
                            AddLogItem("Retrying auto-focus");
                            AutoFocusWorkFlow = WorkFlow.Start;
                            return;
                        }
                        AutoFocusWorkFlow = WorkFlow.FocusOutImageReady;  // We now wait for the image to be ready
                    }
                    
                    break;

                case WorkFlow.FocusOutImageReady:
                    if(m_maxim.Camera.ImageReady)
                    {
                        maxPixel = (long)m_maxim.Camera.MaxPixel;
                        if(maxPixel < AppSettings.MinMaxPixel)
                        {
                            AddLogItem("Star not detected");
                            AutoFocusWorkFlow = WorkFlow.Start;
                            break;
                        }

                        // The X/Y coordinates of the brightest star need to be scaled so that we correctly 
                        // reference full-frame coordinates (the MaxPixelX/Y coords are in sub-frame units)
                        m_x = (short)(m_xStart + (m_maxim.Camera.MaxPixelX - (AppSettings.SubFrameSize / 2)));
                        m_y = (short)(m_yStart + (m_maxim.Camera.MaxPixelY - (AppSettings.SubFrameSize / 2)));

                        AddLogItem("Creating data point at " + Focuser.Position.ToString() + " (" + m_x.ToString() + "," + m_y.ToString() + ")");

                        if(AppSettings.Simulator)
                        {
                            if(Focuser.Position == 0)
                                Focuser.HFD = m_simulatorDataPoints[0];
                            else
                                Focuser.HFD = m_simulatorDataPoints[Math.Abs(Focuser.Position) / AppSettings.StepSize];
                            Focuser.FWHM = Focuser.HFD - 0.75;
                        }
                        else
                        {
                            Focuser.HFD = Math.Round(m_maxim.Camera.HalfFluxDiameter, 2);
                            Focuser.FWHM = Math.Round(m_maxim.Camera.FWHM, 2);
                        }

                        // Take a data point at the current focus position
                        m_focusDataPoints.Add(
                            new FocusPoint(
                                Focuser.Position, 
                                Focuser.FWHM, 
                                Focuser.HFD, 
                                m_x, 
                                m_y, 
                                AppSettings.BinSubFrame, 
                                maxPixel));  // Create a data point

                        // Add a new plot point for the graph
                        FocusPlotPoints.Add(new KeyValuePair<int, double>(Focuser.Position, Focuser.HFD));
                        if(m_graph != null)
                            m_graph.RefreshData();  // This will add the new plot point 

                        AutoFocusWorkFlow = WorkFlow.FocusOutNextPosition;
                    }
                    break;

                case WorkFlow.FocusOutNextPosition:
                    Focuser.Position -= AppSettings.StepSize;  // Move out to the next position

                    if(Focuser.Position < -(AppSettings.MaxOut))
                    {
                        AutoFocusWorkFlow = WorkFlow.FocusOut;  
                        break;
                    }

                    m_focus.Focuser.Move(-AppSettings.StepSize);
                    AutoFocusWorkFlow = WorkFlow.FocusOutNextPositionFocuserReady;
                    break;

                case WorkFlow.FocusOutNextPositionFocuserReady:
                    // Wait for focuser to finish moving
                    if(!m_focus.Focuser.IsMoving)
                    {
                        AddLogItem("Focuser is now at " + Focuser.Position.ToString());
                        AutoFocusWorkFlow = WorkFlow.FocusOut;  // Carry on with our series of images
                    }
                    break;

                case WorkFlow.MoveToPBFPOffset:
                    // We now need to goto PBFP + (StepSizeNearFocusMultiplier * StepSizeNearFocus) -> i.e. back past the PBFP in the 
                    // direction of MaxIn, then return in the direction of MaxOut taking images until we get to 
                    // PBFP - (StepSizeNearFocusMultiplier * StepSizeNearFocus) 
                    AddLogItem("Moving focuser to PBFP offset start");

                    // Calculate relative move from current position - we should be at MaxOut
                    // Remember that moves are relative, so whatever the PBFP this move WILL be positive (in from MaxOut)
                    // Also, remember that MaxOut is stored as a positive integer (the calcs below work for both neg 
                    // and pos FocuserPosition values)
                    int relMove =  (AppSettings.MaxOut + m_pbfp.FocuserPosition) + (AppSettings.StepSizeNearFocusMultiplier * AppSettings.StepSizeNearFocus);
                    Focuser.Position = m_pbfp.FocuserPosition + (AppSettings.StepSizeNearFocusMultiplier * AppSettings.StepSizeNearFocus);  // This is the offset position on the "in" side
                    m_pbfpOffsetEndPos = m_pbfp.FocuserPosition - (AppSettings.StepSizeNearFocusMultiplier * AppSettings.StepSizeNearFocus);  // This is the offset on the "out" side
                    m_focus.Focuser.Move(relMove);

                    // Create a new list of graph plot points and re-scale the x and y axis ranges
                    m_focusPlotPoints = new List<KeyValuePair<int, double>>();
                    if(m_graph != null)
                    {
                        m_graph.DataSource = m_focusPlotPoints;
                        m_graph.XAxis.Maximum = Focuser.Position;
                        m_graph.XAxis.Minimum = m_pbfpOffsetEndPos;
                        m_graph.YAxis.Maximum = AppSettings.MaxHFD;
                        m_graph.YAxis.Minimum = AppSettings.MinHFD;
                    }

                    m_focusDataPoints.Clear();  // Start a new list of focus data points;

                    AutoFocusWorkFlow = WorkFlow.MoveToPBFPOffsetFocuserReady;
                    break;

                case WorkFlow.MoveToPBFPOffsetFocuserReady:
                    if(!m_focus.Focuser.IsMoving)  // Wait for focuser to finish moving
                    {
                        AddLogItem("Focuser at PBFP offset (" + Focuser.Position.ToString() + ")");
                        // We now start taking a series of images while moving the focuser from the 
                        // current position to PBFP - (StepSizeNearFocusMultiplier * StepSizeNearFocus). This gives us a confirmed BFP

                        AutoFocusWorkFlow = WorkFlow.FocusOutPBFP;  
                    }                    
                    break;

                case WorkFlow.FocusOutPBFP:
                    // Take a series of images while moving between the PBFP "in/out" offset positions
                    if(Focuser.Position < m_pbfpOffsetEndPos)
                    {
                        Focuser.Position = m_pbfpOffsetEndPos;
                        AddLogItem("Focuser at PBFP offset end position");

                        // Get the *confirmed* best focus position (BFP) from the series of data points we collected between the PBFP offsets
                        m_bfp = GetBFP();
                        if(m_bfp == null)
                        {
                            MessageBoxWindow.Show(this, "", "Unable to locate best focus point");
                            AutoFocusWorkFlow = WorkFlow.Start;  // Re-try
                            return;
                        }

                        // We now have a confirmed BFP - go to it!

                        AddLogItem("Best focus found (HFD: " + m_bfp.Hfd.ToString() + ") at pos. " + m_bfp.FocuserPosition.ToString());

                        AutoFocusWorkFlow = WorkFlow.MoveToBFP;
                        break;
                    }
                    else
                    {
                        AddLogItem("Taking image at position " + Focuser.Position.ToString());

                        if(!m_maxim.SubFrame(m_x, m_y))
                        {
                            AddLogItem("Sub-frame image failed");
                            AddLogItem("Retrying auto-focus");
                            AutoFocusWorkFlow = WorkFlow.Start;
                            return;
                        }
                        AutoFocusWorkFlow = WorkFlow.FocusOutPBFPImageReady;  // We now wait for the image to be ready
                    }
                    break;

                case WorkFlow.FocusOutPBFPImageReady:
                    if(m_maxim.Camera.ImageReady)
                    {
                        maxPixel = (long)m_maxim.Camera.MaxPixel;
                        if(maxPixel < AppSettings.MinMaxPixel)
                        {
                            AddLogItem("Star not detected");
                            AutoFocusWorkFlow = WorkFlow.Start;
                            break;
                        }

                        // The X/Y coordinates of the brightest star need to be scaled so that we correctly 
                        // reference full-frame coordinates (the MaxPixelX/Y coords are in sub-frame units)
                        m_x = (short)(m_xStart + (m_maxim.Camera.MaxPixelX - (AppSettings.SubFrameSize / 2)));
                        m_y = (short)(m_yStart + (m_maxim.Camera.MaxPixelY - (AppSettings.SubFrameSize / 2)));

                        AddLogItem("Creating data point at " + Focuser.Position.ToString() + " (" + m_x.ToString() + "," + m_y.ToString() + ")");

                        Focuser.HFD = Math.Round(m_maxim.Camera.HalfFluxDiameter, 2);
                        Focuser.FWHM = Math.Round(m_maxim.Camera.FWHM, 2);

                        if(AppSettings.Simulator)
                        {
                            Focuser.HFD = (Focuser.HFD - 2) + ((double)(Math.Abs(Focuser.Position) / AppSettings.StepSizeNearFocus));
                            Focuser.FWHM = Focuser.HFD - 0.75;
                        }

                        // Take a data point at the current focus position
                        m_focusDataPoints.Add(
                            new FocusPoint(
                                Focuser.Position, 
                                Focuser.FWHM, 
                                Focuser.HFD, 
                                m_x, 
                                m_y, 
                                AppSettings.BinSubFrame, 
                                maxPixel));  // Create a data point

                        // Add a new plot point for the graph
                        FocusPlotPoints.Add(new KeyValuePair<int, double>(Focuser.Position, Focuser.HFD));
                        if(m_graph != null)
                            m_graph.RefreshData();  // This will add the new plot point

                        AutoFocusWorkFlow = WorkFlow.FocusOutPBFPNextPosition;
                    }
                    break;

                case WorkFlow.FocusOutPBFPNextPosition:
                    Focuser.Position -= AppSettings.StepSizeNearFocus;  // Move out to the next position
                    if(Focuser.Position < m_pbfpOffsetEndPos)
                    {
                        AutoFocusWorkFlow = WorkFlow.FocusOutPBFP;  
                        return;
                    }

                    m_focus.Focuser.Move(-AppSettings.StepSizeNearFocus);
                    AutoFocusWorkFlow = WorkFlow.FocusOutPBFPNextPositionFocuserReady;
                    break;

                case WorkFlow.FocusOutPBFPNextPositionFocuserReady:
                    // Wait for focuser to finish moving
                    if(!m_focus.Focuser.IsMoving)
                    {
                        AddLogItem("Focuser is now at " + Focuser.Position.ToString());
                        AutoFocusWorkFlow = WorkFlow.FocusOutPBFP;  // Carry on with our series of images
                    }
                    break;

                case WorkFlow.MoveToBFP:
                    AddLogItem("Moving to best focus position");

                    // Calculate relative move from current position
                    int relMoveBFP = (AppSettings.MaxIn - Focuser.Position) - (AppSettings.MaxIn - m_bfp.FocuserPosition);
                    Focuser.Position = m_bfp.FocuserPosition;
                    m_focus.Focuser.Move(relMoveBFP);

                    AutoFocusWorkFlow = WorkFlow.MoveToBFPFocuserReady;
                    break;

                case WorkFlow.MoveToBFPFocuserReady:
                    if(!m_focus.Focuser.IsMoving)  // Wait for focuser to finish moving
                    {
                        AddLogItem("Auto-focus complete:");
                        AddLogItem("-->Relative position: " + Focuser.Position.ToString());
                        AddLogItem("-->HFD: " + m_bfp.Hfd.ToString());
                        AddLogItem("-->FWHM: " + m_bfp.Fwhm.ToString());
                        AutoFocusWorkFlow = WorkFlow.NoActivity;
                        buttonFocus_Click(this, null);  // Reset the auto-focus start/stop button                        
                    }
                    break;

                default:
                    break;
            }
        }

        private FocusPoint GetBFP()
        {
            if(m_focusDataPoints.Count == 0)
                return null;

            FocusPoint bfp = new FocusPoint(0, 99, 99, 0, 0, 1, 0);  // "Blank" current BFP - we'll never get a real HFD/FWHM of 99
            foreach(FocusPoint fp in m_focusDataPoints)
            {
                if(fp.Hfd == 0)
                    continue;

                if(fp.Hfd < bfp.Hfd)
                    bfp = fp;
            }

            if(bfp.Hfd == 99)
                return null;

            return bfp;
        }
        #endregion

        #region Control Event Handlers
        protected void buttonFocus_Click(object sender, RoutedEventArgs e)
        {
            if(AutoFocusInProgress)
            {
                AutoFocusInProgress = false;
                AutoFocusWorkFlow = WorkFlow.NoActivity;
            }
            else
            {
                // Start auto-focus
                if(AppSettings.Simulator)
                {
                    int nSteps = AppSettings.MaxIn / AppSettings.StepSize;
                    double increment = (double)((20.0 / nSteps));  // ~20 is the maximum value for our simulated HFD values
                    m_simulatorDataPoints = new double[nSteps+1];

                    for(int i = 0; i <= nSteps; i++)
                        m_simulatorDataPoints[i] = Math.Round(5 + (i * increment), 2);  // 5 is the lowest simulated HFD
                }

                m_currentAutoFocusTry = 0;
                AutoFocusInProgress = true;
                AddLogItem("Starting Auto-Focus...");
                AutoFocusWorkFlow = WorkFlow.Start;
            }
        }

        protected void buttonMoreOptions_Click(object sender, RoutedEventArgs e)
        {
            PropertiesWindow props = new PropertiesWindow();
            props.Left = this.Left + 5;
            props.Top = this.Top + 35;
            props.m_mainWnd = this;
            props.Init();
            props.ShowDialog();
        }

        protected void checkBoxMaximRunning_Checked(object sender, RoutedEventArgs e)
        {
            if(!m_maxim.MaximConnected)
                m_maxim.MaximConnected = true;
        }

        protected void checkBoxMaximRunning_Unchecked(object sender, RoutedEventArgs e)
        {
            if(m_maxim.MaximConnected)
            {
                m_maxim.MaximConnected = false;
            }
        }

        private void checkBoxCamera_Checked(object sender, RoutedEventArgs e)
        {
            if(!m_maxim.MaximConnected)
                m_maxim.MaximConnected = true;

            if(!m_maxim.CameraConnected)
                m_maxim.CameraConnected = true;
        }

        private void checkBoxCamera_Unchecked(object sender, RoutedEventArgs e)
        {
            if(m_maxim.MaximConnected)
                m_maxim.CameraConnected = false;
        }

        protected void buttonFocuserSelect_Click(object sender, RoutedEventArgs e)
        {
            string tmpFocuserID = ASCOM.DriverAccess.Focuser.Choose(AppSettings.FocuserID);  // The chooser returns the device progID
            if(string.IsNullOrEmpty(tmpFocuserID))
                return;

            // Re-create the focuser object, hook into the property changed event and setup bindings
            m_focus = new Focus(tmpFocuserID, AppSettings.MaxIn, AppSettings.MaxOut);
            m_focus.PropertyChanged += new PropertyChangedEventHandler(m_focus_PropertyChanged);

            buttonFocuserSelect.DataContext = Focuser;      // XAML binding: IsEnabled="{Binding Path=FocuserNotConnected}" 
            checkBoxFocuserConnected.DataContext = Focuser; // XAML binding: IsChecked="{Binding Path=FocuserConnected}"
            textBoxHFD.DataContext = Focuser;               // XAML binding: Text="{Binding Path=HFD}"
            textBoxFWHM.DataContext = Focuser;              // XAML binding: Text="{Binding Path=FWHM}"
            textBoxPosition.DataContext = Focuser;          // XAML binding: Text="{Binding Path=Position, Mode=OneWay, FallbackValue=-}"

            // Map the progID returned by the ASCOM chooser to the friendly device name
            AppSettings.FocuserID = tmpFocuserID;
            AppSettings.FocuserName = "";
            if(AppSettings.FocuserList != null)
            {
                foreach(ASCOM.Utilities.KeyValuePair device in AppSettings.FocuserList)
                {
                    if(device.Key.ToString().CompareTo(AppSettings.FocuserID) == 0)
                    {
                        AppSettings.FocuserName = device.Value.ToString();
                        break;
                    }
                }
            }

            if(string.IsNullOrEmpty(AppSettings.FocuserName))
                AppSettings.FocuserName = AppSettings.FocuserID;
        }

        protected void checkBoxFocuserConnected_Checked(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(AppSettings.FocuserID))
            {
                MessageBoxWindow.Show(this, "", "Please select a Focuser before attempting to connect");
                Focuser.FocuserConnected = false;
                return;
            }

            try
            {
                if(!m_focus.FocuserConnected)
                    m_focus.FocuserConnected = true;

                AddLogItem("Focuser is " + AppSettings.FocuserName);
            }
            catch(Exception ex)
            {
                MessageBoxWindow.Show(this, "", "Error connecting to Focuser: " + ex.Message);
                Focuser.FocuserConnected = false;
            }
        }

        protected void checkBoxFocuserConnected_Unchecked(object sender, RoutedEventArgs e)
        {
            if(m_focus != null && m_focus.FocuserConnected)
                m_focus.FocuserConnected = false;
        }

        protected void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(m_nudge != null)
                m_nudge.Close();  // Close the focus nudge window

            if(m_graph != null)
                m_graph.Close();  // Close the graph window

            if(m_focus != null)
                AppSettings.FocuserID = m_focus.ProgID;

            AppSettings.Left = this.Left;
            AppSettings.Ontop = this.Topmost;
            AppSettings.Top = this.Top;
            AppSettings.WriteConfig();

            string[] log = new string[listBoxFocusMonitorLog.Items.Count];
            for(int i = listBoxFocusMonitorLog.Items.Count - 1; i >= 0; i--)
                log[i] = (string)listBoxFocusMonitorLog.Items[i];
            AppSettings.WriteLog(log);
        }

        protected void checkBoxOnTop_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        protected void checkBoxOnTop_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }

        protected void buttonNudge_Click(object sender, RoutedEventArgs e)
        {
            if(m_nudge != null)
                return;  // Window already open

            m_nudge = new Nudge();  // Create a floating focuser nudge window
            m_nudge.Init(this);
            m_nudge.Show();
            m_nudge.Closed += new EventHandler(NudgeWindowClosed);
        }

        protected void NudgeWindowClosed(object sender, EventArgs e)
        {
            m_nudge = null;
        }

        protected void buttonGraph_Click(object sender, RoutedEventArgs e)
        {
            if(m_graph != null)
                return;  // The graph window is already open

            if(!AutoFocusInProgress)  // If we're in the middle of auto-focusing then m_focusPlotPoints will have been created in the Workflow processing method
                m_focusPlotPoints = new List<KeyValuePair<int, double>>();  // ... otherwise, create an empty data source

            m_graph = new AutoFocusGraph();
            m_graph.Init(this);
            m_graph.DataSource = m_focusPlotPoints;
            m_graph.XAxis.Maximum = AppSettings.MaxIn;
            m_graph.XAxis.Minimum = -(AppSettings.MaxOut);
            m_graph.YAxis.Maximum = AppSettings.MaxHFD;
            m_graph.YAxis.Minimum = AppSettings.MinHFD;
            m_graph.Show();
            m_graph.Closed += new EventHandler(m_graph_Closed);
        }

        protected void m_graph_Closed(object sender, EventArgs e)
        {
            m_graph = null;
        }
        #endregion

        #region Misc Methods
        private void buttonLoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".fit"; // Default file extension
            ofd.Filter = "FITS Image|*.fit;*.fits;*.fts|JPEG Image|*.jpg;*.jpeg|TIF Image|*.tif;*.tiff"; // Filter files by extension
            Nullable<bool> result = ofd.ShowDialog();
            if(result == false)
                return;

            BlockingOperation blockingOp = new BlockingOperation(this.LoadImage);  // This could tie-up the UI so we'll do it on a new thread
            blockingOp.BeginInvoke(ofd.FileName, this.ImageHasLoaded, null);
        }
        private void LoadImage(string filename)
        {
            try
            {
                // Open the image in MaxIm
                m_maxim.Camera.StartX = 0;
                m_maxim.Camera.StartY = 0;
                m_maxim.Camera.NumX = 1;
                m_maxim.Camera.NumY = 1;
                m_maxim.Camera.Expose(0, 1, m_maxim.Camera.Filter);  // Create a new document - there doesn't seem to be any other way of doing this!
                while(m_maxim.Camera.ImageReady == false)
                    System.Threading.Thread.Sleep(250);

                m_maxim.Camera.Document.OpenFile(filename);  // Get the document object for the last image taken and load a file into it
            }
            catch(Exception ex)
            {
                // We're on a different thread from the UI, so we need this:
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
                {
                    MessageBoxWindow.Show(this, "", "Error loading image: " + ex.Message);
                }));
            }
        }

        private void ImageHasLoaded(IAsyncResult result)
        {
            // We're on a different thread from the UI, so we need this:
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
            {
                AddLogItem("Image loaded OK");
            }));
        }
        #endregion

        #region PropertyChanged event handlers
        protected void OnPropertyChanged(string propertyName)
        {
            // Raise property change events from this main window
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void m_focus_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Raise property change events from the Focus class
            // Bubble-up property changes (changes to any of the focuser properties potentially affect our AutoFocusButtonEnabled property)
            if(e.PropertyName.CompareTo("FocuserConnected") == 0)
                this.OnPropertyChanged("AutoFocusButtonEnabled");
        }

        protected void m_maxim_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Raise property change events from the MaxIm class
            // Bubble-up any property changes that affect our AutoFocusButtonEnabled property
            if(e.PropertyName.CompareTo("CameraConnected") == 0 ||
                e.PropertyName.CompareTo("MaximConnected") == 0 ||
                e.PropertyName.CompareTo("MaximAndCameraConnected") == 0)
                this.OnPropertyChanged("AutoFocusButtonEnabled");
        }
        #endregion

    }
}
