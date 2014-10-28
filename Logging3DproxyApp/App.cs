using System;
using System.IO;
using System.Reflection;
using log4net.Config;

namespace Logging3DproxyApp
{
    internal class App
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger
            (MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ProxyHandler m_Handler1;
        private readonly ProxyHandler m_Handler2;
        public App()
        {
            m_Handler1 = new ProxyHandler(Properties.Settings.Default.ListenPort, Properties.Settings.Default.Resource1,
                                          Properties.Settings.Default.EndPoint1,
                                          Path.Combine(Properties.Settings.Default.LogPath,
                                                       Properties.Settings.Default.Resource1));
            m_Handler2 = new ProxyHandler(Properties.Settings.Default.ListenPort, Properties.Settings.Default.Resource2,
                                          Properties.Settings.Default.EndPoint2,
                                          Path.Combine(Properties.Settings.Default.LogPath,
                                                       Properties.Settings.Default.Resource2));

            if (Properties.Settings.Default.TimeoutInSeconds > 0)
            {
                m_Handler1.TimeoutInSeconds = Properties.Settings.Default.TimeoutInSeconds;
                m_Handler2.TimeoutInSeconds = Properties.Settings.Default.TimeoutInSeconds;
                Log.InfoFormat("Timeout set to {0} sec", Properties.Settings.Default.TimeoutInSeconds);
            }

            if (Properties.Settings.Default.StatusCodeOnTimeout > 0)
            {
                m_Handler1.StatusCodeOnGetTimeout = Properties.Settings.Default.StatusCodeOnTimeout;
                m_Handler2.StatusCodeOnGetTimeout = Properties.Settings.Default.StatusCodeOnTimeout;
                m_Handler1.StatusCodeOnPutPostTimeout = Properties.Settings.Default.StatusCodeOnTimeout;
                m_Handler2.StatusCodeOnPutPostTimeout = Properties.Settings.Default.StatusCodeOnTimeout;
                m_Handler1.StatusCodeOnDeleteTimeout = Properties.Settings.Default.StatusCodeOnTimeout;
                m_Handler2.StatusCodeOnDeleteTimeout = Properties.Settings.Default.StatusCodeOnTimeout;
                Log.InfoFormat("StatusCodeOnTimeout set to {0}", Properties.Settings.Default.StatusCodeOnTimeout);
            }

            if (Properties.Settings.Default.StatusCodeOnPutPostTimeout > 0)
            {
                m_Handler1.StatusCodeOnPutPostTimeout = Properties.Settings.Default.StatusCodeOnPutPostTimeout;
                m_Handler2.StatusCodeOnPutPostTimeout = Properties.Settings.Default.StatusCodeOnPutPostTimeout;
                Log.InfoFormat("StatusCodeOnPutPostTimeout set to {0}", Properties.Settings.Default.StatusCodeOnPutPostTimeout);
            }

            if (Properties.Settings.Default.StatusCodeOnDeleteTimeout > 0)
            {
                m_Handler1.StatusCodeOnDeleteTimeout = Properties.Settings.Default.StatusCodeOnDeleteTimeout;
                m_Handler2.StatusCodeOnDeleteTimeout = Properties.Settings.Default.StatusCodeOnDeleteTimeout;
                Log.InfoFormat("StatusCodeOnDeleteTimeout set to {0}", Properties.Settings.Default.StatusCodeOnDeleteTimeout);
            }
        }
        public void Start()
        {
            try
            {
                Log.Info("Starting handler 1");
                m_Handler1.Start();
                Log.Info("Started handler 1");
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Failed to start handler 1: {0}", e.Message);
            }

            try
            {
                Log.Info("Starting handler 2");
                m_Handler2.Start();
                Log.Info("Started handler 2");
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Failed to start handler 2: {0}", e.Message);
            }
        }
        public void Stop()
        {
            try
            {
                Log.Info("Stopping handler 1");
                m_Handler1.Stop();
                Log.Info("Stopped handler 1");
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Failed to Stop handler 1: {0}", e.Message);
            }

            try
            {
                Log.Info("Stopping handler 2");
                m_Handler2.Stop();
                Log.Info("Stopped handler 2");
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Failed to Stop handler 2: {0}", e.Message);
            }
        }
    }
}
