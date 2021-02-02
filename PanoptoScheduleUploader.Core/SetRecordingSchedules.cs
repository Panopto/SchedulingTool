using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using PanoptoScheduleUploader.Services;
using PanoptoScheduleUploader.Services.RemoteRecorderManagement;
using System.Text;
using System.Windows.Forms;

namespace PanoptoScheduleUploader.Core
{
    public static class SetRecordingSchedules
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string MSGBOX_FOLDERNAME_NOTFOUND_TITLE = "Folder does not exist";
        private const string MSGBOX_FOLDERNAME_NOTFOUND_PROMPT = "The folder named '{0}' does not exist for recording '{1}'. Would you like to use the default folder instead?";
        private const string FOLDERNAME_NOTFOUND_MSG = "The folder named '{0}' does not exist for recording '{1}'.";
        public static IEnumerable<SchedulingResult> Execute(string username, string password, string fileName, string folderName, bool overwrite)
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

            using (var sessionManager = new SessionManagementWrapper(username, password))
            {
                using (var remoteRecorderService = new RemoteRecorderManagementWrapper(username, password))
                {
                    List<RemoteRecorder> recorders = remoteRecorderService.GetSettingsByRecorderName();
                    using (var userManager = new UserManagementWrapper(username, password))
                    {

                        foreach (var recording in recordings)
                        {
                            bool retry = true;
                            int attemptCount = 0;
                            while (retry && attemptCount < 10)
                            {
                                try
                                {
                                    RecorderSettings settings = new RecorderSettings();

                                    try
                                    {
                                        RemoteRecorder result = recorders.Where(i => i.Name == recording.RecorderName).FirstOrDefault(); ;
                                        settings = new RecorderSettings { RecorderId = result.Id };

                                    }
                                    catch (Exception)
                                    {
                                        results.Add(new SchedulingResult(false, string.Format("The recorder {0} could not be found.", recording.RecorderName), Guid.Empty));
                                        retry = false;
                                    }

                                    if (settings == new RecorderSettings() || settings.RecorderId == new Guid("00000000-0000-0000-0000-000000000000"))
                                    {
                                        if (log.IsDebugEnabled) log.DebugFormat($"Cannot find Recorder {recording.RecorderName}:, {recording.Title},{recording.CourseTitle},{recording.RecordingDate},{recording.StartTime},{recording.RecorderName}");
                                        results.Add(new SchedulingResult(false, string.Format("The recorder {0} could not be found.", recording.RecorderName), Guid.Empty));
                                        retry = false;
                                    }
                                    else
                                    {

                                        //if (overwrite)
                                        //{
                                        //    overwritten = sessionManager.RemoveConflictingSessions(remoteRecorderService.GetSessionsByRecorderName(recording.RecorderName), recording.StartTime, recording.EndTime);
                                        //}
                                        var folderId = GetFolderId(recording.CourseTitle, sessionManager, Guid.Empty);

                                        // If recording specifies a folder and cannot be found prompt user if they want to use default folder
                                        if (recording.CourseTitle != null && folderId == Guid.Empty)
                                        {

                                            //DialogResult dialogResult = MessageBox.Show(String.Format(MSGBOX_FOLDERNAME_NOTFOUND_PROMPT, recording.CourseTitle, recording.Title), MSGBOX_FOLDERNAME_NOTFOUND_TITLE, MessageBoxButtons.YesNo);
                                            //if (dialogResult == DialogResult.No)
                                            //{
                                            //    throw new Exception(string.Format(FOLDERNAME_NOTFOUND_MSG, recording.CourseTitle, recording.Title));
                                            //}
                                        }

                                        if (folderId == Guid.Empty)
                                        {
                                            var defaultFolderName = ConfigurationManager.AppSettings["defaultFolderName"];
                                            var folder = sessionManager.GetFolderByName(defaultFolderName);
                                            if (folder != null)
                                            {
                                                folderId = folder.Id;
                                            }
                                            else
                                            {
                                                throw new Exception(string.Format("The folder named '{0}' does not exist. This folder must exist as the default location for recordings.", defaultFolderName));
                                            }
                                        }
                                        var result = remoteRecorderService.ScheduleRecording(recording.Title, folderId, recording.IsBroadCast, recording.StartTime, recording.EndTime, new List<RecorderSettings> { settings }, overwrite, username, password);
                                        if (result.SessionId != Guid.Empty)
                                        {
                                            if (result.Success == true && result.LogLine != Guid.Empty.ToString())
                                            {
                                                if (log.IsDebugEnabled) log.DebugFormat($"Scheduled overwritting previous session:, {result.SessionId},{recording.Title},{folderId},{recording.CourseTitle},{recording.RecordingDate},{recording.StartTime},{recording.RecorderName},{result.LogLine}");
                                            }
                                            else
                                            {
                                                if (log.IsDebugEnabled) log.DebugFormat($"Scheduled:, {result.SessionId},{recording.Title},{folderId},{recording.CourseTitle},{recording.RecordingDate},{recording.StartTime},{recording.RecorderName}");
                                            }
                                            sessionManager.UpdateSessionDescription(result.SessionId, "Presented by " + recording.Presenter);
                                        }
                                        if (result.Success == false)
                                        {
                                            if (log.IsDebugEnabled) log.DebugFormat($"Unable to schedule due to confict:,Conflicting session:{result.LogLine},{recording.Title},{folderId},{recording.CourseTitle},{recording.RecordingDate},{recording.StartTime},{recording.RecorderName}");
                                        }
                                        results.Add(result);
                                    }
                                    retry = false;
                                }

                                catch (Exception ex)
                                {
                                    // only retry the call if it's possible this was a transient error (ie, a deadlock)
                                    retry &= (ex.Message == "An error occurred. See server logs for details") && (attemptCount < 10);
                                    if (retry)
                                    {
                                        // sleep for a bit before retrying
                                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(3));
                                    }
                                    else
                                    {
                                        // if we're not retrying, re-throw the exception
                                        if (log.IsDebugEnabled) log.DebugFormat($"Unable to schdule due to error:, {ex},{recording.Title},{Guid.Empty},{recording.CourseTitle},{recording.RecordingDate},{recording.StartTime},{recording.RecorderName}");
                                        continue;
                                    }
                                }
                            
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
