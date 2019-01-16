using QNetwork.Http.Server;
using QNetwork.Http.Server.Cache;
using QNetwork.Http.Server.Log;
using QNetwork.Http.Server.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace QNetwork.Http.Server.Service
{
    [CQServiceRoot()]
    public class CQHttpDefaultService : IQHttpService
    {
        List<string> m_Icons = new List<string>();
        public CQHttpDefaultService()
        {
            string str = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            string[] files = Directory.GetFiles(str, "*.ico");
            this.m_Icons.AddRange(files);
        }

        void CoppyStream(System.IO.Stream src, System.IO.Stream dst)
        {
            byte[] buf = new byte[8192];
            while (src.Position < src.Length)
            {
                int read_len = src.Read(buf, 0, buf.Length);
                dst.Write(buf, 0, read_len);
            }
        }

        public IQHttpServer_Extension Extension { set; get; }
        public IQHttpServer_Log Logger { set; get; }

        [CQServiceMethod("/favicon.ico")]
        public bool favicon(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            process_result_code = ServiceProcessResults.OK;
            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
            if (this.m_Icons.Count() > 0)
            {
                resp.Content = new FileStream(this.m_Icons[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                resp.Content.Position = 0;
                resp.ContentLength = resp.Content.Length;
                resp.ContentType = "image/x-icon";
                resp.Connection = Connections.Close;
            }
            else
            {
                resp.Content = null;
                resp.ContentLength = 0;
                resp.ContentType = "";
                resp.Connection = Connections.Close;
            }

            resp.Set200();
            return true;
        }

        [CQServiceMethod("/PostTest")]
        public bool PostTest(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            process_result_code = ServiceProcessResults.OK;
            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);

            resp.Content = new MemoryStream();
            req.Content.Position = 0;
            this.CoppyStream(req.Content, resp.Content);
            resp.Content.Position = 0;
            resp.ContentLength = req.Content.Length;
            resp.ContentType = req.Headers["CONTENT-TYPE"];
            resp.Connection = Connections.Close;

            resp.Set200();

            return true;
        }

        //[CQServiceMethod("/Test")]
        //public bool Test(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        //{
        //    process_result_code = ServiceProcessResults.OK;
        //    resp = new CQHttpResponse(req.HandlerID, req.ProcessID);

        //    resp.Content = new FileStream("test.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        //    resp.Content.Position = 0;
        //    resp.ContentLength = resp.Content.Length;
        //    resp.ContentType = "text/plain";
        //    resp.Connection = Connections.Close;

        //    resp.Set200();
        //    return true;
        //}

        [CQServiceMethod("/")]
        public bool Default(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            process_result_code = ServiceProcessResults.OK;
            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);

            string content_str = string.Format("Hello world\r\n{0}\r\nQEmbedded Http server\r\n", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo));
            //resp.Content = new MemoryStream(Encoding.UTF8.GetBytes(content_str));
            //resp.ContentLength = resp.Content.Length;
            //resp.ContentType = "text/plain";
            resp.Connection = Connections.KeepAlive;
            resp.SetContent(content_str);
            resp.Set200();
            return true;
        }

        public bool CloseHandler(List<string> handlers)
        {
            return true;
        }

        public void Dispose()
        {
        }

        public bool RegisterCacheManager()
        {
            bool result = true;


            return result;
        }
    }
}
