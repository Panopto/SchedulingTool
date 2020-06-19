using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Microsoft.VisualBasic.FileIO;

namespace PanoptoScheduleUploader.Core
{
    public class RecorderScheduleCSVParser
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private List<string[]> document;
        private Dictionary<int, string> invalidRecordings = new Dictionary<int, string>();

        private int titleIndex = 0;
        private int recorderIndex = 1;
        private int dateIndex = 2;
        private int startTimeIndex = 3;
        private int endTimeIndex = 4;
        private int presenterIndex = 5;
        private int folderIndex = 6;
        private int webcast = 7;

        /// <summary>
        /// Initializes a new instance of the RecorderScheduleCSVParser class.
        /// </summary>
        /// <param name="filepath">The path to the CSV file containing the recorder schedules.</param>
        public RecorderScheduleCSVParser(string filepath)
        {
            if (log.IsDebugEnabled) log.DebugFormat("Loading CSV document from {0}", filepath);

            this.document = new List<string[]>();
            string[] fields;

            try
            {
                using (TextFieldParser parser = new TextFieldParser(filepath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(ConfigurationManager.AppSettings["CSVDelimiter"]);
                    parser.HasFieldsEnclosedInQuotes = true;

                    while (!parser.EndOfData)
                    {
                        fields = parser.ReadFields();
                        this.document.Add(fields);
                    }
                }
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
                foreach (string[] elements in document)
                {
                    var startTime = DateTime.Parse(string.Format("{0} {1}", elements[dateIndex], elements[startTimeIndex]));
                    var endTime = DateTime.Parse(string.Format("{0} {1}", elements[dateIndex], elements[endTimeIndex]));
                    bool b = false;
                    if (elements[webcast] == "1")
                    {
                        b = true;
                    }
                    if (elements[webcast] == "0")
                    {
                        b = true;
                    }
                    else
                    {
                        bool.TryParse(elements[webcast], out b);
                    }
                    recorderSchedules.Add(new Recording
                    {
                        Title = elements[titleIndex],
                        IsBroadCast = b,
                        FolderId = 0,
                        RecorderName = elements[recorderIndex],
                        StartTime = startTime,
                        EndTime = endTime,
                        Presenter = elements[presenterIndex],
                        CourseTitle = elements[folderIndex],
                        RecordingDate = elements[dateIndex]
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
