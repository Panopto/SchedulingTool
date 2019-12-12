using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PanoptoScheduleUploader.Services;
using PanoptoScheduleUploader.Services.RemoteRecorderManagement;
using PanoptoScheduleUploader.Core;
using System.Configuration;

namespace PanoptoScheduleUploader
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            var xmlFile = @"..\..\..\local_test.xml";
            var username = "Admin";
            var password = "panopto123";
            var folderName = "Test";

            using (var sessionManager = new SessionManagementWrapper(username, password))
            {
                var folderId = GetFolderId(folderName, sessionManager);

                using (var remoteRecorderService = new RemoteRecorderManagementWrapper(username, password))
                {
                    using (var usageManager = new UsageManagementWrapper(username, password))
                    {
                        SessionFilter filter = new SessionFilter();

                        //usageManager.IsSessionUsageOK();
                    }
                }
            }

            try
            {
                var parser = new RecorderScheduleXmlParser(xmlFile);
                var recordings = parser.ExtractRecordings();

                using (var sessionManager = new SessionManagementWrapper(username, password))
                {
                    var folderId = GetFolderId(folderName, sessionManager);

                    using (var remoteRecorderService = new RemoteRecorderManagementWrapper(username, password))
                    {
                        //var settings = remoteRecorderService.GetSettingsByRecorderName("EDKNIGHT-PC");
                        //remoteRecorderService.ScheduleRecording("TestRecording", folderId, false, DateTime.Now, DateTime.Now.AddMinutes(1), new List<RecorderSettings> { settings });

                        foreach (var recording in recordings)
                        {
                            var settings = remoteRecorderService.GetSettingsByRecorderName(recording.RecorderName);
                            remoteRecorderService.ScheduleRecording(recording.Title, folderId, false, recording.StartTime, recording.EndTime, new List<RecorderSettings> { settings }, false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(e);
                Console.WriteLine("An error has occurred:  {0}", e.Message);
                Console.WriteLine("See the log for more details.");
            }

            Console.ReadLine();
        }

        private static Guid GetFolderId(string folderName, SessionManagementWrapper sessionManager)
        {
            var folderId = Guid.NewGuid();
            var defaultFolderId = ConfigurationManager.AppSettings["defaultFolderId"];

            if (!string.IsNullOrWhiteSpace(defaultFolderId))
            {
                folderId = Guid.Parse(defaultFolderId);
            }
            else
            {
                var folder = sessionManager.GetFolderByName(folderName);

                if (folder == null)
                {
                    throw new Exception("Folder not found!");
                }

                folderId = folder.Id;
            }
            return folderId;
        }
    }
}
