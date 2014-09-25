using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PanoptoScheduleUploader.Services.UsageManagement;
using System.ServiceModel;
using PanoptoScheduleUploader.Services.SessionManagement;

namespace PanoptoScheduleUploader.Services
{
    public class UsageManagementWrapper : IDisposable
    {
        UsageReportingClient usageManager;
        PanoptoScheduleUploader.Services.UsageManagement.AuthenticationInfo authentication;

        public UsageManagementWrapper(string username, string password)
        {
            this.usageManager = new UsageReportingClient();

            // Ensure server certificate is validated
            CertificateValidation.EnsureCertificateValidation();

            this.authentication = new PanoptoScheduleUploader.Services.UsageManagement.AuthenticationInfo()
            {
                UserKey = username,
                Password = password
            };
        }

        public SessionUsage IsSessionUsageOK(Session session, SessionFilter filter)
        {
            SessionUsage sessUsage = new SessionUsage();

            IEnumerable<SummaryUsageResponseItem> response = usageManager.GetSessionSummaryUsage(
                this.authentication,
                session.Id, //The session Guid
                session.StartTime.Value, //From when the session was created
                DateTime.Now, //Until now
                UsageGranularity.Hourly);

            if (response != null)
            {
                var sessionUsage = response.FirstOrDefault();

                if (sessionUsage != null)
                {
                    sessUsage.MinutesViewed = sessionUsage.MinutesViewed;
                    sessUsage.NumberOfViews = sessionUsage.Views;
                    sessUsage.NumberOfVisitors = sessionUsage.UniqueUsers;

                    if (sessionUsage.MinutesViewed < filter.MinutesViewed || sessionUsage.Views < filter.NumberOfViews || sessionUsage.UniqueUsers < filter.NumberOfVisitors)
                    {
                        sessUsage.IsOK = false;
                    }
                    else
                    {
                        sessUsage.IsOK = true;
                    }
                }
            }
            return sessUsage;
        }

        public void Dispose()
        {
            if (this.usageManager.State == CommunicationState.Faulted)
            {
                this.usageManager.Abort();
            }

            if (this.usageManager.State != CommunicationState.Closed)
            {
                this.usageManager.Close();
            }
        }
    }
}
