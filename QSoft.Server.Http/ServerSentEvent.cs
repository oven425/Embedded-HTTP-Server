using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Concurrent;

namespace QSoft.Server.Http
{
    public class ServerSentEvent
    {
        ConcurrentBag<HttpListenerResponse> m_Resps = new ConcurrentBag<HttpListenerResponse>();
        public void Add(HttpListenerResponse resp)
        {
            this.m_Resps.Add(resp);
        }

        public void Send()
        {

        }
    }
}
