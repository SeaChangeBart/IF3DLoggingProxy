using System.ServiceProcess;

namespace Logging3DproxyApp
{
    partial class ProxyService : ServiceBase
    {
        private App m_App;

        public ProxyService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            m_App = new App(_ => { });
            m_App.Start();
        }

        protected override void OnStop()
        {
            m_App.Stop();
        }
    }
}
