using QNetwork.Http.Server.Log;
using QNetwork.Http.Server.Protocol;
using QNetwork.Http.Server.Router;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNetwork
{
    public interface IQSession:IDisposable
    {
        string HandlerID { get; }
        CQHttpRequest Request { set; get; }
        CQHttpResponse Response { set; get; }
        string ID { get; }
        CQRouterData Router { set; get; }
    }

    public class CQSession : IQSession
    {
        string m_ID;
        string m_HandlerID;
        public CQHttpRequest Request { set; get; }
        public CQHttpResponse Response { set; get; }

        public string ID => this.m_ID;

        public string HandlerID => this.m_HandlerID;

        public CQRouterData Router { set; get; }

        public CQSession(string handler)
        {
            this.m_HandlerID = handler;
            this.m_ID = Guid.NewGuid().ToString();
        }

        public void Dispose()
        {
           
        }
    }
}
