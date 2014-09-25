using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PanoptoScheduleUploader.Services.UserManagement;
using System.ServiceModel;

namespace PanoptoScheduleUploader.Services
{
    public class UserManagementWrapper : IDisposable
    {
        public UserManagementClient userManager;
        AuthenticationInfo authentication;

        public UserManagementWrapper(string username, string password)
        {
            this.userManager = new UserManagementClient();
            CertificateValidation.EnsureCertificateValidation();

            this.authentication = new AuthenticationInfo()
            {
                UserKey = username,
                Password = password
            };
        }

        public User GetUserByUserName(string username)
        {
            return this.userManager.GetUserByKey(authentication, username);
        }

        public void Dispose()
        {
            if (this.userManager.State == CommunicationState.Faulted)
            {
                this.userManager.Abort();
            }

            if (this.userManager.State != CommunicationState.Closed)
            {
                this.userManager.Close();
            }
        }
    }
}
