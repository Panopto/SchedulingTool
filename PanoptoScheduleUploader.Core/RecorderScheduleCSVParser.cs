using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PanoptoScheduleUploader.Core
{
    public class RecorderScheduleCSVParser
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string[] document;
        private Dictionary<int, string> invalidRecordings = new Dictionary<int, string>();

        private int titleIndex = 0;
        private int recorderIndex = 1;
        private int dateIndex = 2;
        private int startTimeIndex = 3;
        private int endTimeIndex = 4;
        private int presenterIndex = 5;
        private int folderIndex = 6;

        /// <summary>
        /// Initializes a new instance of the RecorderScheduleCSVParser class.
        /// </summary>
        /// <param name="filepath">The path to the CSV file containing the recorder schedules.</param>
        public RecorderScheduleCSVParser(string filepath)
        {
            if (log.IsDebugEnabled) log.DebugFormat("Loading CSV document from {0}", filepath);

            try
            {
                this.document = System.IO.File.ReadAllLines(filepath);
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                throw;
            }
        }

        /// <summary>
        /// Extracts the information needed to schedule recordings from the CSV file and converts them into a <see cref="Recording"/> object.
        /// </summary>
        /// <returns>A list of Recording objects.</returns>
        public IEnumerable<Recording> ExtractRecordings()
        {
            if (log.IsDebugEnabled) log.DebugFormat("Extracting recordings from CSV file.");

            var recorderSchedules = new List<Recording>();
            try
            {
                for (int i = 0; i < document.Length; i++)
                {
                    string[] elements = new string[7];
                    int idx = 0;
                    foreach (string elem in document[i].Split(','))
                    {
                        if (idx == 7)
                        {
                            break;
                        }

                        elements[idx] = elem;
                        idx++;
                    }
                    while (idx < 7)
                    {
                        elements[idx] = null;
                        idx++;
                    }

                    var startTime = DateTime.Parse(string.Format("{0} {1}", elements[dateIndex], elements[startTimeIndex]));
                    var endTime = DateTime.Parse(string.Format("{0} {1}", elements[dateIndex], elements[endTimeIndex]));
                    recorderSchedules.Add(new Recording
                    {
                        Title = string.Format("{0} {1}", elements[titleIndex], elements[dateIndex]),
                        IsBroadCast = false,
                        FolderId = 0,
                        RecorderName = elements[recorderIndex],
                        StartTime = startTime,
                        EndTime = endTime,
                        Presenter = elements[presenterIndex],
                        CourseTitle = elements[folderIndex]
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
