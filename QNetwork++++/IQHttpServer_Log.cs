using QNetwork.Http.Server.Accept;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNetwork.Http.Server.Log
{
    public enum LogStates_Accept
    {
        Create,
        Closed,
        Opening,
        Fail,
        Normal
    }

    public enum LogStates_Process
    {
        CreateHandler,
        CreateRequest,
        ProcessRequest,
        Service_Begin,
        Service_End,
        CreateResponse,
        ProcessResponse,
        SendResponse,
        SendResponse_Compelete,
        DestoryResponse,
        DestoryRequest,
        DestoryHandler
    }
    public enum LogStates_Cache
    {
        CreateManager,
        CreateCahce,
        DestoryCache,
        DestoryManager
    }

    public interface IQHttpServer_Log
    {
        bool LogProcess(LogStates_Process state, string handler_id, string process_id, DateTime time, CQHttpRequest request, CQHttpResponse response);
        bool LogAccept(LogStates_Accept state, string ip, int port, CQSocketListen obj);
        bool LogCache(LogStates_Cache state, DateTime time, string id, string name);
    }

    public class CQDefault_Log : IQHttpServer_Log
    {
        public bool LogProcess(LogStates_Process state, string handler_id, string process_id, DateTime time, CQHttpRequest request, CQHttpResponse response)
        {
            System.Diagnostics.Trace.WriteLine(string.Format("State:{0} Handler:{1} Process:{2} time:{3}"
                , state
                , handler_id
                , process_id
                , time.ToString("yyyy/MM/dd HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
            return true;
        }

        public bool LogCache(LogStates_Cache state, DateTime time, string id, string name)
        {
            System.Diagnostics.Trace.WriteLine(string.Format("State:{0} Handler:{1} Process:{2} time:{3}"
               , state
               , id
               , name
               , time.ToString("yyyy/MM/dd HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
            return true;
        }

        public bool LogAccept(LogStates_Accept state, string ip, int port, CQSocketListen obj)
        {
            System.Diagnostics.Trace.WriteLine(string.Format("State:{0} IP:{1} Port:{2} "
                  , state
                  , ip
                  , port));
            return true;
        }
    }
}
