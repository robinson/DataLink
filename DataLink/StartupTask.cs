using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.System.Threading;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace DataLink
{
    public sealed class StartupTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {

            taskInstance.GetDeferral();
            DataLinkApplication dlApp = new DataLinkApplication();
            dlApp.LoadSetting();
            dlApp.Initial();
            ThreadPool.RunAsync(workItem =>
            {
                dlApp.Start();
            });
            while (true) ;

            //DataLogging dataLogging = new DataLogging();
            //dataLogging.LoadSetting();
            //dataLogging.Initial();
            //ThreadPool.RunAsync(workItem =>
            //{
            //dataLogging.Start();
            //});
            //WebServer server = new WebServer();
            //ThreadPool.RunAsync(workItem =>
            //{
            //    server.Start();
            //});
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //
        }
    }
}
