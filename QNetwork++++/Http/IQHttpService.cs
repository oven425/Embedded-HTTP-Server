using QNetwork.Http.Server.Cache;
using QNetwork.Http.Server.Handler;
using QNetwork.Http.Server.Log;
using QNetwork.Http.Server.Protocol;
using QNetwork.Http.Server.Router;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace QNetwork.Http.Server.Service
{
    public enum ServiceProcessResults
    {
        None = 0,
        OK = 1,
        PassToPushService = 2,
        PassToOther = 3,
        ControlTransfer = 4,
        WebSocket = 5
    }
    public interface IQHttpService : IDisposable
    {
        IQHttpServer_Log Logger { set; get; }
        bool RegisterCacheManager();
        //bool Process(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code);
        bool CloseHandler(List<string> handlers);
        IQHttpServer_Extension Extension { set; get; }
        //List<string> Methods { get; }
    }
}
namespace QNetwork.Http.Server
{
    //public interface IQProtocolParse:IDisposable
    //{
    //    bool Parse(string tcphandler_id, byte[] data, int len);
    //    bool Parse(string tcphandler_id, Stream data);
    //}

    public enum CacheOperates
    {
        Get,
        Create,
        Destory
    }


   public enum CacheIDProviderTypes
    {
        GetID,
        ResetID,
    }
    
    public interface IQHttpServer_Extension
    {
        bool SendMultiPart(List<CQHttpResponse> datas);
        bool ControlTransfer(string handlerid, out CQTCPHandler tcphandler);
        
        bool CacheControl<T>(CacheOperates op, string id,  ref T cache, string manager_id = "default") where T : CQCacheBase, new();
    }

    public interface IQHttpServer_Operation
    {
        bool GetRouters(out List<CQRouterData> datas);
        bool GetAccetpAddress(out List<string> addresslist);
        bool Reboot();
        bool Open();
        bool Close();
    }
}
