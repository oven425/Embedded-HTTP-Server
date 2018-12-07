using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNetwork.Http.Server.Log
{
    public interface IQHttpServer_Log
    {
        bool LogProcess(LogStates_Process state, string handler_id, string prcoess_id, DateTime time, CQHttpRequest request, CQHttpResponse response);
        bool LogAccept(LogStates_Accept state, string ip, int port);
        bool LogCache(LogStates_Cache state, DateTime time, string id, string name);
    }

    public class CQDefault_Log : IQHttpServer_Log
    {
        public bool LogAccept(LogStates_Accept state, string ip, int port)
        {
            throw new NotImplementedException();
        }

        public bool LogCache(LogStates_Cache state, DateTime time, string id, string name)
        {
            throw new NotImplementedException();
        }

        public bool LogProcess(LogStates_Process state, string handler_id, string prcoess_id, DateTime time, CQHttpRequest request, CQHttpResponse response)
        {
            throw new NotImplementedException();
        }
    }
}
