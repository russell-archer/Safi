using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using ASCOM.DriverAccess;

namespace Safi
{ 
    /// <summary>
    /// This class encapsulates the ASCOM Focuser driver
    /// </summary>
    class Focus : INotifyPropertyChanged
    {
        #region Declarations
        public event PropertyChangedEventHandler PropertyChanged;

        private Focuser m_focus = null;
        private string m_progID;
        private double m_hfd;
        private double m_fwhm;
        private int m_position;  // Focuser internal 'position' (releative focusers don't support the Position property)
        private int m_maxIn;
        private int m_maxOut;

        #endregion

        #region Properties
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

        public int Position
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = value;
                this.OnPropertyChanged("Position");
            }
        }
        
        public double HFD
        {
            get
            {
                return m_hfd;
            }
            set
            {
                m_hfd = value;
                this.OnPropertyChanged("HFD");
            }
        }

        public double FWHM
        {
            get
            {
                return m_fwhm;
            }
            set
            {
                m_fwhm = value;
                this.OnPropertyChanged("FWHM");
            }
        }

        public Focuser Focuser
        {
            get
            {
                return this.m_focus;
            }
        }

        public string ProgID
        {
            get
            {
                return m_progID;
            }
            set
            {
                m_progID = value;
                this.OnPropertyChanged("ProgID");
            }
        }

        public bool FocuserConnected
        {
            get
            {
                if(m_focus == null)
                    return false;  // Focuser is not connected
                else
                {
                    try
                    {
                        // Check that the Focuser really is connected by trying to access the Link property
                        if(m_focus.Link)
                            return true;
                        else
                            return false;
                    }
                    catch
                    {
                        // There was a null-ptr exception accessing the Focuser - the connection's been closed
                        m_focus = null;
                        return false;
                    }
                }
            }

            set
            {
                if(value == true)
                {
                    if(string.IsNullOrEmpty(this.ProgID))
                        throw new Exception("Focuser ProgID must be set before attempting to connect");

                    if(m_focus == null)
                        m_focus = new Focuser(m_progID);
                }

                m_focus.Link = value;
                this.OnPropertyChanged("FocuserConnected");
                this.OnPropertyChanged("FocuserNotConnected");
            }
        }

        public bool FocuserNotConnected
        {
            get
            {
                return !FocuserConnected;
            }
        }
        #endregion

        #region Construction
        public Focus() : this("", 0, 0)
        {
        }

        public Focus(string progID, int maxIn, int maxOut)
        {
            this.ProgID = progID;
            this.HFD = 0;
            this.FWHM = 0;
            this.Position = 0;
        }
        #endregion

        #region Public Methods
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void MoveFocuser(int relativeMove)
        {
            MoveFocuser(relativeMove, true);
        }

        public void MoveFocuser(int relativeMove, bool rangeCheck)
        {
            if(m_focus != null && this.FocuserConnected)
            {
                if(relativeMove > 0 && rangeCheck && (Position + relativeMove > this.MaxIn))
                    relativeMove = this.MaxIn - Position;
                else if(relativeMove < 0 && rangeCheck && (Position + relativeMove < -(this.MaxOut)))
                    relativeMove = -(Position + this.MaxOut);

                m_focus.Move(relativeMove);
                Position += relativeMove;
            }
        }

        public void MoveToZero()
        {
            int relativeMove = 0;
            if(m_focus != null && this.FocuserConnected)
            {
                if(Position > 0)
                    relativeMove = -(Position);
                else if(Position < 0)
                    relativeMove = Math.Abs(Position);

                m_focus.Move(relativeMove);
                Position = 0;
            }
        }
        #endregion
    }
}
