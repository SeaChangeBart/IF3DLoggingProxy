using System;
using System.Net;
using System.Threading;

namespace Logging3DproxyApp
{
    public class HttpListenerCallbackState
    {
        private readonly HttpListener m_Listener;
        private readonly AutoResetEvent m_ListenForNextRequest;

        public HttpListenerCallbackState(HttpListener listener)
        {
            if (listener == null) throw new ArgumentNullException("listener");
            m_Listener = listener;
            m_ListenForNextRequest = new AutoResetEvent(false);
        }

        public HttpListener Listener
        {
            get { return m_Listener; }
        }

        public AutoResetEvent ListenForNextRequest
        {
            get { return m_ListenForNextRequest; }
        }
    }
}