using System;
using System.Net;
using System.Threading;

namespace Logging3DproxyApp
{
    public abstract class HttpRequestHandler
    {
        private int m_RequestCounter;
        private readonly ManualResetEvent m_StopEvent = new ManualResetEvent(false);
        private readonly HttpListener m_Listener;

        protected HttpRequestHandler(string[] prefixes)
        {
            m_Listener = new HttpListener();
            Array.ForEach(prefixes, m_Listener.Prefixes.Add);
        }

        public void Start()
        {
            m_Listener.Start();
            var state = new HttpListenerCallbackState(m_Listener);
            ThreadPool.QueueUserWorkItem(Listen, state);
        }

        public void Stop()
        {
            m_StopEvent.Set();
        }

        private void Listen(object state)
        {
            var callbackState = (HttpListenerCallbackState) state;

            while (callbackState.Listener.IsListening)
            {
                callbackState.Listener.BeginGetContext(ListenerCallback, callbackState);
                var n = WaitHandle.WaitAny(new WaitHandle[] {callbackState.ListenForNextRequest, m_StopEvent});

                if (n == 1)
                {
                    // stopEvent was signalled 
                    callbackState.Listener.Stop();
                    break;
                }
            }
        }

        private void ListenerCallback(IAsyncResult ar)
        {
            var callbackState = (HttpListenerCallbackState) ar.AsyncState;
            HttpListenerContext context;

            Interlocked.Increment(ref m_RequestCounter);

            try
            {
                context = callbackState.Listener.EndGetContext(ar);
            }
            catch (Exception)
            {
                return;
            }
            finally
            {
                callbackState.ListenForNextRequest.Set();
            }

            DoHandleRequest(context);
        }

        protected abstract void DoHandleRequest(HttpListenerContext context);
    }
}