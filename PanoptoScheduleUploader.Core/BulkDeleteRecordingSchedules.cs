using System;
using System.Collections.Generic;
using System.Configuration;
using PanoptoScheduleUploader.Services;
using PanoptoScheduleUploader.Services.RemoteRecorderManagement;
using System.Text;
using PanoptoScheduleUploader.Services.SessionManagement;

namespace PanoptoScheduleUploader.Core
{
    public static class BulkDeleteRecordingSchedules
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static Dictionary<Session, SessionUsage> FetchSessions(string username, string password, SessionFilter filter)
        {
            Dictionary<Session, SessionUsage> retSessions = new Dictionary<Session, SessionUsage>();

            using (var sessionManager = new SessionManagementWrapper(username, password))
            {
                Session[] results = sessionManager.GetSessionsInDateRange(filter.StartDate.Value, filter.EndDate.Value);

                using (var usageManager = new UsageManagementWrapper(username, password))
                {
                    foreach (var session in results)
                    {
                        var usage = usageManager.IsSessionUsageOK(session, filter);
                        if (!usage.IsOK)
                        {
                            retSessions.Add(session, usage);
                        }
                    }
                }
            }

            return retSessions;
        }

        public static void DeleteSessions(string username, string password, Guid[] sessions)
        {
            using (var sessionManager = new SessionManagementWrapper(username, password))
            {
                sessionManager.DeleteSessions(sessions);
            }
        }

    }
}
