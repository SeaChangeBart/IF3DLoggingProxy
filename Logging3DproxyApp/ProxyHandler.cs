using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Logging3DproxyApp
{
    public class ProxyHandler : HttpRequestHandler
    {
        private readonly string m_EndPoint;
        private readonly string m_LogPath;
        private readonly string m_SharedLogFile;

        private static string[] Prefixes(short port, string resource)
        {
            var prefix = string.Format("http://+:{0}/{1}/", port, resource);
            return new[] {prefix};
        }

        public ProxyHandler(short port, string resource, string endPoint, string logPath, Action<string> trace)
            : base(Prefixes(port, resource))
        {
            m_EndPoint = endPoint;
            m_LogPath = logPath;
            m_Trace = trace;
            if (!Directory.Exists(m_LogPath))
                Directory.CreateDirectory(m_LogPath);
            m_SharedLogFile = Path.Combine(m_LogPath, resource + ".log");
            TimeoutInSeconds = 10;
            StatusCodeOnTimeout = 503;
        }

        public int TimeoutInSeconds { get; set; }

        protected override void DoHandleRequest(HttpListenerContext context)
        {
            var httpListenerRequest = context.Request;
            var contentId = httpListenerRequest.Url.Segments.Last();
            if (contentId.Equals("Status", StringComparison.InvariantCultureIgnoreCase))
                contentId = httpListenerRequest.Url.Segments.Reverse().Skip(1).First().Replace("/", "");

            try
            {
                m_Trace(httpListenerRequest.Url.PathAndQuery);
                var actualResource = string.Concat(httpListenerRequest.Url.Segments.Skip(2));
                var urlToCall = m_EndPoint + actualResource;

                var webRequest = WebRequest.Create(urlToCall);
                webRequest.Timeout = TimeoutInSeconds * 1000;
                webRequest.Method = httpListenerRequest.HttpMethod;

                webRequest.ContentLength = httpListenerRequest.ContentLength64;
                webRequest.ContentType = httpListenerRequest.ContentType;

                var requestBody = new MemoryStream();
                if (httpListenerRequest.HasEntityBody)
                {
                    m_Trace("Copying stream");
                    httpListenerRequest.InputStream.CopyTo(requestBody);
                    requestBody.Seek(0, SeekOrigin.Begin);
                    m_Trace("Copying stream 2");
                    requestBody.CopyTo(webRequest.GetRequestStream());
                }

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                try
                {
                    HttpWebResponse webResponse;
                    try
                    {
                        m_Trace("Calling");
                        webResponse = (HttpWebResponse) webRequest.GetResponse();
                    }
                    catch (WebException webEx)
                    {
                        m_Trace("WebEx");
                        webResponse = (HttpWebResponse) webEx.Response;
                        if (webResponse == null)
                            throw;
                        context.Response.StatusCode = (int) webResponse.StatusCode;
                    }

                    context.Response.ContentLength64 = webResponse.ContentLength;
                    context.Response.ContentType = webResponse.ContentType;

                    var responseBody = new MemoryStream();
                    var responseStream = webResponse.GetResponseStream();
                    if (responseStream != null)
                    {
                        responseStream.CopyTo(responseBody);
                        responseBody.Seek(0, SeekOrigin.Begin);
                        responseBody.CopyTo(context.Response.OutputStream);
                        responseStream.Close();
                    }

                    stopWatch.Stop();

                    Log(contentId, httpListenerRequest.HttpMethod, context.Request.Url, requestBody,
                        context.Response.StatusCode, responseBody, (int)stopWatch.ElapsedMilliseconds);

                    webResponse.Close();
                }
                catch (Exception e)
                {
                    stopWatch.Stop();
                    Log(contentId, httpListenerRequest.HttpMethod, context.Request.Url, requestBody,
                        "Exception", e.Message, (int)stopWatch.ElapsedMilliseconds);
                    context.Response.StatusCode = StatusCodeOnTimeout;
                }
                context.Response.OutputStream.Close();
                context.Response.Close();
            }
            catch (Exception e)
            {
                Log(contentId, "Exception handling request: " + e.Message);
                context.Response.StatusCode = 500;
                context.Response.OutputStream.Close();
                context.Response.Close();
            }
        }

        private void Log(string contentId, string method, Uri url, MemoryStream requestBody, int statusCode, MemoryStream responseBody, int responseTimeMs)
        {
            var responseBodyString = TsvCompatible(Encoding.UTF8.GetString(responseBody.ToArray()));
            var statusCodeString = string.Format("HTTP {0}", statusCode);
            Log(contentId, method, url, requestBody, statusCodeString, responseBodyString, responseTimeMs);
        }

        private void Log(string contentId, string method, Uri url, MemoryStream requestBody, string statusCodeString, string responseBodyString, int responseTimeMs)
        {
            var requestBodyString = TsvCompatible(Encoding.UTF8.GetString(requestBody.ToArray()));
            var requestString = url.PathAndQuery;

            var logLine = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}ms", contentId, method, requestString,
                                        requestBodyString,
                                        statusCodeString, responseBodyString, responseTimeMs);

            Log(contentId, logLine);
        }

        static readonly char[] OngewensteFiguren = { '\r', '\t', '\n' };
        private readonly Action<string> m_Trace;
        public int StatusCodeOnTimeout { get; set; }

        private static string TsvCompatible(string rawString)
        {
            var retVal = rawString;
            if (string.IsNullOrWhiteSpace(retVal))
                return "n/a";
            foreach (var ongewenstFiguur in OngewensteFiguren)
                retVal = retVal.Replace(ongewenstFiguur, ' ');
            return retVal;
        }

        private void Log(string contentId, string logLine)
        {
            lock (this)
            {
                var timeString = DateTime.Now.ToString("o");
                var timedLogLine = string.Format("{0}\t{1}\r\n", timeString, logLine);
                var perContentLogFile = Path.Combine(m_LogPath, contentId + ".log");
                File.AppendAllText(perContentLogFile, timedLogLine);
                File.AppendAllText(m_SharedLogFile, timedLogLine);
                m_Trace(logLine);
            }
        }
    }
}