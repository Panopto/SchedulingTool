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
        private const int RESULTS_PER_PAGE = 250; // Max is 10,000
        public SessionManagementClient sessionManager;
        AuthenticationInfo authentication;

        private Dictionary<string, Folder> folderByNameCache;
        
        private Dictionary<Guid, Folder> folderByIdCache;

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

            // Initialize caches
            this.folderByNameCache = new Dictionary<string, Folder>();
            this.folderByIdCache = new Dictionary<Guid, Folder>();
        }

        public Folder GetFolderByName(string folderName)
        {
            string caseInvariantFolderKey = folderName.ToLower();
            if (!this.folderByNameCache.ContainsKey(caseInvariantFolderKey))
            {
                Folder result = GetChosenFolder(folderName);
                this.folderByNameCache[caseInvariantFolderKey] = result;
                // If the result was not null, save in the id cache too as an optimization
                if (result != null)
                {
                    this.folderByIdCache[result.Id] = result;
                }
            }

            return this.folderByNameCache[caseInvariantFolderKey];
        }

        private Folder GetChosenFolder(string folderName)
        {
            Folder result = null;

            if (this.folderByNameCache.ContainsKey(folderName))
            {
                result = this.folderByNameCache[folderName];
            }
            else
            {
                // Gather all folders with a matching name, and show them to the user with the full folder paths
                // So that the user may select which one they intend
                List<Folder> matchingFolders = GetAllMatchingFolders(folderName);
                if (matchingFolders.Count > 1)
                {
                    StringBuilder chosenFolder = new StringBuilder();
                    FolderChooser chooser = new FolderChooser(GetFullFolderStrings(matchingFolders), chosenFolder, folderName);
                    chooser.ShowDialog();
                    result = matchingFolders[Int32.Parse(chosenFolder.ToString())];
                }
                else
                {
                    result = matchingFolders.SingleOrDefault();
                }
            }
            return result;
        }

        private List<Folder> GetAllMatchingFolders(String folderName)
        {
            List<Folder> result = new List<Folder>();

            
            // Set a limit which should never be reached of 100 pages of 50
            for (int page = 0; page < 100; page++)
            {
                Pagination pagination = new Pagination { MaxNumberResults = RESULTS_PER_PAGE, PageNumber = page };
                ListFoldersResponse response = this.sessionManager.GetFoldersList(
                    this.authentication,
                    new ListFoldersRequest {
                        Pagination = pagination,
                        WildcardSearchNameOnly = true,
                    },
                    folderName);
                result.AddRange(response.Results);
                if (result.Count >= response.TotalNumberResults)
                {
                    break;
                }
            }

        
            // Filter down to case-invariant exact matches
            result = result.Where(f => String.Equals(f.Name, folderName, StringComparison.InvariantCultureIgnoreCase)).ToList();
            return result;
        }

        public Folder GetFolderById(Guid id)
        {
            if (!this.folderByIdCache.ContainsKey(id))
            {
                this.folderByIdCache[id] = this.sessionManager.GetFoldersById(
                    this.authentication,
                    new Guid[] { id }
                ).Single();
            }
            return this.folderByIdCache[id];
        }

        private string[] GetFullFolderStrings(List<Folder> matchingFolders)
        {
            string[] folderStrings = new string[matchingFolders.Count];
            for (int i = 0; i < matchingFolders.Count; ++i)
            {
                Folder currentParent = matchingFolders[i];
                // Build the string names by walking the parent list
                string folderPath = currentParent.Name;
                while (currentParent.ParentFolder.HasValue)
                {
                    currentParent = this.GetFolderById(currentParent.ParentFolder.Value);
                    folderPath = currentParent.Name + "/" + folderPath;
                }
                folderStrings[i] = folderPath;
            }
            return folderStrings;
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
                ListSessionsResponse response = sessionManager.GetSessionsList(this.authentication, new ListSessionsRequest() { Pagination = pagination, StartDate = start, EndDate = end }, null);
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
            Pagination pagination = new Pagination { MaxNumberResults = 25, PageNumber = 0 };
            ListSessionsResponse sessions = this.sessionManager.GetSessionsList(this.authentication, new ListSessionsRequest { Pagination = pagination }, null);

            Session session = sessions.Results.SingleOrDefault(s => s.Name == sessionName);

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
      
        //public bool RemoveConflictingSessions(Guid[] sessions, DateTime startTime, DateTime endTime)
        //{
        //    List<Guid> sessionsToDelete = new List<Guid>();
        //    foreach (Session session in sessionManager.GetSessionsById(this.authentication, sessions))
        //    {
        //        DateTime localTime = session.StartTime.Value.ToLocalTime();
        //        if (IsOverlap(localTime, localTime.AddSeconds((Double)session.Duration), startTime, endTime))
        //        {
        //            sessionsToDelete.Add(session.Id);
        //        }
        //    }

        //    if (sessionsToDelete.Count > 0)
        //    {
        //        sessionManager.DeleteSessions(this.authentication, sessionsToDelete.ToArray());
        //        return true;
        //    }
        //    return false;
        //}

        public bool DeleteSessions(Guid[] sessionIds)
        {
            try
            {
                sessionManager.DeleteSessions(this.authentication, sessionIds);
                return true;
            }
            catch
            {
                return false;
            }
   

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
