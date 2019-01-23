using QNetwork.Http.Server;
using QNetwork.Http.Server.Cache;
using QNetwork.Http.Server.Handler;
using QNetwork.Http.Server.Log;
using QNetwork.Http.Server.Protocol;
using QNetwork.Http.Server.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace WPF_Server_Http.Service
{
    public class CQHttpService_Playback : IQHttpService
    {
        public CQHttpService_Playback()
        {
        }

        public IQHttpServer_Extension Extension { set; get; }
        public IQHttpServer_Log Logger { set; get; }

        public bool CloseHandler(List<string> handlers)
        {
            return true;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        [CQServiceMethod("/PLAYBACKCONTROL")]
        public bool PLAYBACKCONTROL(string handlerid, CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            bool result = true;
            process_result_code = ServiceProcessResults.None;
            resp = null;


            return result;
        }

        [CQServiceMethod("/PLAYBACK")]
        public bool PLAYBACK(string handlerid, CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            bool result = true;
            process_result_code = ServiceProcessResults.None;
            resp = null;
            //CQCache_Playback cache = null;

            //string query_str = req.URL.Query;
            //process_result_code = ServiceProcessResults.ControlTransfer;
            //CQTCPHandler tcp;
            //this.Extension.ControlTransfer(req.HandlerID, out tcp);

            //this.Extension.CacheControl<CQCache_Playback>(CacheOperates.Create, "", ref cache, "playback");
            //cache.Open(tcp, req);

            return result;
        }
        public bool RegisterCacheManager()
        {
            bool result = true;
            //this.Extension.CacheManger_Registered<CQCacheManager>("playback");

            return result;
        }
    }
}
