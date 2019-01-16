using QNetwork.Http.Server;
using QNetwork.Http.Server.Cache;
using QNetwork.Http.Server.Log;
using QNetwork.Http.Server.Protocol;
using QNetwork.Http.Server.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace WPF_Server_Http.Service
{
    public class CQHttpService_WebMediaPlayer : IQHttpService
    {
        public IQHttpServer_Log Logger { set; get; }
        public IQHttpServer_Extension Extension { set; get; }

        //public List<string> Methods => this.m_Methods;
        //List<string> m_Methods = new List<string>();
        public CQHttpService_WebMediaPlayer()
        {
            //this.m_Methods.Add("/MediaTest");
            //this.m_Methods.Add("/GetMediaFile");
        }


        public bool CloseHandler(List<string> handlers)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        [CQServiceMethod("/MediaTest")]
        public bool MediaTest(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
            process_result_code = ServiceProcessResults.None;
            bool result = true;

            byte[] bb = File.ReadAllBytes("../WebTest/WebMediaPlayer.html");
            resp.ContentLength = bb.LongLength;
            resp.Content = new MemoryStream(bb);
            resp.ContentType = "text/html; charset=utf-8";
            process_result_code = ServiceProcessResults.OK;

            return result;
        }

        [CQServiceMethod("/GetMediaFile")]
        public bool GetMediaFile(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
            process_result_code = ServiceProcessResults.None;
            bool result = true;

            CQMediaFiles files = new CQMediaFiles();
            CQMediaFile mf = new CQMediaFile();
            mf.FullName = "D:\\123.mp4";
            mf.ID = "1";
            mf.Name = "1234.mp4";
            files.Files.Add(mf);
            JavaScriptSerializer ss = new JavaScriptSerializer();
            string sre = ss.Serialize(files.Files);
            byte[] bb = Encoding.UTF8.GetBytes(sre);
            resp.ContentLength = bb.LongLength;
            resp.Content = new MemoryStream(bb);
            resp.ContentType = "application/json; charset=utf-8";
            process_result_code = ServiceProcessResults.OK;
            this.Extension.CacheControl<CQMediaFiles>(CacheOperates.Create, "QQ", ref files);

            return result;
        }

        //public bool Process(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        //{
        //    resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
        //    process_result_code = ServiceProcessResults.None;
        //    bool result = true;
        //    switch (req.URL.LocalPath)
        //    {
        //        case "/MediaTest":
        //            {
        //                //WebMediaPlayer.html

        //                byte[] bb = File.ReadAllBytes("../WebTest/WebMediaPlayer.html");
        //                resp.ContentLength = bb.LongLength;
        //                resp.Content = new MemoryStream(bb);
        //                resp.ContentType = "text/html; charset=utf-8";
        //                process_result_code = ServiceProcessResults.OK;
        //            }
        //            break;
        //        case "/GetMediaFile":
        //            {
        //                CQMediaFiles files = new CQMediaFiles();
        //                CQMediaFile mf = new CQMediaFile();
        //                mf.FullName = "D:\\123.mp4";
        //                mf.ID = "1";
        //                mf.Name = "1234.mp4";
        //                files.Files.Add(mf);
        //                JavaScriptSerializer ss = new JavaScriptSerializer();
        //                string sre = ss.Serialize(files.Files);
        //                byte[] bb = Encoding.UTF8.GetBytes(sre);
        //                resp.ContentLength = bb.LongLength;
        //                resp.Content = new MemoryStream(bb);
        //                resp.ContentType = "application/json; charset=utf-8";
        //                process_result_code = ServiceProcessResults.OK;
        //                this.Extension.CacheControl<CQMediaFiles> (CacheOperates.Create, "QQ", ref files);
        //            }
        //            break;


        //    }
        //    return result;
        //}

        public bool RegisterCacheManager()
        {
            return true;
        }
    }

    public class CQMediaFile
    {
        public string ID { set; get; }
        public string Name { set; get; }
        [ScriptIgnore]
        public string FullName { set; get; }
    }

    public class CQMediaFiles: CQCacheBase
    {
        public List<CQMediaFile> Files { set; get; }
        public CQMediaFiles()
        {
            this.Files = new List<CQMediaFile>();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
