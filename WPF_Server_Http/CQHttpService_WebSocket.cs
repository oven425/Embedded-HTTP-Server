using QNetwork.Http.Server;
using QNetwork.Http.Server.Cache;
using QNetwork.Http.Server.Handler;
using QNetwork.Http.Server.Log;
using QNetwork.Http.Server.Protocol;
using QNetwork.Http.Server.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WPF_Server_Http.Service
{
    public class CQHttpService_WebSocket : IQHttpService
    {
        public IQHttpServer_Extension Extension { set; get; }
        //List<string> m_Methods = new List<string>();
        //public List<string> Methods => this.m_Methods;

        public IQHttpServer_Log Logger { set; get; }

        public CQHttpService_WebSocket()
        {
            //this.m_Methods.Add("/WEBSOCKET");
            //this.m_Methods.Add("/WEBSOCKET_TEST");
        }
        public bool CloseHandler(List<string> handlers)
        {
            return true;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        [CQServiceMethod("/WEBSOCKET")]
        public bool WEBSOCKET(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            resp = null;
            process_result_code = 0;
            bool result = true;

            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
            resp.Content = new FileStream("../WebTest/Websocket.html", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            resp.ContentLength = resp.Content.Length;
            resp.ContentType = "text/html; charset=utf-8";
            process_result_code = ServiceProcessResults.OK;
            resp.Set200();

            return result;
        }

        [CQServiceMethod("/WEBSOCKET_TEST")]
        public bool WEBSOCKET_TEST(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            resp = null;
            process_result_code = 0;
            bool result = true;

            //[4] = {[Upgrade, websocket]}
            if ((req.Headers.ContainsKey("UPGRADE") == true) && (req.Headers["UPGRADE"] == "websocket"))
            {
                CQWebSocket websocket = new CQWebSocket();
                CQTCPHandler handler;
                this.Extension.ControlTransfer(req.HandlerID, out handler);
                websocket.Open(handler, req.HeaderRaw, req.HeaderRaw.Length);
                this.Extension.CacheControl<CQWebSocket>(CacheOperates.Create, websocket.ID, ref websocket, "websocket");
                process_result_code = ServiceProcessResults.WebSocket;
            }

            return result;
        }


        //public bool Process(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        //{
        //    resp = null;
        //    process_result_code = 0;
        //    bool result = true;
        //    switch (req.URL.LocalPath.ToUpperInvariant())
        //    {
        //        case "/WEBSOCKET":
        //            {
        //                resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
        //                resp.Content = new FileStream("../WebTest/Websocket.html", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        //                resp.ContentLength = resp.Content.Length;
        //                resp.ContentType = "text/html; charset=utf-8";
        //                process_result_code = ServiceProcessResults.OK;
        //                resp.Set200();
        //            }
        //            break;
        //        case "/WEBSOCKET_TEST":
        //            {
        //                //[4] = {[Upgrade, websocket]}
        //                if ((req.Headers.ContainsKey("UPGRADE") == true) && (req.Headers["UPGRADE"] == "websocket"))
        //                {
        //                    CQWebSocket websocket = new CQWebSocket();
        //                    CQTCPHandler handler;
        //                    this.Extension.ControlTransfer(req.HandlerID, out handler);
        //                    websocket.Open(handler, req.HeaderRaw, req.HeaderRaw.Length);
        //                    this.Extension.CacheControl<CQWebSocket>(CacheOperates.Create, websocket.ID, ref websocket, "websocket");
        //                    process_result_code = ServiceProcessResults.WebSocket;
        //                }
        //            }
        //            break;

        //    }

        //    return result;
        //}

        public bool TimeOut_Cache()
        {
            return true;
        }

        public bool RegisterCacheManager()
        {
            //this.Extension.CacheManger_Registered<CQCacheManager>("websocket");
            return true;
        }

        //public bool Process_Cache(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        //{
        //    bool result = true;
        //    process_result_code = ServiceProcessResults.None;
        //    resp = null;
        //    switch (req.URL.LocalPath.ToUpperInvariant())
        //    {
        //        //case "/WEBSOCKET_TEST":
        //        //    {
        //        //        //[4] = {[Upgrade, websocket]}
        //        //        if ((req.Headers.ContainsKey("UPGRADE") == true) && (req.Headers["UPGRADE"] == "websocket"))
        //        //        {
        //        //            CQWebSocket websocket = new CQWebSocket();
        //        //            CQTCPHandler handler;
        //        //            this.Extension.ControlTransfer(req.HandlerID, out handler);
        //        //            websocket.Open(handler, req.HeaderRaw, req.HeaderRaw.Length);
        //        //        }
        //        //    }
        //        //    break;
        //    }
        //    return result;
        //}
    }
}
