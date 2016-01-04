using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace PanoptoScheduleUploader.Core
{
    /// <summary>
    /// Takes an xml file and parses it to extract recording schedules.
    /// </summary>
    public class RecorderScheduleXmlParser
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private XmlDocument document;
        private Dictionary<int, string> invalidRecordings = new Dictionary<int, string>();

        /// <summary>
        /// Initializes a new instance of the RecorderScheduleXmlParser class.
        /// </summary>
        /// <param name="filepath">The path to the xml file containing the recorder schedules.</param>
        public RecorderScheduleXmlParser(string filepath)
        {
            if (log.IsDebugEnabled) log.DebugFormat("Loading XML document from {0}", filepath);

            try
            {
                this.document = new XmlDocument();
                this.document.Load(filepath);
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                throw;
            }
        }

        /// <summary>
        /// Extracts the RecorderSchedule elements from the xml file and converts them into a <see cref="Recording"/> object.
        /// </summary>
        /// <returns>A list of Recording objects.</returns>
        public IEnumerable<Recording> ExtractRecordings()
        {
            if (log.IsDebugEnabled) log.DebugFormat("Extracting recordings from XML file.");

            var recorderSchedules = new List<Recording>();
            try
            {
                var recorderSchedulesXml = this.document.DocumentElement.GetElementsByTagName("RecorderSchedule");

                for (int i = 0; i < recorderSchedulesXml.Count; i++)
                {
                    var recorderScheduleXmlElement = new RecorderScheduleXmlElement(recorderSchedulesXml[i]);

                    var startTime = DateTime.Parse(string.Format("{0} {1}", recorderScheduleXmlElement.RecordingDate, recorderScheduleXmlElement.RecordingStartTime));
                    var endTime = DateTime.Parse(string.Format("{0} {1}", recorderScheduleXmlElement.RecordingDate, recorderScheduleXmlElement.RecordingEndTime));
                    recorderSchedules.Add(new Recording
                    {
                        Title = recorderScheduleXmlElement.Class,
                        IsBroadCast = false,
                        FolderId = 0,
                        RecorderName = recorderScheduleXmlElement.Classroom,
                        StartTime = startTime,
                        EndTime = endTime,
                        Presenter = recorderScheduleXmlElement.Presenter,
                        CourseTitle = recorderScheduleXmlElement.CourseTitle,
                        RecordingDate = recorderScheduleXmlElement.RecordingDate
                    });
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                throw;
            }

            return recorderSchedules;
        }
    }
}
