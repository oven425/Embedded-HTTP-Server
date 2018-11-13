using QNetwork.Http.Server.Cache;
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
        bool Process(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code);
        bool CloseHandler(List<string> handlers);
        IQHttpServer_Extension Extension { set; get; }
        List<string> Methods { get; }
    }

    public interface IQHttpRouter
    {
        IQHttpService Process(CQHttpRequest req);
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
        Check,
        Destory,
    }

    public interface IQHttpServer_Log
    {
        bool Log_Accept();
        bool Log_Handler();
        bool Log_Request();
        bool Log_Response();
    }

    public interface IQHttpServer_Extension
    {
        bool SendMultiPart(List<CQHttpResponse> datas);
        bool ControlTransfer(string handlerid, out CQTCPHandler tcphandler);
        bool CacheControl<T>(CacheOperates op, string id,  ref T cache, bool not_exist_build=true, string nickname = "default") where T : CQCacheBase, new();
        bool CacheManger_Registered<T>(string name = "default") where T : CQCacheManager, new();
    }

    public interface IQHttpServer_Operation
    {
        bool GetAccetpAddress(out List<string> addresslist);
        bool Reboot();
        bool Open();
        bool Close();
    }
}
