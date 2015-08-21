using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using PanoptoScheduleUploader.Services.SessionManagement;
using System.ServiceModel;
using System.Collections;

namespace PanoptoScheduleUploader.Services
{
    public class SessionManagementWrapper : IDisposable
    {
        private static String[] STOP_WORDS = new String[] {"a","an","and","are","as","at","be","but","by","for","if","in","into","is","it","no","not","of","on","or","such",
            "that","the","their","then","there","these","they","this","to","was","will","which"};

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

        public Folder TryGetFolderById(string id)
        {
            Guid guid;
            if (Guid.TryParse(id, out guid))
            {
                Folder[] folders = null;
                try
                {
                    folders = this.sessionManager.GetFoldersById(this.authentication, new Guid[] { guid });
                }
                catch (FaultException)
                {
                    //no folder of the given guid exists; this is fine and we should move on
                }
                return (folders == null || folders.Length == 0) ? null : folders[0];
            }
            return null;
        }

        public Folder GetFolderByName(string folderName)
        {
            folderName = folderName.ToLower();

            Folder result = null;

            ArrayList matchingFolders = GetAllMatchingFolders(folderName, folderName);
            //We couldn't find the folder - maybe it's using a stopword, so now we'll do a broader search
            if (matchingFolders.Count == 0 && StringContainsStopWord(folderName))
            {
                matchingFolders = GetAllMatchingFolders(folderName, null);
            }

            if (matchingFolders.Count > 0)
            {
                if (matchingFolders.Count == 1)
                {
                    result = (Folder)matchingFolders[0];
                }
                else
                {
                    StringBuilder folderHolder = new StringBuilder();
                    FolderChooser chooser = new FolderChooser(GetFullFolderStrings(matchingFolders), folderHolder, folderName);
                    chooser.ShowDialog();
                    result = (Folder)matchingFolders[Int32.Parse("" + folderHolder[0])];
                }
            }

            return result;

        }

        private ArrayList GetAllMatchingFolders(String folderName, String query)
        {
            int resultPerPage = 50;
            var pagination = new Pagination { MaxNumberResults = resultPerPage, PageNumber = 0 };
            var response = this.sessionManager.GetFoldersList(this.authentication, new ListFoldersRequest { Pagination = pagination }, query);

            ArrayList matchingFolders = new ArrayList();

            foreach (Folder folder in response.Results)
            {
                if (folder.Name.ToLower() == folderName)
                {
                    matchingFolders.Add(folder);
                }
            }

            // Get more data while there are more to get
            int totalResults = response.TotalNumberResults;
            int currentResults = resultPerPage;

            while (currentResults < totalResults)
            {
                pagination.PageNumber += 1;
                response = this.sessionManager.GetFoldersList(this.authentication, new ListFoldersRequest { Pagination = pagination }, query);
                foreach (Folder folder in response.Results)
                {
                    if (folder.Name.ToLower() == folderName)
                    {
                        matchingFolders.Add(folder);
                    }
                }
                currentResults += resultPerPage;
            }

            return matchingFolders;
        }

        private string[] GetFullFolderStrings(ArrayList matchingFolders)
        {
            string[] folderStrings = new string[matchingFolders.Count];
            for (int i = 0; i < matchingFolders.Count; ++i)
            {
                Folder currentParent = (Folder)matchingFolders[i];
                string folderPath = currentParent.Name;
                while (currentParent.ParentFolder != null)
                {
                    Folder[] newParentAsArray = this.sessionManager.GetFoldersById(this.authentication, new Guid[] { (System.Guid)currentParent.ParentFolder });
                    currentParent = newParentAsArray[0];
                    folderPath = currentParent.Name + "/" + folderPath;
                }
                folderStrings[i] = folderPath;
            }
            return folderStrings;
        }

        private bool StringContainsStopWord(string str)
        {
            for (int i = 0; i < STOP_WORDS.Length; ++i)
            {
                if (str.Contains(STOP_WORDS[i] + " ") || str.Contains(" " + STOP_WORDS[i]) || str.Equals(STOP_WORDS[i]))
                {
                    return true;
                }
            }
            return false;
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

        public bool RemoveConflictingSessions(Guid[] sessions, DateTime startTime, DateTime endTime)
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
                return true;
            }
            return false;
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
