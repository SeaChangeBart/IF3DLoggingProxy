using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Logging3DproxyApp
{
    public class ProxyHandler : HttpRequestHandler
    {
        private readonly log4net.ILog _logger;
        private readonly string m_EndPoint;
        private readonly string m_LogPath;

        private static string[] Prefixes(short port, string resource)
        {
            var prefix = string.Format("http://+:{0}/{1}/", port, resource);
            return new[] {prefix};
        }

        private void Debug(string msg)
        {
            _logger.Debug(msg);
        }

        private void Debug(string msg, params object[] prm)
        {
            _logger.DebugFormat(msg, prm);
        }

        public ProxyHandler(short port, string resource, string endPoint, string logPath)
            : base(Prefixes(port, resource))
        {
            _logger = log4net.LogManager.GetLogger("Handler." + resource);
            m_EndPoint = endPoint;
            m_LogPath = logPath;
            if (!Directory.Exists(m_LogPath))
                Directory.CreateDirectory(m_LogPath);
            TimeoutInSeconds = 10;
            StatusCodeOnGetTimeout = 503;
            StatusCodeOnPutPostTimeout = 503;
            StatusCodeOnDeleteTimeout = 503;
            m_MigrationMode = true;
            m_MigrationThread = new Thread(() =>
            {
                var perContentLogFilesInLogDirDirectly = Directory.EnumerateFiles(m_LogPath, "*.log")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(id => id.Length > 8)
                    .Where(fn => !fn.StartsWith(resource));

                while (perContentLogFilesInLogDirDirectly.Any())
                {
                    Debug("Migration Run Starting");
                    foreach (var contentId in perContentLogFilesInLogDirDirectly)
                        TryMigrate(contentId);
                    Debug("Migration Run Completed");
                }
                Debug("Migration Completed or Unnecessary");
                m_MigrationMode = false;
            }) {Priority = ThreadPriority.BelowNormal};
            m_MigrationThread.Start();
        }

        private void TryMigrate(string contentId)
        {
            try
            {
                var newPath = PerContentLogFile(contentId);
                TryMigrate(contentId, newPath);
            }
            catch (Exception e)
            {
                Debug("Migration Error for {0}: {1}", contentId, e.Message);
            }
        }

        public int TimeoutInSeconds { get; set; }

        class MyWebClient : WebClient
        {
            private readonly int _timeoutInMs;

            public MyWebClient(int timeoutInMs)
            {
                _timeoutInMs = timeoutInMs;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                if (request == null)
                    return null;

                request.Timeout = _timeoutInMs;
                return request;
            }
        }

        protected override void DoHandleRequest(HttpListenerContext context)
        {
            var startTime = DateTime.UtcNow;
            var httpListenerRequest = context.Request;
            var contentId = httpListenerRequest.Url.Segments.Last();
            if (contentId.Equals("Status", StringComparison.InvariantCultureIgnoreCase))
                contentId = httpListenerRequest.Url.Segments.Reverse().Skip(1).First().Replace("/", "");

            try
            {
                Debug("Received {0} {1}", httpListenerRequest.HttpMethod, httpListenerRequest.Url.PathAndQuery);
                var actualResource = string.Concat(httpListenerRequest.Url.Segments.Skip(2));
                var urlToCall = m_EndPoint + actualResource;

                using (var webClient = new MyWebClient(TimeoutInSeconds*1000))
                {
                    //webClient.Headers.Add(HttpRequestHeader.ContentType, httpListenerRequest.ContentType);
                    webClient.Headers.Add(httpListenerRequest.Headers);

                    MemoryStream requestBody = null;
                    if (httpListenerRequest.HasEntityBody)
                    {
                        requestBody = new MemoryStream();
                        httpListenerRequest.InputStream.CopyTo(requestBody);
                        requestBody.Seek(0, SeekOrigin.Begin);
                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    try
                    {
                        var responseData = new byte[0];
                        try
                        {
                            Debug("Calling {0} {1}", httpListenerRequest.HttpMethod, urlToCall);
                            responseData = requestBody != null
                                ? webClient.UploadData(urlToCall, httpListenerRequest.HttpMethod, requestBody.ToArray())
                                : webClient.DownloadData(urlToCall);
                            stopWatch.Stop();
                        }
                        catch (WebException webEx)
                        {
                            var webResponse = (HttpWebResponse) webEx.Response;
                            if (webResponse == null)
                                throw;
                            context.Response.StatusCode = (int)webResponse.StatusCode;
                        }
                        context.Response.ContentLength64 = responseData.Length;
                        if (webClient.ResponseHeaders != null)
                        {
                            if (
                                webClient.ResponseHeaders.AllKeys.Any(
                                    k => k.Equals("Content-Type", StringComparison.InvariantCultureIgnoreCase)))
                            {
                                context.Response.ContentType =
                                    webClient.ResponseHeaders[HttpResponseHeader.ContentType];
                            }
                        }

                        context.Response.OutputStream.Write(responseData, 0, responseData.Length);

                        Log(startTime, contentId, httpListenerRequest.HttpMethod, context.Request.Url, requestBody,
                            context.Response.StatusCode, responseData, (int)stopWatch.ElapsedMilliseconds);
                    }
                    catch (Exception e)
                    {
                        stopWatch.Stop();
                        Log(startTime, contentId, httpListenerRequest.HttpMethod, context.Request.Url, requestBody,
                            "Exception", e.Message, (int) stopWatch.ElapsedMilliseconds);
                        context.Response.StatusCode = StatusCodeOnTimeout(httpListenerRequest.HttpMethod);
                    }
                }
                context.Response.OutputStream.Close();
                context.Response.Close();
            }
            catch (Exception e)
            {
                Log(startTime, contentId, "Exception handling request: " + e.Message);
                context.Response.StatusCode = 500;
                context.Response.OutputStream.Close();
                context.Response.Close();
            }
        }

        private int StatusCodeOnTimeout(string method)
        {
            switch (method)
            {
                case "DELETE":
                    return StatusCodeOnDeleteTimeout;
                case "POST":
                case "PUT":
                    return StatusCodeOnPutPostTimeout;
                default:
                    return StatusCodeOnGetTimeout;
            }
        }

        private void Log(DateTime time, string contentId, string method, Uri url, MemoryStream requestBody, int statusCode, byte[] responseBody, int responseTimeMs)
        {
            var responseBodyString = TsvCompatible(Encoding.UTF8.GetString(responseBody));
            var statusCodeString = string.Format("HTTP {0}", statusCode);
            Log(time, contentId, method, url, requestBody, statusCodeString, responseBodyString, responseTimeMs);
        }

        private void Log(DateTime time, string contentId, string method, Uri url, MemoryStream requestBody, string statusCodeString, string responseBodyString, int responseTimeMs)
        {
            if (requestBody == null)
                requestBody = new MemoryStream();
            var requestBodyString = TsvCompatible(Encoding.UTF8.GetString(requestBody.ToArray()));
            var requestString = url.PathAndQuery;

            if (responseBodyString.StartsWith("<html>"))
                responseBodyString = "<html>...";

            var logLine = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}ms", contentId, method, requestString,
                                        requestBodyString,
                                        statusCodeString, responseBodyString, responseTimeMs);

            Log(time, contentId, logLine);
        }

        static readonly char[] OngewensteFiguren = { '\r', '\t', '\n' };

        private bool m_MigrationMode;
        private Thread m_MigrationThread;
        public int StatusCodeOnGetTimeout { get; set; }
        public int StatusCodeOnPutPostTimeout { get; set; }
        public int StatusCodeOnDeleteTimeout { get; set; }

        private static string TsvCompatible(string rawString)
        {
            var retVal = rawString;
            if (string.IsNullOrWhiteSpace(retVal))
                return "n/a";
            foreach (var ongewenstFiguur in OngewensteFiguren)
                retVal = retVal.Replace(ongewenstFiguur, ' ');
            return retVal;
        }

        private void Log(DateTime time, string contentId, string logLine)
        {
            lock (this)
            {
                var timeString = time.ToString("o");
                var timedLogMessage = string.Format("{0}\t{1}", timeString, logLine);
                var perContentLogFile = PerContentLogFile(contentId);
                if (m_MigrationMode)
                    TryMigrate(contentId, perContentLogFile);
                _logger.Info(timedLogMessage);
                TryRetry(() => File.AppendAllLines(perContentLogFile, new []{timedLogMessage}), 3);
            }
        }

        private void TryMigrate(string contentId, string perContentLogFile)
        {
            lock (this)
                try
                {
                    if (File.Exists(perContentLogFile))
                        return;
                    var oldFile = OldPerContentLogFile(contentId);
                    if (!File.Exists(oldFile))
                        return;
                    File.Move(oldFile, perContentLogFile);
                    Debug("Migrated " + oldFile);

                }
                catch (Exception e)
                {
                    _logger.ErrorFormat("Couldn't move old log to new log {1}: {0}", e.Message, perContentLogFile);
                }
        }

        private string OldPerContentLogFile(string contentId)
        {
            return Path.Combine(m_LogPath, contentId + ".log");
        }

        private string PerContentLogFile(string contentId)
        {
            var dir = PerContentLogDir(contentId);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return Path.Combine(dir, contentId + ".log");
        }

        private string PerContentLogDir(string contentId)
        {
            return contentId.Length > 8
                ? Path.Combine(m_LogPath, contentId.Substring(0, 2), contentId.Substring(0, 4))
                : m_LogPath;
        }

        private bool TryRetry(Action what, int howOften)
        {
            for (var i = 0; i <= howOften; i++)
            {
                if (Try(what))
                    return true;
                System.Threading.Thread.Sleep(25);
            }
            return false;
        }

        private bool Try(Action what)
        {
            try
            {
                what();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}