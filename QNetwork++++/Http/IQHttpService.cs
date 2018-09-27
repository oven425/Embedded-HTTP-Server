using System;
using System.Collections.Generic;
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
        bool Process(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code, out bool to_cache);
        bool Process_Cache(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code);
        bool TimeOut_Cache();
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
    

    public interface IQHttpServer_Extension
    {
        bool SendMultiPart(List<CQHttpResponse> datas);
        bool ControlTransfer(string handlerid, out CQTCPHandler tcphandler);
    }

   

    //public abstract class CQHttpService : IQHttpService
    //{
    //    protected List<string> m_Methods = new List<string>();
    //    List<string> m_PushHandler = new List<string>();
    //    virtual public bool CloseHandler(List<string> handlers) { return true; }
    //    //protected object m_CachesLock = new object();
    //    //protected Dictionary<string, IQCacheData> m_Caches = new Dictionary<string, IQCacheData>();

    //    public IQHttpServer_Extension Extension { get; set; }
    //    public List<string> Methods { set { } get { return this.m_Methods; } }


    //    abstract public bool Process(CQHttpRequest req, out CQHttpResponse resp, out int process_result_code, out bool to_cache);
    //    virtual public bool TimeOut_Cache()
    //    {
    //        bool result = true;
    //        //List<string> keys = new List<string>();

    //        //Monitor.Enter(this.m_CachesLock);
    //        //for (int i = 0; i < this.m_Caches.Count; i++)
    //        //{
    //        //    if (this.m_Caches.ElementAt(i).Value.IsTimeOut(TimeSpan.FromMinutes(1)) == true)
    //        //    {
    //        //        keys.Add(this.m_Caches.ElementAt(i).Key);
    //        //    }
    //        //}
    //        //foreach (string key in keys)
    //        //{
    //        //    this.m_Caches.Remove(key);
    //        //}
    //        //Monitor.Exit(this.m_CachesLock);
    //        return result;
    //    }
    //}
}
