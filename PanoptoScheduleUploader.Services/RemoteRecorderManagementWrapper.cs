using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel;

using PanoptoScheduleUploader.Services.RemoteRecorderManagement;

namespace PanoptoScheduleUploader.Services
{
    public class RemoteRecorderManagementWrapper : IDisposable
    {
        private RemoteRecorderManagementClient remoteRecorderManager;

        private AuthenticationInfo authenticationInfo;
        private string dateTimeFormat;
        private Dictionary<string, RemoteRecorder> recorders;
        private object lockRecorders = new object();
        private string user;
        private string password;

        // RemoteRecorderManagement.ListRecorders() (Gets the list of all recorders and allows filtering by recorder name.)
        // RemoteRecorderManagement.ScheduleRecording() (Creates a new recording on a particular remote recorder.)
        // RemoteRecorderManagement.UpdateRecordingTime() (Allows modification of a previously scheduled recording.)

        public RemoteRecorderManagementWrapper(string user, string password)
        {
            // Instantiate service clients
            this.remoteRecorderManager = new RemoteRecorderManagementClient();


            // Ensure server certificate is validated
            CertificateValidation.EnsureCertificateValidation();

            // Create auth info. For this sample, we will be passing auth info into all of the PublicAPI web service calls
            // instead of using IAuth.LogonWithPassword() (which is the alternative method).
            this.authenticationInfo = new AuthenticationInfo()
            {
                UserKey = user,
                Password = password
            };

            this.user = user;
            this.password = password;

            // Set default DateTime format
            this.dateTimeFormat = ConfigurationManager.AppSettings["dateTimeFormat"] ?? "dd-MMM-yyyy hh:mm tt";
        }

        public RecorderSettings GetSettingsByRecorderName(string name)
        {
            try
            {
                this.EnsureRecorders();
                if (!this.recorders.TryGetValue(name, out RemoteRecorder recorder))
                {
                    return null;
                }

                return new RecorderSettings { RecorderId = recorder.Id };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Guid[] GetSessionsByRecorderName(string name)
        {
            this.EnsureRecorders();
            if (!this.recorders.TryGetValue(name, out RemoteRecorder recorder))
            { 
                return null;
            }

            List<Guid> sessions = new List<Guid>();

            foreach (var recording in recorder.ScheduledRecordings)
            {
                sessions.Add(recording);
            }

            return sessions.ToArray();
        }

        private void EnsureRecorders()
        {
            lock(this.lockRecorders)
            {
                if (this.recorders == null)
                {
                    this.recorders = new Dictionary<string, RemoteRecorder>();

                    int retryCount = 0;
                    for (int pageNumber = 0; ; pageNumber++)
                    {
                        try
                        {
                            var response = this.remoteRecorderManager.ListRecorders(
                                this.authenticationInfo,
                                new Pagination
                                {
                                    MaxNumberResults = 30,
                                    PageNumber = pageNumber
                                },
                                RecorderSortField.Name
                            );

                            if (response.PagedResults.Count > 0)
                            {
                                foreach (RemoteRecorder recorder in response.PagedResults)
                                {
                                    this.recorders[recorder.Name] = recorder;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch (ProtocolException e)
                        {
                            Trace.WriteLine($"ListRecorders threw: {e}");
                            retryCount++;
                            if (retryCount > 10)
                            {
                                break;
                            }

                            Trace.WriteLine($"Retrying {retryCount} times.");
                            pageNumber--;
                        }
                    }
                }
            }
        }

        public SchedulingResult ScheduleRecording(string name, Guid folderId, bool isBroadcast, DateTime startTime, DateTime endTime, List<RecorderSettings> settings, bool overwrite)
        {
            if (!folderId.Equals(Guid.Empty))
            {
                ScheduledRecordingResult result = null;
                string conflictingSessions = "";
                bool retry = true;
                int attemptCount = 0;
                while (retry)
                {
                    try
                    {
                        result = this.remoteRecorderManager.ScheduleRecording(this.authenticationInfo, name, folderId, isBroadcast, startTime.ToUniversalTime(), endTime.ToUniversalTime(), settings);
                        retry = false;

                        if (result.ConflictsExist)
                        {

                            if (overwrite == true)
                            {
                                using (var sessionManager = new SessionManagementWrapper(user, password))
                                {
                                    foreach (ScheduledRecordingInfo session in result.ConflictingSessions)
                                    {
                                        bool sessiondeleted = sessionManager.DeleteSessions(new Guid[] { session.SessionID });
                                        if (sessiondeleted == false)
                                        {
                                            return new SchedulingResult(false, string.Format("Unable to schedule recording {0} between {1} and {2} due to schedule conflicts. Unable to delete session {3}",
                                            name, startTime.ToString(this.dateTimeFormat), endTime.ToString(this.dateTimeFormat), session.SessionID), Guid.Empty);
                                        }
                                        else
                                        {
                                            conflictingSessions += string.Format("{0} ", session.SessionID );                                
                                        }
                                    }
                                  
                                }
                                result = this.remoteRecorderManager.ScheduleRecording(this.authenticationInfo, name, folderId, isBroadcast, startTime.ToUniversalTime(), endTime.ToUniversalTime(), settings);
                                retry = false;
                                return new SchedulingResult(true, string.Format("Recording {0} was scheduled between {1} and {2} overwriting previously scheduled sessions",
                                name, startTime.ToString(this.dateTimeFormat), endTime.ToString(this.dateTimeFormat)), result.SessionIDs[0], conflictingSessions);
                                

                            }
                            else
                            {
                                return new SchedulingResult(false, string.Format("Unable to schedule recording {0} between {1} and {2} due to schedule conflicts.",
                                 name, startTime.ToString(this.dateTimeFormat), endTime.ToString(this.dateTimeFormat)), Guid.Empty, result.ConflictingSessions[0].SessionID.ToString());
                            }

                        }
                       

                    }
                    catch (FaultException e)
                    {
                        // only retry the call if it's possible this was a transient error (ie, a deadlock)
                        retry &= (e.Message == "An error occurred. See server logs for details") && (attemptCount < 3);
                        if (retry)
                        {
                            // sleep for a bit before retrying
                            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(3));
                        }
                        else
                        {
                            // if we're not retrying, re-throw the exception
                            throw;
                        }
                    }
                    attemptCount++;

                }

                return new SchedulingResult(true,string.Format("Recording {0} was scheduled between {1} and {2}",
                        name, startTime.ToString(this.dateTimeFormat), endTime.ToString(this.dateTimeFormat)), result.SessionIDs[0]);

            }
            else
            {
                return new SchedulingResult(false, string.Format("Recording {0} failed; folder not found.", name), Guid.Empty);
            }

        }

        public void Dispose()
        {
            if (this.remoteRecorderManager.State == CommunicationState.Faulted)
            {
                this.remoteRecorderManager.Abort();
            }

            if (this.remoteRecorderManager.State != CommunicationState.Closed)
            {
                this.remoteRecorderManager.Close();
            }
        }

        public LoginResults getListRecordersForLoginVerification()
        {
            try
            {
                var pagination = new Pagination { MaxNumberResults = 1, PageNumber = 0 };
                var recorderListResponse = this.remoteRecorderManager.ListRecorders(this.authenticationInfo, pagination, RecorderSortField.Name);

                // User has no Remote Recorder Access
                if (recorderListResponse.TotalResultCount < 1)
                    return LoginResults.NoAccess;
            }
            catch
            {
                return LoginResults.Failed;
            }
            return LoginResults.Succeeded;
        }
    }
}
