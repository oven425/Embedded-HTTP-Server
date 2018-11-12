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
using QNetwork.Http.Server.Accept;
using QNetwork.Http.Server.Service;
using QNetwork.Http.Server.Cache;

namespace QNetwork.Http.Server
{
    public class CQHttpServer: IQHttpServer_Extension
    {
        public List<IQHttpRouter> Routers { set; get; }
        List<BackgroundWorker> m_Threads = new List<BackgroundWorker>();
        Dictionary<CQSocketListen_Address, CQSocketListen> m_AcceptSockets = new Dictionary<CQSocketListen_Address, CQSocketListen>();
        BackgroundWorker m_Thread;
        Dictionary<string, CQHttpHandler> m_Sessions = new Dictionary<string, CQHttpHandler>();
        List<CQHttpRequest> m_Requests = new List<CQHttpRequest>();
        object m_RequestsLock = new object();
        object m_CacheManagersLock = new object();
        Dictionary<string, CQCacheManager> m_CacheManagers = new Dictionary<string, CQCacheManager>();
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
            this.Routers = new List<IQHttpRouter>();
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
                    if (this.OnHttpHandlerChange != null)
                    {
                        this.OnHttpHandlerChange(handler, false);
                    }
                    handler.Close();
                    
                    Monitor.Enter(this.m_RequestsLock);
                    this.m_Requests.RemoveAll(x => x.HandlerID == handler.ID);
                    closehandlers.Add(handler.ID);
                    Monitor.Exit(this.m_RequestsLock);
                    this.m_Sessions.Remove(handler.ID);
                }
                Monitor.Exit(this.m_SessionsLock);
                //if (closehandlers.Count > 0)
                //{
                //    for (int i = 0; i < this.m_Services.Count; i++)
                //    {
                //        this.m_Services[i].CloseHandler(closehandlers);
                //    }
                //}

                for(int i= 0; i<this.m_CacheManagers.Count; i++)
                {
                    this.m_CacheManagers.ElementAt(i).Value.TimeOut();
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
                ServiceProcessResults process_result;
                CQCacheBase cache;
                CQHttpResponse resp;
                //this.ProcessWebSocket(req, out resp, out process_result);
                //if(process_result == 0)
                {
                    this.ProcessRequest(req, out resp, out process_result);
                    if ((resp != null) && (process_result == ServiceProcessResults.OK))
                    {
                        Monitor.Enter(this.m_SessionsLock);
                        if (this.m_Sessions.ContainsKey(req.HandlerID) == true)
                        {
                            CQHttpHandler handler = this.m_Sessions[req.HandlerID];
                            this.ServiceChange(req, null, Request_ServiceStates.Response);
                            handler.SendResp(resp);
                        }
                        Monitor.Exit(this.m_SessionsLock);
                    }
                    else if (process_result == ServiceProcessResults.PassToPushService)
                    {

                    }
                    else
                    {
                        Monitor.Enter(this.m_SessionsLock);
                        if (this.m_Sessions.ContainsKey(req.HandlerID) == true)
                        {
                            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
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

        public delegate bool HttpHandlerChangeDelegate(CQHttpHandler handler, bool isadd);
        public event HttpHandlerChangeDelegate OnHttpHandlerChange;
        bool ProcessAccept(CQSocketListen listen, Socket client, byte[] acceptbuf, int accept_len)
        {
            bool result = true;
            CQTCPHandler tcphandler = new CQTCPHandler(client, listen.Addrss);
            CQHttpHandler session = new CQHttpHandler(tcphandler);
            session.OnNewRequest += new CQHttpHandler.NewRequestDelegate(session_OnNewRequest);
            Monitor.Enter(this.m_SessionsLock);
            if (this.m_Sessions.ContainsKey(session.ID) == true)
            {
                System.Diagnostics.Trace.WriteLine("");
            }
            this.m_Sessions.Add(session.ID, session);
            if(this.OnHttpHandlerChange != null)
            {
                this.OnHttpHandlerChange(session, true);
            }
            Monitor.Exit(this.m_SessionsLock);
            session.Open(acceptbuf, accept_len);
            return result;
        }

        bool session_OnNewRequest(CQHttpHandler hadler, List<CQHttpRequest> requests)
        {
            bool result = true;
            Monitor.Enter(this.m_RequestsLock);
            this.m_Requests.AddRange(requests);
            for (int i = 0; i < requests.Count; i++)
            {
                this.ServiceChange(requests[i], null, Request_ServiceStates.Request);
            }
            Monitor.Exit(this.m_RequestsLock);
            
            return result;
        }

        protected virtual bool ProcessAuth(CQHttpRequest request, out CQHttpResponse resp)
        {
            resp = null;
            bool result = true;
            if (request.Headers.ContainsKey("Authorization") == false)
            {
                resp = new CQHttpResponse(request.HandlerID, request.ProcessID);
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
                                resp = new CQHttpResponse(request.HandlerID, request.ProcessID);
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
                                resp = new CQHttpResponse(request.HandlerID, request.ProcessID);
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

        protected virtual IQHttpService GetService(CQHttpRequest request)
        {
            IQHttpService service = null;
            var vv = this.m_Service.FirstOrDefault(x => x.Urls.Any(y => y == request.URL.LocalPath) == true);
            if (vv != null)
            {
                service = Activator.CreateInstance(vv.Service) as IQHttpService;
            }
            return service;
        }

        protected bool ProcessRequest(CQHttpRequest request, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            bool result = true;
            process_result_code = ServiceProcessResults.None;
            //cache = null;
            resp = null;
            IQHttpService instance = this.GetService(request);
            if(instance != null)
            {
                this.ServiceChange(request, instance, Request_ServiceStates.Service_Begin);
                instance.Extension = this;
                instance.RegisterCacheManager();
                if (instance != null)
                {
                    instance.Process(request, out resp, out process_result_code);
                }
                this.ServiceChange(request, instance, Request_ServiceStates.Service_End);
            }
            else
            {
                resp = new CQHttpResponse(request.HandlerID, "");
                resp.Set404();
            }

            //if ((this.m_Services1.ContainsKey(request.URL.LocalPath) == true) && ((process_result_code == (int)ServiceProcessResults.None)))
            //{
            //    List<Type> types = this.m_Services1[request.URL.LocalPath];
            //    if(types.Count > 0)
            //    {
            //        IQHttpService instance = Activator.CreateInstance(types[0]) as IQHttpService;
            //        this.ServiceChange(request, instance, Request_ServiceStates.Service_Begin);
            //        instance.Extension = this;
            //        instance.RegisterCacheManager();
            //        if (instance != null)
            //        {
            //            instance.Process(request, out resp, out process_result_code);
            //        }
            //        this.ServiceChange(request, instance, Request_ServiceStates.Service_End);
            //    }
            //    else
            //    {

            //    }
            //}
            //else
            //{

            //}

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

        public enum Request_ServiceStates
        {
            Request,
            Service_Begin,
            Service_End,
            Response,
            End
        }


        public delegate bool ServiceChangeDelegate(CQHttpRequest req, IQHttpService service, Request_ServiceStates isadd);
        public event ServiceChangeDelegate OnServiceChange;
        private void ServiceChange(CQHttpRequest req, IQHttpService service, Request_ServiceStates isadd)
        {
            if (this.OnServiceChange != null)
            {
                this.OnServiceChange(req, service, isadd);
            }
        }

       
        List<CQRouterData> m_Service = new List<CQRouterData>();
        public bool Open(List<CQSocketListen_Address> address, List<IQHttpService> services, bool adddefault=true)
        {
            bool result = true;
            //for (int i = 0; i < services.Count; i++)
            //{
            //    services[i].Extension = this;
            //    this.m_Services.Add(services[i]);
            //}
            //if(adddefault == true)
            //{
            //    this.m_Services.Add(new CQHttpDefaultService());
            //}
            for (int i = 0; i < address.Count; i++)
            {
                this.OpenListen(address[i]);
            }
            for (int i = 0; i < services.Count; i++)
            {
                CQRouterData rd = new CQRouterData();
                rd.Urls.AddRange(services[i].Methods);
                rd.Service = services[i].GetType();
                this.m_Service.Add(rd);
                //Type type = services[i].GetType();
                //for (int j = 0; j < services[i].Methods.Count; j++)
                //{
                //    if(this.m_Services1.ContainsKey(services[i].Methods[j]) == false)
                //    {
                //        this.m_Services1.Add(services[i].Methods[j], new List<Type>());
                //    }
                //    this.m_Services1[services[i].Methods[j]].Add(type);
                //}
            }

            if (adddefault == true)
            {
                CQHttpDefaultService ds = new CQHttpDefaultService();
                CQRouterData rd = new CQRouterData();
                rd.Urls.AddRange(ds.Methods);
                rd.Service = ds.GetType();
                this.m_Service.Add(rd);

                //for (int i=0; i<ds.Methods.Count; i++)
                //{
                //    if (this.m_Services1.ContainsKey(ds.Methods[i]) == false)
                //    {
                //        this.m_Services1.Add(ds.Methods[i], new List<Type>());
                //    }
                //    this.m_Services1[ds.Methods[i]].Add(ds.GetType());
                //}
            }
            if (this.m_Thread.IsBusy == false)
            {
                this.m_Thread.RunWorkerAsync();
            }
            return result;
        }


        private bool Listen_OnNewClient(CQSocketListen listen, Socket socket, byte[] data, int len)
        {
            this.ProcessAccept(listen, socket, data, len);
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

        public bool ControlTransfer(string handlerid, out CQTCPHandler tcphandler)
        {
            tcphandler = null;
            Monitor.Enter(this.m_RequestsLock);
            this.m_Requests.RemoveAll(x => x.HandlerID == handlerid);
            Monitor.Exit(this.m_RequestsLock);

            Monitor.Enter(this.m_SessionsLock);
            if (this.m_Sessions.ContainsKey(handlerid) == true)
            {
                this.m_Sessions[handlerid].ControlTransfer(out tcphandler);
                this.m_Sessions.Remove(handlerid);
            }
            Monitor.Exit(this.m_SessionsLock);

            Monitor.Enter(this.m_RequestsLock);
            this.m_Requests.RemoveAll(x => x.HandlerID == handlerid);
            Monitor.Exit(this.m_RequestsLock);
            return true;
        }

        virtual public bool CacheManger_Registered<T>(string name = "default") where T:CQCacheManager, new()
        {
            bool result = true;
            Monitor.Enter(this.m_CacheManagersLock);
            if(this.m_CacheManagers.ContainsKey(name) == false)
            {
                this.m_CacheManagers.Add(name, new T());
            }
            Monitor.Exit(this.m_CacheManagersLock);
            return result;
        }

        virtual public bool CacheControl<T>(CacheOperates op, string id, ref T cache, bool not_exist_build, string nickname = "default") where T : CQCacheBase, new()
        {
            bool result = true;
            Monitor.Enter(this.m_CacheManagersLock);
            switch (op)
            {
                case CacheOperates.Get:
                    {
                        CQCacheManager manager = null;
                        if (this.m_CacheManagers.ContainsKey(nickname) == false)
                        {
                            if(not_exist_build == true)
                            {
                                manager = new CQCacheManager();
                                this.m_CacheManagers.Add(nickname, manager);
                            }
                        }
                        else
                        {
                            manager = this.m_CacheManagers[nickname];
                        }
                        if(manager != null)
                        {
                            cache = manager.Get<T>(id, not_exist_build);
                        }
                    }
                    break;
            }
            Monitor.Exit(this.m_CacheManagersLock);
            return result;
        }
    }
}
