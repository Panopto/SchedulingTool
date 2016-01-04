using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PanoptoScheduleUploader.Core
{
    public class Recording
    {
        public int FolderId { get; set; }
        public bool IsBroadCast { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string RecorderName { get; set; }
        public string Title { get; set; }
        public string Presenter { get; set; }
        public string CourseTitle { get; set; }
        public string RecordingDate { get; set; }
    }
}
