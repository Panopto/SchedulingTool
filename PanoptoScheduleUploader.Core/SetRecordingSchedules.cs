﻿using System;
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
                                bool overwritten = false;
                                if (overwrite)
                                {
                                    overwritten = sessionManager.RemoveConflictingSessions(remoteRecorderService.GetSessionsByRecorderName(recording.RecorderName), recording.StartTime, recording.EndTime);
                                }
                                var folderId = GetFolderId(recording.CourseTitle, sessionManager, Guid.Empty);

                                // If recording specifies a folder and cannot be found prompt user if they want to use default folder
                                if (recording.CourseTitle != null && folderId == Guid.Empty)
                                {
                                    DialogResult dialogResult = MessageBox.Show(String.Format(MSGBOX_FOLDERNAME_NOTFOUND_PROMPT, recording.CourseTitle, recording.Title), MSGBOX_FOLDERNAME_NOTFOUND_TITLE, MessageBoxButtons.YesNo);
                                    if (dialogResult == DialogResult.No)
                                    {
                                        throw new Exception(string.Format(FOLDERNAME_NOTFOUND_MSG, recording.CourseTitle, recording.Title));
                                    }
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
                                var result = remoteRecorderService.ScheduleRecording(recording.Title, folderId, false, recording.StartTime, recording.EndTime, new List<RecorderSettings> { settings }, overwritten);
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
