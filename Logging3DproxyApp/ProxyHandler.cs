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
        private readonly string m_EndPoint;
        private readonly string m_LogPath;
        private readonly string m_SharedLogFileTemplate;

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
            m_SharedLogFileTemplate = Path.Combine(m_LogPath, resource + "_{0:yyyyMMdd}.log");
            TimeoutInSeconds = 10;
            StatusCodeOnGetTimeout = 503;
            StatusCodeOnPutPostTimeout = 503;
            StatusCodeOnDeleteTimeout = 503;
            m_MigrationMode = true;
            m_MigrationThread = new Thread(() =>
            {
                var perContentLogFilesInLogDirDirectly = Directory.EnumerateFiles(m_LogPath, "*.log")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(fn => !fn.StartsWith(resource));

                while (perContentLogFilesInLogDirDirectly.Any())
                {
                    Console.WriteLine("Migration Run Starting");
                    foreach (var contentId in perContentLogFilesInLogDirDirectly)
                        TryMigrate(contentId);
                    Console.WriteLine("Migration Run Completed");
                }
                Console.WriteLine("Migration Completed or Unnecessary");
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
                Console.Error.WriteLine("Migration Error for {0}: {1}", contentId, e.Message);
            }
        }

        public int TimeoutInSeconds { get; set; }

        protected override void DoHandleRequest(HttpListenerContext context)
        {
            var startTime = DateTime.UtcNow;
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

                    using (webResponse)
                    {
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

                        Log(startTime, contentId, httpListenerRequest.HttpMethod, context.Request.Url, requestBody,
                            context.Response.StatusCode, responseBody, (int) stopWatch.ElapsedMilliseconds);

                        webResponse.Close();
                    }
                }
                catch (Exception e)
                {
                    stopWatch.Stop();
                    Log(startTime, contentId, httpListenerRequest.HttpMethod, context.Request.Url, requestBody,
                        "Exception", e.Message, (int)stopWatch.ElapsedMilliseconds);
                    context.Response.StatusCode = StatusCodeOnTimeout(webRequest.Method);
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

        private void Log(DateTime time, string contentId, string method, Uri url, MemoryStream requestBody, int statusCode, MemoryStream responseBody, int responseTimeMs)
        {
            var responseBodyString = TsvCompatible(Encoding.UTF8.GetString(responseBody.ToArray()));
            var statusCodeString = string.Format("HTTP {0}", statusCode);
            Log(time, contentId, method, url, requestBody, statusCodeString, responseBodyString, responseTimeMs);
        }

        private void Log(DateTime time, string contentId, string method, Uri url, MemoryStream requestBody, string statusCodeString, string responseBodyString, int responseTimeMs)
        {
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
        private readonly Action<string> m_Trace;
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
                var timedLogLine = string.Format("{0}\t{1}\r\n", timeString, logLine);
                var perContentLogFile = PerContentLogFile(contentId);
                if (m_MigrationMode)
                    TryMigrate(contentId, perContentLogFile);
                var perDayLogFile = string.Format(m_SharedLogFileTemplate, time);
                TryRetry(() => File.AppendAllText(perContentLogFile, timedLogLine), 3);
                TryRetry(() => File.AppendAllText(perDayLogFile, timedLogLine), 3);
                m_Trace(logLine);
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

                }
                catch (Exception e)
                {
                    Console.WriteLine("Couldn't move old log to new log {1}: {0}", e.Message, perContentLogFile);
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