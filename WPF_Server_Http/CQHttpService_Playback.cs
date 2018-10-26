using QNetwork.Http.Server;
using QNetwork.Http.Server.Cache;
using QNetwork.Http.Server.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace WPF_Server_Http
{
    public class CQHttpService_Playback : IQHttpService
    {
        List<string> m_Methods;
        
        
        bool m_IsEnd = false;
        public CQHttpService_Playback()
        {
            
            this.m_Methods = new List<string>();
            this.m_Methods.Add("/PLAYBACK");
            this.Methods.Add("/PLAYBACKCONTROL");
        }

        

        public IQHttpServer_Extension Extension { set; get; }

        public List<string> Methods => this.m_Methods;

        public bool CloseHandler(List<string> handlers)
        {
            return true;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool Process(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            bool result = true;
            process_result_code = ServiceProcessResults.None;
            resp = null;
            switch (req.URL.LocalPath.ToUpperInvariant())
            {
                case "/PLAYBACKCONTROL":
                    {

                    }
                    break;
                case "/PLAYBACK":
                    {
                        CQCache_Playback cache = null;
                        
                        string query_str = req.URL.Query;
                        process_result_code = ServiceProcessResults.ControlTransfer;
                        CQTCPHandler tcp;
                        this.Extension.ControlTransfer(req.HandlerID, out tcp);

                        this.Extension.CacheControl<CQCache_Playback>(CacheOperates.Get, "", ref cache, true, "playback");
                        cache.Open(tcp, req);
                    }
                    break;
            }

            return result;
        }

        

        public bool RegisterCacheManager()
        {
            bool result = true;
            this.Extension.CacheManger_Registered<CQCacheManager>("playback");

            return result;
        }
    }
}
