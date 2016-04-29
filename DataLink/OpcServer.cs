using Opc.Ua;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink
{
    public sealed class OpcServer
    {
        public void Start()
        {
            OnLaunch();
        }
        async void OnLaunch()
        {
            ApplicationInstance application = new ApplicationInstance();
            application.ApplicationName = "UA Sample Server";
            application.ApplicationType = ApplicationType.Server;
            application.ConfigSectionName = "Opc.Ua.SampleServer";

            try
            {
                // load the application configuration.
                await application.LoadApplicationConfiguration(false);

                // This call registers the certificate with HTTP.SYS 
                // It must be called once after installation if the HTTPS endpoint is enabled.
                // HttpAccessRule.SetHttpsCertificate(c.SecurityConfiguration.ApplicationCertificate.Find(true), 51212, false);

                // check the application certificate.
                await application.CheckApplicationInstanceCertificate(false, 0);
                // start the server.
                await application.Start(new Opc.Ua.Sample.SampleServer());
            }
            catch (Exception ex)
            {
                Utils.Trace("ServiceResultException:" + ex.Message);
            }
        }
    }
}
