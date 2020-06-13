using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace Safi
{
    class FocusPoint
    {
        #region Declarations
        private int m_focuserPosition;                          // Focuser position when the image was taken
        private double m_fwhm;                                  // FWHM when the image was taken
        private double m_hfd;                                   // HFD when the image was taken
        private short m_x;                                      // X coordinate of the star
        private short m_y;                                      // Y coordinate of the star
        private short m_bin;                                    // Binning value when the image was taken
        private long m_maxPixel;                                // The star's brightest pixel value
        private KeyValuePair<int, double> m_positionHfd;        // A key/value pair used by the graph plot

        #endregion

        #region Construction
        protected FocusPoint() 
        {  
            // The public parameterized constructor should be used 
        }

        public FocusPoint(int    focuserPosition,
                          double fwhm,
                          double hfd,
                          short  x,
                          short  y,
                          short  bin,
                          long   maxPixel)
        {
            m_focuserPosition   = focuserPosition;
            m_fwhm              = fwhm;
            m_hfd               = hfd;
            m_x                 = x;
            m_y                 = y;
            m_bin               = bin;
            m_maxPixel          = maxPixel;
            m_positionHfd = new KeyValuePair<int, double>(focuserPosition, hfd);
        }
        #endregion

        #region Properties
        public KeyValuePair<int, double> PositionHfd
        {
            get
            {
                return m_positionHfd;
            }
        }

        public long MaxPixel
        {
            get
            {
                return m_maxPixel;
            }
            set
            {
                m_maxPixel = value;
            }
        }

        public int FocuserPosition
        {
            get
            {
                return m_focuserPosition;
            }
            set
            {
                m_focuserPosition = value;
            }
        }

        public double Fwhm
        {
            get
            {
                return m_fwhm;
            }
            set
            {
                m_fwhm = value;
            }
        }

        public double Hfd
        {
            get
            {
                return m_hfd;
            }
            set
            {
                m_hfd = value;
            }
        }

        public short X
        {
            get
            {
                return m_x;
            }
            set
            {
                m_x = value;
            }
        }

        public short Y
        {
            get
            {
                return m_y;
            }
            set
            {
                m_y = value;
            }
        }

        public short Bin
        {
            get
            {
                return m_bin;
            }
            set
            {
                m_bin = value;
            }
        }
        #endregion
    }
}
