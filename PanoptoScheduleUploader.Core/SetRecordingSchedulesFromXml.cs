using System;
using System.Collections.Generic;
using System.Configuration;
using PanoptoScheduleUploader.Services;
using PanoptoScheduleUploader.Services.RemoteRecorderManagement;
using System.Text;

namespace PanoptoScheduleUploader.Core
{
    public static class SetRecordingSchedulesFromXml
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static IEnumerable<SchedulingResult> Execute(string username, string password, string xmlFile, string folderName, bool overwrite)
        {
            var results = new List<SchedulingResult>();

            var parser = new RecorderScheduleXmlParser(xmlFile);
            var recordings = parser.ExtractRecordings();

            using (var sessionManager = new SessionManagementWrapper(username, password))
            {
                using (var remoteRecorderService = new RemoteRecorderManagementWrapper(username, password))
                {
                    using (var userManager = new UserManagementWrapper(username, password))
                    {
                        foreach (var recording in recordings)
                        {
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
                                results.Add(new SchedulingResult(false, string.Format("The recorder {0} could not be found.", recording.RecorderName), Guid.Empty));
                            }
                            else
                            {
                                var defaultFolderId = Guid.Empty;
                                var defaultFolderName = ConfigurationManager.AppSettings["defaultFolderName"];
                                var folder = sessionManager.GetFolderByName(defaultFolderName);
                                if (folder != null)
                                {
                                    defaultFolderId = folder.Id;
                                }
                                else
                                {
                                    throw new Exception(string.Format("The folder named '{0}' does not exist. This folder must exist as the default location for recordings.", defaultFolderName));
                                }

                                if (overwrite)
                                {
                                    sessionManager.RemoveConflictingSessions(remoteRecorderService.GetSessionsByRecorderName(recording.RecorderName), recording.StartTime, recording.EndTime);
                                }

                                var folderId = GetFolderId(recording.CourseTitle, sessionManager, defaultFolderId);
                                //Unused at present
                                //var user = userManager.GetUserByUserName(recording.Presenter);
                                var result = remoteRecorderService.ScheduleRecording(recording.Title, folderId, false, recording.StartTime, recording.EndTime, new List<RecorderSettings> { settings });
                                if (result.SessionId != Guid.Empty)
                                {
                                    sessionManager.UpdateSessionDescription(result.SessionId, "Presented by " + recording.Presenter);
                                }
                                results.Add(result);
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

            if (!string.IsNullOrEmpty(folderName))
            {
                var folder = sessionManager.GetFolderByName(folderName);

                if (folder != null)
                {
                    folderId = folder.Id;
                }
            }

            return folderId;
        }
    }
}
