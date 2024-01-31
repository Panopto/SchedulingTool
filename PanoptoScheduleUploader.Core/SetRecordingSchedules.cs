using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel;
using System.Windows.Forms;
using PanoptoScheduleUploader.Services;
using PanoptoScheduleUploader.Services.RemoteRecorderManagement;

using System.ComponentModel;

namespace PanoptoScheduleUploader.Core
{
    public static class SetRecordingSchedules
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string MSGBOX_FOLDERNAME_NOTFOUND_TITLE = "Folder does not exist";
        private const string MSGBOX_FOLDERNAME_NOTFOUND_PROMPT = "The folder named '{0}' does not exist for recording '{1}'. Would you like to use the default folder instead?";
        private const string FOLDERNAME_NOTFOUND_MSG = "The folder named '{0}' does not exist for recording '{1}'.";
      
        public static IEnumerable<SchedulingResult> Execute( BackgroundWorker bw, string username, string password, string fileName, string folderName, bool overwrite)
        {

            var results = new List<SchedulingResult>();

            IEnumerable<Recording> recordings = null;
            if (System.IO.Path.GetExtension(fileName) == ".xml")
            {
                var parser = new RecorderScheduleXmlParser(fileName);
                recordings = parser.ExtractRecordings();
            }
            else if (System.IO.Path.GetExtension(fileName) == ".csv")
            {
                var parser = new RecorderScheduleCSVParser(fileName);
                recordings = parser.ExtractRecordings();
            }
            else
            {
                throw new NotImplementedException("Expected an xml or csv file. Got filename: " + fileName);
            }

            using (var sessionManager = new SessionManagementWrapper(username, password))
            {
                using (var remoteRecorderService = new RemoteRecorderManagementWrapper(username, password))
                {
                    using (var userManager = new UserManagementWrapper(username, password))
                    {
                        int count = 0;
                        foreach (var recording in recordings)
                        {
                            count++;
                            Trace.WriteLine($"{count}: {recording.Title}");

                            RecorderSettings settings;
                            try
                            {
                                settings = remoteRecorderService.GetSettingsByRecorderName(recording.RecorderName);
                            }
                            catch (Exception)
                            {
                                throw;
                            }

                            if (settings == null)
                            {
                                if (log.IsDebugEnabled) log.DebugFormat($"Cannot find Recorder {recording.RecorderName}:, {recording.Title},{recording.CourseTitle},{recording.StartTime},{recording.RecorderName}");
                                results.Add(new SchedulingResult(false, string.Format("The recorder {0} could not be found.", recording.RecorderName), Guid.Empty));
                                bw.ReportProgress(1, $"The recorder {recording.RecorderName} could not be found." );
                            }
                            else
                            {
                                
                                var folderId = GetFolderId(recording.CourseTitle, sessionManager, Guid.Empty);
                                var defaultFolderName = ConfigurationManager.AppSettings["defaultFolderName"];
                                // If recording specifies a folder and cannot be found prompt user if they want to use default folder (can be disabled by the config file)
                                if (recording.CourseTitle != null && folderId == Guid.Empty && ConfigurationManager.AppSettings["DisableNoMatchingFolderPrompt"] == "False")
                                {

                                    DialogResult dialogResult = MessageBox.Show(String.Format(MSGBOX_FOLDERNAME_NOTFOUND_PROMPT, recording.CourseTitle, recording.Title), MSGBOX_FOLDERNAME_NOTFOUND_TITLE, MessageBoxButtons.YesNo);
                                    if (dialogResult == DialogResult.No)
                                    {
                                        if (log.IsDebugEnabled) log.DebugFormat($"Skipped. Cannot find folder {recording.CourseTitle}. User opted to not record to default folder {defaultFolderName}:, {recording.Title},{recording.CourseTitle},{recording.StartTime},{recording.RecorderName}");
                                        results.Add(new SchedulingResult(false, string.Format($"Skipped. Cannot find folder {recording.CourseTitle} for {recording.Title} at {recording.StartTime}. User opted to not record to default folder {defaultFolderName}"), Guid.Empty));
                                        bw.ReportProgress(1, $"Skipped. Cannot find folder {recording.CourseTitle} for {recording.Title} at {recording.StartTime}. User opted to not record to default folder {defaultFolderName}");
                                        continue;
                                    }
                                }

                                if (folderId == Guid.Empty)
                                {
                                  
                                    var folder = sessionManager.GetFolderByName(defaultFolderName);
                                    if (folder != null)
                                    {
                                        folderId = folder.Id;
                                        if (log.IsDebugEnabled) log.DebugFormat($"Cannot find folder {recording.CourseTitle}. Recording to default folder {defaultFolderName}:, {recording.Title},{recording.CourseTitle},{recording.StartTime},{recording.RecorderName}");
                                        bw.ReportProgress(1, $"Cannot find folder {recording.CourseTitle}. Recording to default folder {defaultFolderName}");

                                    }
                                    else
                                    {
                                        if (log.IsDebugEnabled) log.DebugFormat($"Cannot find folder {recording.CourseTitle}. The folder named '{defaultFolderName}' does not exist. This folder must exist as the default location for recordings., {recording.Title},{recording.CourseTitle},{recording.StartTime},{recording.RecorderName}");
                                        results.Add(new SchedulingResult(false, string.Format($"Cannot find folder {recording.CourseTitle}. The folder named '{defaultFolderName}' does not exist. This folder must exist as the default location for recordings."), Guid.Empty));
                                        bw.ReportProgress(1, $"Cannot find folder {recording.CourseTitle}. The folder named '{defaultFolderName}' does not exist. This folder must exist as the default location for recordings.");
                                        continue;
                                        

                                    }
                                }
                                var result = remoteRecorderService.ScheduleRecording(recording.Title, folderId, recording.IsBroadCast, recording.StartTime, recording.EndTime, new List<RecorderSettings> { settings }, overwrite);
                                if (result.SessionId != Guid.Empty)
                                {
                                    if (result.Success == true && result.LogLine != null)
                                    {
                                        if (log.IsDebugEnabled) log.DebugFormat($"Scheduled overwritting previous session:, {result.SessionId},{recording.Title},{folderId},{recording.CourseTitle},{recording.StartTime},{recording.RecorderName},{result.LogLine}");
                                    }
                                    else
                                    {
                                        if (log.IsDebugEnabled) log.DebugFormat($"Scheduled:, {result.SessionId},{recording.Title},{folderId},{recording.CourseTitle},{recording.StartTime},{recording.RecorderName}");
                                    }
                                    if (recording.Presenter != "")
                                    {
                                        sessionManager.UpdateSessionDescription(result.SessionId, recording.Presenter);
                                    }
                                }
                                if (result.Success == false)
                                {
                                    if (log.IsDebugEnabled) log.DebugFormat($"Unable to schedule due to confict:,Conflicting session:{result.LogLine},{recording.Title},{folderId},{recording.CourseTitle},{recording.StartTime},{recording.RecorderName}");
                                }
                                results.Add(result);
                                bw.ReportProgress(1, result.Result);
                            }
                
                        }
                    }
                }
            }

            return results;
        }

        private static Guid GetFolderId(string folderName, SessionManagementWrapper sessionManager, Guid defaultFolderId)
        {
            var folderId = defaultFolderId;
            if (Guid.TryParse(folderName, out var newGuid))
            {
                var folderid = sessionManager.GetFolderById(newGuid);
                if (folderid.Id != new Guid("00000000-0000-0000-0000-000000000000"))
                {
                    folderId = folderid.Id;

                }
            }
            else
            {
                if (!string.IsNullOrEmpty(folderName))
                {
                    var folder = sessionManager.GetFolderByName(folderName);

                    if (folder != null)
                    {
                        folderId = folder.Id;
                    }
                }
            }
            return folderId;
        }
    }
}
