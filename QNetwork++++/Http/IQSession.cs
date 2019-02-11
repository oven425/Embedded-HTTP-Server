using QNetwork.Http.Server.Log;
using QNetwork.Http.Server.Protocol;
using QNetwork.Http.Server.Router;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNetwork
{
    public enum SessionStates
    {
        None,
        Request_Create,
        Session_Create,
        Request_Process,
        Response_Create,
        Response_Send,
        Response_SendCompelete,
        Session_Destory
    }
    public interface IQSession:IDisposable
    {
        string HandlerID { get; }
        CQHttpRequest Request { set; get; }
        CQHttpResponse Response { set; get; }
        string ID { get; }
        CQRouterData Router { set; get; }
        DateTime Request_Create { set; get; }
        DateTime Session_Create { set; get; }
        DateTime Request_Process { set; get; }
        DateTime Response_Create { set; get; }
        DateTime Response_Send { set; get; }
        DateTime Response_SendCompelete { set; get; }
        DateTime Session_Destory { set; get; }
        SessionStates SessionState { set; get; }
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
        public DateTime Request_Create { set; get; }
        public DateTime Session_Create { set; get; }
        public DateTime Request_Process { set; get; }
        public DateTime Response_Create { set; get; }
        public DateTime Response_Send { set; get; }
        public DateTime Response_SendCompelete { set; get; }
        public DateTime Session_Destory { set; get; }
        public SessionStates SessionState { set; get; }

        public CQSession(string handler)
        {
            this.m_HandlerID = handler;
            this.m_ID = Guid.NewGuid().ToString();
        }

        public void Dispose()
        {
           if(this.Request != null)
            {
                this.Request.Dispose();
                this.Request = null;
            }
           if(this.Response != null)
            {
                this.Response.Dispose();
                this.Response = null;
            }
        }
    }
}
