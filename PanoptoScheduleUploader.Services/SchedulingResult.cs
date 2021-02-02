using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PanoptoScheduleUploader.Services
{
    public class SchedulingResult
    {
        public SchedulingResult(string result, Guid sessionId)
        {
            this.Success = true;
            this.Result = result;
            this.SessionId = sessionId;
        }

        public SchedulingResult(bool success, string result, Guid sessionId)
        {
            this.Success = success;
            this.Result = result;
            this.SessionId = sessionId;
        }

        public SchedulingResult(bool success, string result, Guid sessionId, string logline)
        {
            this.Success = success;
            this.Result = result;
            this.SessionId = sessionId;
            this.LogLine = logline;
        }

        public bool Success { get; set; }
        public string Result { get; set; }
        public Guid SessionId { get; set; }
        public string LogLine { get; set; }
    }
}
