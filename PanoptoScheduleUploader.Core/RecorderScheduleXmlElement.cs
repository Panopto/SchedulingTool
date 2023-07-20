using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace PanoptoScheduleUploader.Core
{
    /// <summary>
    /// Extracts the RecorderSchedule element properties from an XML node.
    /// </summary>
    public class RecorderScheduleXmlElement
    {
        /// <summary>
        /// Initializes anew instance of the RecorderScheduleXmlElement class.
        /// </summary>
        /// <param name="recorderScheduleNode">The RecorderSchedule node.</param>
        public RecorderScheduleXmlElement(XmlNode recorderScheduleNode)
        {
            if (recorderScheduleNode.Name != "RecorderSchedule")
            {
                throw new Exception("The node is not a RecorderSchedule element.");
            }

            this.Class = recorderScheduleNode.SelectSingleNode("Class") != null ? recorderScheduleNode.SelectSingleNode("Class").InnerText : "";
            this.Classroom = recorderScheduleNode.SelectSingleNode("Classroom") != null ? recorderScheduleNode.SelectSingleNode("Classroom").InnerText : "";
            this.RecordingDate = recorderScheduleNode.SelectSingleNode("RecordingDate") != null ? recorderScheduleNode.SelectSingleNode("RecordingDate").InnerText : "";
            this.RecordingStartTime = recorderScheduleNode.SelectSingleNode("RecordingStartTime") != null ? recorderScheduleNode.SelectSingleNode("RecordingStartTime").InnerText : ""; 
            this.RecordingEndTime = recorderScheduleNode.SelectSingleNode("RecordingEndTime") != null ? recorderScheduleNode.SelectSingleNode("RecordingEndTime").InnerText : "";
            this.Presenter = recorderScheduleNode.SelectSingleNode("Presenter") != null ? recorderScheduleNode.SelectSingleNode("Presenter").InnerText : "";
            this.CourseTitle = recorderScheduleNode.SelectSingleNode("CourseTitle") != null ? recorderScheduleNode.SelectSingleNode("CourseTitle").InnerText : "";
            this.TemplateId = recorderScheduleNode.SelectSingleNode("TemplateId") != null ? recorderScheduleNode.SelectSingleNode("TemplateId").InnerText : "";

            if (!this.HasAllValues)
            {
                throw new Exception("The Recorder Schedule XML element does not contain all expected properties.");
            }
        }

        public string Class { get; set; }
        public string Classroom { get; set; }
        public string RecordingDate { get; set; }
        public string RecordingStartTime { get; set; }
        public string RecordingEndTime { get; set; }
        public string Presenter { get; set; }
        public string CourseTitle { get; set; }
        public string TemplateId { get; set; }

        /// <summary>
        /// Gets a value indicating whether the node contains values for all the expected properties on a RecorderSchedule node.
        /// </summary>
        private bool HasAllValues
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this.Class)
                   && !string.IsNullOrWhiteSpace(this.Classroom)
                   && !string.IsNullOrWhiteSpace(this.RecordingDate)
                   && !string.IsNullOrWhiteSpace(this.RecordingStartTime)
                   && !string.IsNullOrWhiteSpace(this.RecordingEndTime);
            }
        }
    }
}
