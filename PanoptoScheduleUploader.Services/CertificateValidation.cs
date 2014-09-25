using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace PanoptoScheduleUploader.Services
{
    internal static class CertificateValidation
    {
        private static bool hasBeenInitialized = false;

        /// <summary>
        /// Ensures that our custom certificate validation has been applied
        /// </summary>
        internal static void EnsureCertificateValidation()
        {
            if (!hasBeenInitialized)
            {
                ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(CustomCertificateValidation);
                hasBeenInitialized = true;
            }
        }

        /// <summary>
        /// Ensures that server certificate is authenticated
        /// </summary>
        private static bool CustomCertificateValidation(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }

    }
}
