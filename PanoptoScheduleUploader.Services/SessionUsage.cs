using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PanoptoScheduleUploader.Services
{
    public class SessionUsage
    {
        #region Internals

        private double _Views = 0;
        private double _Minutes = 0;
        private double _Visitors = 0;
        private bool _Ok = false;

        #endregion

        #region Constructors

        public SessionUsage()
        {
        }

        #endregion

        #region Properties

        public bool IsOK
        {
            get
            {
                return _Ok;
            }
            set
            {
                _Ok = value;
            }
        }

        public double NumberOfViews
        {
            get
            {
                return _Views;
            }
            set
            {
                _Views = value;
            }
        }

        public double MinutesViewed
        {
            get
            {
                return _Minutes;
            }
            set
            {
                _Minutes = value;
            }
        }

        public double NumberOfVisitors
        {
            get
            {
                return _Visitors;
            }
            set
            {
                _Visitors = value;
            }
        }

        #endregion
    }
}
