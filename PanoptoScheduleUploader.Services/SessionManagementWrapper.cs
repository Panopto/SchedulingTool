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
        public SessionManagementClient sessionManager;
        AuthenticationInfo authentication;

        private Dictionary<string, Folder> savedFolders;
        
        // Conflicting Folder names
        private Dictionary<string, List<Folder>> savedDupFolders;

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
            if (folderName != null)
            {
                folderName = folderName.ToLower();
            }

            Folder result = null;

            if (this.savedFolders == null)
            {
                GetAndSetFolderByList();
            }

            if (savedFolders != null)
            {
                if (this.savedFolders.ContainsKey(folderName))
                {
                    result = this.savedFolders[folderName];
                }
                else if(this.savedDupFolders.ContainsKey(folderName))
                {
                    StringBuilder folderHolder = new StringBuilder();
                    FolderChooser chooser = new FolderChooser(GetFullFolderStrings(this.savedDupFolders[folderName]), folderHolder, folderName);
                    chooser.ShowDialog();
                    result = this.savedDupFolders[folderName][Int32.Parse("" + folderHolder[0])];
                }
            }

            return result;

        }

        private void GetAndSetFolderByList()
        {
            ListFoldersResponse response = null;
            int resultPerPage = 250; // Max is 10,000
            int pageNumber = 0;
            Pagination pagination = new Pagination { MaxNumberResults = resultPerPage, PageNumber = pageNumber };

            response = this.sessionManager.GetFoldersList(this.authentication, new ListFoldersRequest { Pagination = pagination }, searchQuery: null);
            if (response != null)
            {
                savedFolders = new Dictionary<string, Folder>();
                savedDupFolders = new Dictionary<string,List<Folder>>();
                SaveFolders(response.Results);
            }

            int totalNumberResults = response.TotalNumberResults;
            if (response.TotalNumberResults > resultPerPage)
            {
                pageNumber++;
                while (totalNumberResults >= ((pageNumber) * resultPerPage))
                {
                    pagination = new Pagination { MaxNumberResults = resultPerPage, PageNumber = pageNumber };
                    response = this.sessionManager.GetFoldersList(this.authentication, new ListFoldersRequest { Pagination = pagination }, searchQuery: null);
                 
                    if (response != null)
                    {
                        SaveFolders(response.Results);
                    }
                    pageNumber++;
                }
            }
        }
        
        /// <summary>
        /// Save Unique Folder Names in this.savedFolders
        /// Save Duplicate Folder Names in this.savedDupFolder and Remove any existing duplicate folder names from this.savedFolders
        /// </summary>
        /// <param name="folders">Array of Folders to save</param>
        private void SaveFolders(Folder[] folders)
        {
            foreach (Folder folder in folders)
            {
                // Brand new entry
                if (!this.savedDupFolders.ContainsKey(folder.Name.ToLower()) && !this.savedFolders.ContainsKey(folder.Name.ToLower()))
                {
                    this.savedFolders.Add(folder.Name.ToLower(), folder);
                }
                // Exist in Duplicated Folder but not Original saved folder
                else if (this.savedDupFolders.ContainsKey(folder.Name.ToLower()) && !this.savedFolders.ContainsKey(folder.Name.ToLower()))
                {
                    this.savedDupFolders[folder.Name.ToLower()].Add(folder);
                }
                // Remove from Original folders and include all duplicates in a special Dictionary
                else
                {
                    List<Folder> tempFolderList = new List<Folder>();
                    tempFolderList.Add(this.savedFolders[folder.Name.ToLower()]);
                    tempFolderList.Add(folder);
                    this.savedDupFolders.Add(folder.Name.ToLower(), tempFolderList);

                    this.savedFolders.Remove(folder.Name.ToLower());
                }
            }
        }

        private string[] GetFullFolderStrings(List<Folder> matchingFolders)
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
