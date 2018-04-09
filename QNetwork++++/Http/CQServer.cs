using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Security.Cryptography;

namespace QNetwork.Http.Server
{
    public class CQHttpServer: IQHttpServer_Extension
    {
        List<BackgroundWorker> m_Threads = new List<BackgroundWorker>();
        Dictionary<CQSocketListen_Address, CQSocketListen> m_AcceptSockets = new Dictionary<CQSocketListen_Address, CQSocketListen>();
        BackgroundWorker m_Thread;
        Dictionary<string, CQHttpHandler> m_Sessions = new Dictionary<string, CQHttpHandler>();
        List<CQHttpRequest> m_Requests = new List<CQHttpRequest>();
        object m_RequestsLock = new object();
        List<IQHttpService> m_Services = new List<IQHttpService>();
        public CQHttpServer()
        {
            for (int i = 0; i < 8; i++)
            {
                BackgroundWorker thread = new BackgroundWorker();
                thread.DoWork += new DoWorkEventHandler(thread_DoWork);
                this.m_Threads.Add(thread);
            }
            this.m_Thread = new BackgroundWorker();
            this.m_Thread.DoWork += new DoWorkEventHandler(m_Thread_DoWork);
        }

        object m_SessionsLock = new object();
        void m_Thread_DoWork(object sender, DoWorkEventArgs e)
        {
            List<string> closehandlers = new List<string>();
            while (true)
            {
                Monitor.Enter(this.m_SessionsLock);
                var vv = this.m_Sessions.Values.Where(x=>x.IsEnd == true).ToList();
                closehandlers.Clear();
                foreach (CQHttpHandler handler in vv)
                {
                    handler.Close();
                    Monitor.Enter(this.m_RequestsLock);
                    this.m_Requests.RemoveAll(x => x.HandlerID == handler.ID);
                    closehandlers.Add(handler.ID);
                    Monitor.Exit(this.m_RequestsLock);
                    this.m_Sessions.Remove(handler.ID);
                }
                Monitor.Exit(this.m_SessionsLock);
                if (closehandlers.Count > 0)
                {
                    for (int i = 0; i < this.m_Services.Count; i++)
                    {
                        this.m_Services[i].CloseHandler(closehandlers);
                    }
                }
                foreach (CQHttpService service in this.m_Services)
                {
                    service.TimeOut_Cache();
                }
                var threads_nobusy = this.m_Threads.Where(x=>x.IsBusy == false);
                if (threads_nobusy.Count() > 0)
                {
                    CQHttpRequest req = this.GetRequest();
                    if (req != null)
                    {
                        threads_nobusy.First().RunWorkerAsync(req);
                    }
                }
                System.Threading.Thread.Sleep(100);
            }
        }

        CQHttpRequest GetRequest()
        {
            CQHttpRequest req = null;
            try
            {
                Monitor.Enter(this.m_RequestsLock);
                if (this.m_Requests.Count > 0)
                {
                    req = this.m_Requests[0];
                    this.m_Requests.RemoveAt(0);
                }
            }
            catch (Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
            }
            finally
            {
                Monitor.Exit(this.m_RequestsLock);
            }
            return req;
        }

        void thread_DoWork(object sender, DoWorkEventArgs e)
        {
            CQHttpRequest req = e.Argument as CQHttpRequest;
            while (req != null)
            {
                int process_result;
                CQHttpResponse resp;
                //this.ProcessWebSocket(req, out resp, out process_result);
                //if(process_result == 0)
                {
                    this.ProcessRequest(req, out resp, out process_result);
                    if ((resp != null) && (process_result == 1))
                    {
                        Monitor.Enter(this.m_SessionsLock);
                        if (this.m_Sessions.ContainsKey(req.HandlerID) == true)
                        {
                            CQHttpHandler handler = this.m_Sessions[req.HandlerID];
                            handler.SendResp(resp);
                        }
                        Monitor.Exit(this.m_SessionsLock);
                    }
                    else if (process_result == 2)
                    {

                    }
                    else
                    {
                        Monitor.Enter(this.m_SessionsLock);
                        if (this.m_Sessions.ContainsKey(req.HandlerID) == true)
                        {
                            resp = new CQHttpResponse(req.HandlerID);
                            resp.Set403();
                            CQHttpHandler handler = this.m_Sessions[req.HandlerID];
                            handler.SendResp(resp);
                        }
                        Monitor.Exit(this.m_SessionsLock);
                    }
                    if(req.Content != null)
                    {
                        req.Content.Close();
                        req.Content.Dispose();
                        req.Content = null;
                    }
                }
                req = this.GetRequest();
            }
        }

        bool ProcessAccept(Socket client, byte[] acceptbuf, int accept_len)
        {
            bool result = true;
            CQTCPHandler tcphandler = new CQTCPHandler(client);
            CQHttpHandler session = new CQHttpHandler(tcphandler);
            session.OnNewRequest += new CQHttpHandler.NewRequestDelegate(session_OnNewRequest);
            Monitor.Enter(this.m_SessionsLock);
            if (this.m_Sessions.ContainsKey(session.ID) == true)
            {
                System.Diagnostics.Trace.WriteLine("");
            }
            this.m_Sessions.Add(session.ID, session);
            Monitor.Exit(this.m_SessionsLock);
            session.Open(acceptbuf, accept_len);
            return result;
        }

        bool session_OnNewRequest(CQHttpHandler hadler, List<CQHttpRequest> requests)
        {
            bool result = true;
            Monitor.Enter(this.m_RequestsLock);
            this.m_Requests.AddRange(requests);
            Monitor.Exit(this.m_RequestsLock);
            return result;
        }

        protected virtual bool ProcessAuth(CQHttpRequest request, out CQHttpResponse resp)
        {
            resp = null;
            bool result = true;
            if (request.Headers.ContainsKey("Authorization") == false)
            {
                resp = new CQHttpResponse(request.HandlerID);
                resp.Set401();
            }
            else
            {
                string account = "admin";
                string password = "admin";
                string auth_str = request.Headers["Authorization"];
                int index = auth_str.IndexOf(" ");
                string type = auth_str.Substring(0, index);
                auth_str = auth_str.Remove(0, index+1);
                
                switch (type)
                {
                    case "Basic":
                        {
                            string bb = string.Format("{0}:{1}",account, password);
                            string bbstr = Convert.ToBase64String(Encoding.UTF8.GetBytes(bb));
                            if (bbstr != auth_str)
                            {
                                resp = new CQHttpResponse(request.HandlerID);
                                resp.Set401();
                            }
                        }
                        break;
                    case "Digest":
                        {
                            string username = "";
                            string nonce = "";
                            string realm = "";
                            string uri = "";
                            string response = "";
                            string[] ll_digest = auth_str.Split(new string[] { " , ",", ", " ,",","}, StringSplitOptions.None);
                            foreach (string oo in ll_digest)
                            {
                                if (oo.Contains("username=") == true)
                                {
                                    username = oo.Substring("username=".Length);
                                    username = username.Replace("\"", "");
                                }
                                else if (oo.Contains("nonce") == true)
                                {
                                    nonce = oo.Substring("nonce=".Length);
                                    nonce = nonce.Replace("\"", "");
                                }
                                else if (oo.Contains("realm") == true)
                                {
                                    realm = oo.Substring("realm=".Length);
                                    realm = realm.Replace("\"", "");
                                }
                                else if (oo.Contains("uri") == true)
                                {
                                    uri = oo.Substring("uri=".Length);
                                    uri = uri.Replace("\"", "");
                                }
                                else if (oo.Contains("response") == true)
                                {
                                    response = oo.Substring("response=".Length);
                                    response = response.Replace("\"", "");
                                }
                            }
                            string bbstr = this.CreateDigestResponse(account, password, realm, uri, request.Method, nonce);
                            if (bbstr != response)
                            {
                                resp = new CQHttpResponse(request.HandlerID);
                                resp.Set401();
                            }

                        }
                        break;
                    default:
                        {
                        }
                        break;
                }
            }

            return result;
        }

        string CreateDigestResponse(string account, string password, string realm, string uri, string method, string nonce)
        {
            MD5 md5_1 = MD5.Create();
            byte[] data = md5_1.ComputeHash(Encoding.Default.GetBytes(account + ":" + realm + ":" + password));
            StringBuilder md5_1str = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                md5_1str.Append(data[i].ToString("x2"));
            }

            MD5 md5_2 = MD5.Create();
            data = md5_2.ComputeHash(Encoding.Default.GetBytes(method + ":" + uri));
            StringBuilder md5_2str = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                md5_2str.Append(data[i].ToString("x2"));
            }

            MD5 md5_3 = MD5.Create();
            data = md5_3.ComputeHash(Encoding.Default.GetBytes(md5_1str + ":" + nonce + ":" + md5_2str));
            StringBuilder md5_3str = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                md5_3str.Append(data[i].ToString("x2"));
            }

            return md5_3str.ToString();
        }

        protected virtual bool ProcessWebSocket(CQHttpRequest request, out CQHttpResponse resp, out int process_result_code)
        {
            bool result = true;
            process_result_code = 0;
            resp = null;
            process_result_code = 3;
            System.Diagnostics.Trace.WriteLine(request.ResourcePath);
            if ((request.Headers.ContainsKey("Upgrade") == true) && (request.Headers["Upgrade"] == "websocket"))
            {
                //CQWebSocket websocket = new CQWebSocket();
                Socket socket;
                //this.SendControlTransfer(req.HandlerID, out socket);
                //websocket.Open(socket, req.HeaderRaw, req.HeaderRaw.Length);
            }


            return result;
        }

        protected virtual bool ProcessRequest(CQHttpRequest request, out CQHttpResponse resp, out int process_result_code)
        {
            bool result = true;
            process_result_code = 0;
            //System.Diagnostics.Trace.WriteLine(request.ResourcePath);
            resp = null;
            for (int i = 0; i < this.m_Services.Count; i++)
            {
                this.m_Services[i].Process(request, out resp, out process_result_code);

                if (process_result_code != (int)ServiceProcessResults.None)
                {
                    break;
                }

            }
            
            return result;
        }

        public bool Close()
        {
            bool result = true;
            for(int i=0; i<this.m_AcceptSockets.Count; i++)
            {
                this.m_AcceptSockets.ElementAt(i).Value.Close();
            }
            this.m_AcceptSockets.Clear();
            return result;
        }

        public bool CloseListen(CQSocketListen_Address data)
        {
            bool result = true;
            EndPoint end = data.ToEndPint();
            if(this.m_AcceptSockets.ContainsKey(data) == true)
            {
                this.m_AcceptSockets[data].Close();
                this.m_AcceptSockets.Remove(data);
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("");
            }

            return result;
        }

        public bool OpenListen(CQSocketListen_Address data)
        {
            bool result = true;
            CQSocketListen listen = new CQSocketListen(data);
            listen.OnListenState += Listen_OnListenState;
            listen.OnNewClient += Listen_OnNewClient;
            // listen.Address = new IPEndPoint(IPAddress.Parse(address[i].IP), address[i].Port);
            listen.Open();
            this.m_AcceptSockets.Add(data, listen);
            return result;
        }

        public delegate bool ListentStateChangeDelegate(CQSocketListen_Address listen_addres, ListenStates state);
        public event ListentStateChangeDelegate OnListentStateChange;
        private bool Listen_OnListenState(CQSocketListen listen)
        {
            if(this.OnListentStateChange != null)
            {
                this.OnListentStateChange(listen.Addrss, listen.ListenState);
            }
            return true;
        }

        public bool Open(List<CQSocketListen_Address> address, List<CQHttpService> services, bool adddefault=true)
        {
            bool result = true;
            for (int i = 0; i < services.Count; i++)
            {
                services[i].Extension = this;
                //services[i].OnMultiPart += new CQHttpService.MultiPartDelegate(CQHttpServer_OnMultiPart);
                services[i].OnControlTransfer += new CQHttpService.ControlTransferDelegate(CQHttpServer_OnControlTransfer);
                this.m_Services.Add(services[i]);
            }
            //this.m_Services.AddRange(services);
            if(adddefault == true)
            {
                this.m_Services.Add(new CQHttpDefaultService());
            }
            for(int i=0; i<address.Count; i++)
            {
                this.OpenListen(address[i]);
            }
            if (this.m_Thread.IsBusy == false)
            {
                this.m_Thread.RunWorkerAsync();
            }
            return result;
        }


        private bool Listen_OnNewClient(Socket socket, byte[] data, int len)
        {
            this.ProcessAccept(socket, data, len);
            return true;
        }

        bool CQHttpServer_OnControlTransfer(string handlerid, out Socket socket)
        {
            socket = null;
            Monitor.Enter(this.m_RequestsLock);
            this.m_Requests.RemoveAll(x => x.HandlerID == handlerid);
            Monitor.Exit(this.m_RequestsLock);

            Monitor.Enter(this.m_SessionsLock);
            if (this.m_Sessions.ContainsKey(handlerid) == true)
            {
                this.m_Sessions[handlerid].ControlTransfer(out socket);
                this.m_Sessions.Remove(handlerid);
            }
            Monitor.Exit(this.m_SessionsLock);

            Monitor.Enter(this.m_RequestsLock);
            this.m_Requests.RemoveAll(x => x.HandlerID == handlerid);
            Monitor.Exit(this.m_RequestsLock);
            return true;
        }

        bool CQHttpServer_OnMultiPart(List<CQHttpResponse> datas)
        {
            for (int i = 0; i < datas.Count; i++)
            {
                Monitor.Enter(this.m_SessionsLock);
                if (this.m_Sessions.ContainsKey(datas[i].HandlerID) == true)
                {
                    this.m_Sessions[datas[i].HandlerID].SendResp(datas[i]);
                }
                Monitor.Exit(this.m_SessionsLock);
            }
            return true;
        }

        public bool SendMultiPart(List<CQHttpResponse> datas)
        {
            for (int i = 0; i < datas.Count; i++)
            {
                Monitor.Enter(this.m_SessionsLock);
                if (this.m_Sessions.ContainsKey(datas[i].HandlerID) == true)
                {
                    this.m_Sessions[datas[i].HandlerID].SendResp(datas[i]);
                }
                Monitor.Exit(this.m_SessionsLock);
            }
            return true;
        }

        public bool SendControlTransfer(string handlerid, out Socket socket)
        {
            throw new NotImplementedException();
        }
    }

   

    public interface IQCacheData
    {
        string ID { get; }
        bool IsTimeOut(TimeSpan timeout);
        object Data { set; get; }
    }

    public class CQCacheData : IQCacheData
    {
        public CQCacheData(string id)
        {
            this.m_CreateTime = DateTime.Now;
            this.m_ID = id;
        }
        string m_ID;
        public string ID { get { return this.m_ID; } }
        DateTime m_CreateTime;
        public bool IsTimeOut(TimeSpan timeout)
        {
            bool result = true;
            if (DateTime.Now - this.m_CreateTime > timeout)
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }

        public object Data { set; get; }

    }

    public enum ServiceProcessResults
    {
        None=0,
        OK=1,
        PassToPushService=2,
        PassToOther=3,
        ControlTransfer=4,
        WebSocket=5
    }

    public interface IQHttpServer_Extension
    {
        bool SendMultiPart(List<CQHttpResponse> datas);
        bool SendControlTransfer(string handlerid, out Socket socket);
    }

    public interface IQHttpService
    {
        bool Process(CQHttpRequest req, out CQHttpResponse resp, out int process_result_code);
        bool TimeOut_Cache();
        bool CloseHandler(List<string> handlers);
        IQHttpServer_Extension Extension { set; get; }
    }



    public abstract class CQHttpService: IQHttpService
    {
        List<string> m_PushHandler = new List<string>();
        virtual public bool CloseHandler(List<string> handlers) { return true; }
        protected object m_CachesLock = new object();
        protected Dictionary<string, IQCacheData> m_Caches = new Dictionary<string, IQCacheData>();

        public IQHttpServer_Extension Extension { get; set; }

        abstract public bool Process(CQHttpRequest req, out CQHttpResponse resp, out int process_result_code);
        virtual public bool TimeOut_Cache()
        {
            bool result = true;
            List<string> keys = new List<string>();

            Monitor.Enter(this.m_CachesLock);
            for (int i = 0; i < this.m_Caches.Count; i++)
            {
                if (this.m_Caches.ElementAt(i).Value.IsTimeOut(TimeSpan.FromMinutes(1)) == true)
                {
                    keys.Add(this.m_Caches.ElementAt(i).Key);
                }
            }
            foreach (string key in keys)
            {
                this.m_Caches.Remove(key);
            }
            Monitor.Exit(this.m_CachesLock);
            return result;
        }
        //public delegate bool MultiPartDelegate(List<CQHttpResponse> datas);
        //public event MultiPartDelegate OnMultiPart;
        //protected bool SendMultiPart(List<CQHttpResponse> datas)
        //{
        //    bool result = true;
        //    if (this.OnMultiPart != null)
        //    {
        //        this.OnMultiPart(datas);
        //    }
        //    return result;
        //}

        public delegate bool ControlTransferDelegate(string handlerid, out Socket socket);
        public event ControlTransferDelegate OnControlTransfer;
        protected bool SendControlTransfer(string handlerid, out Socket socket)
        {
            bool result = true;
            socket = null;
            if (this.OnControlTransfer != null)
            {
                this.OnControlTransfer(handlerid, out socket);
            }
            return result;
        }
    }
    

    
}
