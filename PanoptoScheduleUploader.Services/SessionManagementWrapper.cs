using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using PanoptoScheduleUploader.Services.SessionManagement;
using System.ServiceModel;

namespace PanoptoScheduleUploader.Services
{
    public class SessionManagementWrapper : IDisposable
    {
        public SessionManagementClient sessionManager;
        AuthenticationInfo authentication;

        public SessionManagementWrapper(string username, string password)
        {
            this.sessionManager = new SessionManagementClient();

            // Ensure server certificate is validated
            CertificateValidation.EnsureCertificateValidation();

            this.authentication = new AuthenticationInfo()
            {
                UserKey = username,
                Password = password
            };
        }

        public Folder GetFolderByName(string folderName)
        {
            int resultPerPage = 10;
            bool folderFound = false;
            Folder result = null;
            var pagination = new Pagination { MaxNumberResults = resultPerPage, PageNumber = 0 };
            var response = this.sessionManager.GetFoldersList(this.authentication, new ListFoldersRequest { Pagination = pagination }, null);

            string[] lineage = folderName.Split(new string[]{ConfigurationManager.AppSettings["folderPathDelimiter"]}, StringSplitOptions.None);
            foreach (Folder folder in response.Results)
            {
                if (folder.Name == lineage[lineage.Length - 1])
                {
                    if (FolderParentsMatchInput(folder,lineage))
                    {
                        folderFound = true;
                        result = folder;
                        break;
                    }
                }
            }

            // Get more data while there are more to get
            int totalResults = response.TotalNumberResults;
            int currentResults = resultPerPage;

            while (currentResults < totalResults && !folderFound)
            {
                pagination.PageNumber += 1;
                response = this.sessionManager.GetFoldersList(this.authentication, new ListFoldersRequest { Pagination = pagination }, null);
                foreach (Folder folder in response.Results)
                {
                    if (folder.Name == lineage[lineage.Length - 1])
                    {
                        if (FolderParentsMatchInput(folder,lineage))
                        {
                            folderFound = true;
                            result = folder;
                            break;
                        }
                    }
                }
                currentResults += resultPerPage;
            }

            return result;

        }

        private Boolean FolderParentsMatchInput(Folder checkFolder, string[] lineage)
        {
            Folder currentParent = checkFolder;
            for (int i = lineage.Length - 2; i >= 0; --i)
            {
                if (currentParent.ParentFolder == null)
                {
                    return false;
                }
                Folder[] newParentAsArray = this.sessionManager.GetFoldersById(this.authentication, new Guid[] { (System.Guid)currentParent.ParentFolder });
                currentParent = newParentAsArray[0];
                if (currentParent.Name != lineage[i])
                {
                    return false;
                }
            }
            return true;
        }

        public Session[] GetSessionsInDateRange(DateTime start, DateTime end)
        {
            Session[] result = null;

            int itemPerPage = 10;
            int pageNum = 0;
            int totalItem = 0;
            int itemRead = 0;
            bool firstRun = true;
            int resultIndex = 0;

            do
            {
                Pagination pagination = new Pagination() { MaxNumberResults = itemPerPage, PageNumber = pageNum };
                var response = sessionManager.GetSessionsList(this.authentication, new ListSessionsRequest() { Pagination = pagination, StartDate = start, EndDate = end }, null);
                if (response == null)
                {
                    throw new Exception(string.Format("Unable to fetch sessions between dates {0} and {1}", start.ToString(), end.ToString()));
                }

                totalItem = response.TotalNumberResults;
                itemRead += itemPerPage;
                pageNum++;

                if (firstRun)
                {
                    firstRun = false;
                    result = new Session[totalItem];
                }

                foreach (Session session in response.Results)
                {
                    result[resultIndex] = session;
                    resultIndex++;
                }

            } while (itemRead < totalItem);

            return result;
        }

        public Session[] GetSessionById(Guid id)
        {
            List<Guid> ids = new List<Guid>();
            ids.Add(id);
            return this.sessionManager.GetSessionsById(this.authentication, ids.ToArray());
        }

        public bool TryGetSessionId(string sessionName, out Guid sessionId)
        {
            sessionId = Guid.NewGuid();
            var pagination = new Pagination { MaxNumberResults = 25, PageNumber = 0 };
            var sessions = this.sessionManager.GetSessionsList(this.authentication, new ListSessionsRequest {  Pagination = pagination }, null);

            var session = sessions.Results.SingleOrDefault(s => s.Name == sessionName);

            if (session != null)
            {
                sessionId = session.Id;
                return true;
            }

            return false;
        }

        public void UpdateSessionDescription(Guid id, string description)
        {
            List<Guid> ids = new List<Guid>();
            ids.Add(id);
            this.sessionManager.UpdateSessionDescription(this.authentication, ids[0], description);
        }

        public void RemoveConflictingSessions(Guid[] sessions, DateTime startTime, DateTime endTime)
        {
            List<Guid> sessionsToDelete = new List<Guid>();
            foreach (var session in sessionManager.GetSessionsById(this.authentication, sessions))
            {
                DateTime localTime = session.StartTime.Value.ToLocalTime();
                if (IsOverlap(localTime, localTime.AddSeconds((Double)session.Duration), startTime, endTime))
                {
                    sessionsToDelete.Add(session.Id);
                }
            }

            if (sessionsToDelete.Count > 0)
            {
                sessionManager.DeleteSessions(this.authentication, sessionsToDelete.ToArray());
            }
        }

        public void DeleteSessions(Guid[] sessionIds)
        {
            sessionManager.DeleteSessions(this.authentication, sessionIds);
        }

        public bool IsOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            if (start1 >= end2) return false;
            if (end1 <= start2) return false;
            return true;
        }

        public void Dispose()
        {
            if (this.sessionManager.State == CommunicationState.Faulted)
            {
                this.sessionManager.Abort();
            }

            if (this.sessionManager.State != CommunicationState.Closed)
            {
                this.sessionManager.Close();
            }
        }
    }
}
