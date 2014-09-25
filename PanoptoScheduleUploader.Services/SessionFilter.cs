using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PanoptoScheduleUploader.Services
{
    public class SessionFilter
    {
        #region Internals

        private double _Views = 0;
        private double _Minutes = 0;
        private double _Visitors = 0;
        private DateTime? _StartDate = null;
        private DateTime? _EndDate = null;

        #endregion

        #region Constructors

        public SessionFilter()
        { 
        }

        public SessionFilter(DateTime? start, DateTime? end, double numberOfViews, double minsViewed, double numberOfVisitors)
        {
            _StartDate = start;
            _EndDate = end;
            _Views = numberOfViews;
            _Minutes = minsViewed;
            _Visitors = numberOfViews;
        }

        #endregion

        #region Properties

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

        public DateTime? StartDate
        {
            get
            {
                return _StartDate;
            }
            set
            {
                _StartDate = value;
            }
        }

        public DateTime? EndDate
        {
            get
            {
                return _EndDate;
            }
            set
            {
                _EndDate = value;
            }
        }

        #endregion
    }
}

