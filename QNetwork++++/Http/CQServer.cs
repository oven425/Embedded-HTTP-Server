﻿using System;
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
using QNetwork.Http.Server.Log;
using System.Reflection;
using QNetwork.Http.Server.Handler;
using QNetwork.Http.Server.Router;
using QNetwork.Http.Server.Protocol;

namespace QNetwork.Http.Server
{
    public class CQHttpServer: IQHttpServer_Extension,IQHttpServer_Log
    {
        List<BackgroundWorker> m_Threads = new List<BackgroundWorker>();
        Dictionary<CQSocketListen_Address, CQSocketListen> m_AcceptSockets = new Dictionary<CQSocketListen_Address, CQSocketListen>();
        BackgroundWorker m_Thread;
        Dictionary<string, CQHttpHandler> m_Sessions = new Dictionary<string, CQHttpHandler>();
        List<CQHttpRequest> m_Requests = new List<CQHttpRequest>();
        object m_RequestsLock = new object();
        object m_CacheManagersLock = new object();
        //Dictionary<string, CQCacheManager> m_CacheManagers = new Dictionary<string, CQCacheManager>();
        //Dictionary<string, IQCacheIDProvider> m_CacheID_Provider = new Dictionary<string, IQCacheIDProvider>();
        public IQHttpServer_Log Logger { set; get; }
        public CQHttpServer()
        {
            this.Logger = new CQDefault_Log();
            for (int i = 0; i < 8; i++)
            {
                BackgroundWorker thread = new BackgroundWorker();
                thread.DoWork += new DoWorkEventHandler(thread_DoWork);
                this.m_Threads.Add(thread);
            }
            this.m_Thread = new BackgroundWorker();
            this.m_Thread.DoWork += new DoWorkEventHandler(m_Thread_DoWork);

            
            //CQCacheID_Default<byte> aa = new CQCacheID_Default<byte>();
            //aa.NewID();
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
                    this.LogProcess(LogStates_Process.DestoryHandler, handler.ID, "", DateTime.Now, null, null);
                    handler.Close();
                    
                    Monitor.Enter(this.m_RequestsLock);
                    this.m_Requests.RemoveAll(x => x.HandlerID == handler.ID);
                    closehandlers.Add(handler.ID);
                    Monitor.Exit(this.m_RequestsLock);
                    this.m_Sessions.Remove(handler.ID);
                }
                Monitor.Exit(this.m_SessionsLock);


                for(int i=0; i<this.m_Caches.Count; i++)
                {
                    Dictionary<string, IQCache> caches = this.m_Caches.ElementAt(i).Value;
                    List<IQCache> ccs = caches.Values.Where(x => x.IsTimeOut(TimeSpan.FromSeconds(30)) == true).ToList();
                    for(int j= 0; j<ccs.Count; j++)
                    {
                        ccs[j].Dispose();
                        caches.Remove(ccs[j].ID);
                    }
                }
                var threads_nobusy = this.m_Threads.Where(x=>x.IsBusy == false);
                if (threads_nobusy.Count() > 0)
                {
                    CQHttpRequest req;
                    CQRouterData rd;
                    this.GetRequest(out req, out rd);
                    if (req != null)
                    {

                        threads_nobusy.First().RunWorkerAsync(new List<object>() { req, rd});
                    }
                }
                System.Threading.Thread.Sleep(100);
            }
        }

        bool GetRequest(out CQHttpRequest req, out CQRouterData router)
        {
            bool result = true;
            req = null;
            router = null;
            try
            {
                Monitor.Enter(this.m_RequestsLock);
                for(int i=0; i<this.m_Requests.Count; i++)
                {
                    CQRouterData rd = this.GetService(this.m_Requests[i]);
                    if(rd.LifeType == LifeTypes.Singleton)
                    {
                        if(rd.CurrentUse < rd.UseLimit)
                        {
                            req = this.m_Requests[i];
                            rd.CurrentUse = rd.CurrentUse + 1;
                        }
                    }
                    else
                    {
                        req = this.m_Requests[i];
                    }
                    if(req != null)
                    {
                        router = rd;
                        break;
                    }
                }
                if(req != null)
                {
                    this.m_Requests.Remove(req);
                }
                //if (this.m_Requests.Count > 0)
                //{
                //    req = this.m_Requests[0];
                //    this.m_Requests.RemoveAt(0);
                //}
            }
            catch (Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                result = false;
            }
            finally
            {
                Monitor.Exit(this.m_RequestsLock);
            }
            return result;
        }

        void thread_DoWork(object sender, DoWorkEventArgs e)
        {
            //CQHttpRequest req = e.Argument as CQHttpRequest;
            List<object> args = e.Argument as List<object>;
            CQHttpRequest req = null;
            CQRouterData rd = null;
            if(args.Count==2)
            {
                req = args[0] as CQHttpRequest;
                rd = args[1] as CQRouterData;
            }
            while (req != null)
            {
                ServiceProcessResults process_result;
                //CQCacheBase cache;
                CQHttpResponse resp;
                //this.ProcessWebSocket(req, out resp, out process_result);
                //if(process_result == 0)
                {
                    this.ProcessRequest(req, rd, out resp, out process_result);
                    
                    if ((resp != null) && (process_result == ServiceProcessResults.OK))
                    {
                        resp.Logger = this;
                        Monitor.Enter(this.m_SessionsLock);
                        if (this.m_Sessions.ContainsKey(req.HandlerID) == true)
                        {
                            CQHttpHandler handler = this.m_Sessions[req.HandlerID];

                            this.LogProcess(LogStates_Process.ProcessResponse, handler.ID, req.ProcessID, DateTime.Now, req, resp);
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
                            resp.Logger = this;
                            resp.Set403();
                            CQHttpHandler handler = this.m_Sessions[req.HandlerID];
                            handler.SendResp(resp);
                        }
                        Monitor.Exit(this.m_SessionsLock);
                    }
                    if (req.Content != null)
                    {
                        req.Content.Close();
                        req.Content.Dispose();
                        req.Content = null;
                    }
                }

                //req = this.GetRequest();
                this.GetRequest(out req, out rd);
            }
        }

        //public delegate bool HttpHandlerChangeDelegate(CQHttpHandler handler, bool isadd);
        //public event HttpHandlerChangeDelegate OnHttpHandlerChange;
        bool ProcessAccept(CQSocketListen listen, Socket client, byte[] acceptbuf, int accept_len)
        {
            bool result = true;
            
            CQTCPHandler tcphandler = new CQTCPHandler(client, listen.Addrss);
            CQHttpHandler session = new CQHttpHandler(tcphandler);
            this.LogProcess(LogStates_Process.CreateHandler, session.ID, "", DateTime.Now, null, null);
            session.OnNewRequest += new CQHttpHandler.NewRequestDelegate(session_OnNewRequest);
            Monitor.Enter(this.m_SessionsLock);
            if (this.m_Sessions.ContainsKey(session.ID) == true)
            {
                System.Diagnostics.Trace.WriteLine("");
            }
            this.m_Sessions.Add(session.ID, session);
            //if(this.OnHttpHandlerChange != null)
            //{
            //    this.OnHttpHandlerChange(session, true);
            //}
            Monitor.Exit(this.m_SessionsLock);
            session.Open(acceptbuf, accept_len);
            return result;
        }

        bool session_OnNewRequest(CQHttpHandler hadler, List<CQHttpRequest> requests)
        {
            bool result = true;
            Monitor.Enter(this.m_RequestsLock);
            for(int i=0; i<requests.Count; i++)
            {
                this.LogProcess(LogStates_Process.CreateRequest, hadler.ID, requests[i].ProcessID, DateTime.Now, requests[i], null);
            }
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

        protected virtual CQRouterData GetService(CQHttpRequest request)
        {
            //var vv = this.m_Service.FirstOrDefault(x => x.Urls.Any(y => y == request.URL.LocalPath) == true);
            var vv = this.m_Services.FirstOrDefault(x => x.Url == request.URL.LocalPath);
           
            return vv;
        }

        protected bool ProcessRequest(CQHttpRequest request, CQRouterData router, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            bool result = true;
            process_result_code = ServiceProcessResults.None;
            resp = null;

            if (router != null)
            {
                IQHttpService instance = null;
                switch(router.LifeType)
                {
                    case LifeTypes.Singleton:
                        {
                            instance = router.Service;
                        }
                        break;
                    case LifeTypes.Transient:
                        {
                            instance = Activator.CreateInstance(router.Service.GetType()) as IQHttpService;
                        }
                        break;
                }
                
                this.LogProcess(LogStates_Process.ProcessRequest, request.HandlerID, request.ProcessID, DateTime.Now, request, null);
                instance.Extension = this;
                instance.RegisterCacheManager();
                if (instance != null)
                {
                    object[] oos = new object[3];
                    oos[0] = request;
                    oos[1] = resp;
                    oos[2] = process_result_code;
                                        
                    router.Method.Invoke(instance, oos);
                    router.CurrentUse = router.CurrentUse - 1;
                    resp = oos[1] as CQHttpResponse;
                    process_result_code = (ServiceProcessResults)oos[2];
                }
                this.LogProcess(LogStates_Process.CreateResponse, request.HandlerID, request.ProcessID, DateTime.Now, request, resp);
            }
            else
            {
                resp = new CQHttpResponse(request.HandlerID, request.ProcessID);
                resp.Set404();
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
            string id = Guid.NewGuid().ToString();
            
            CQSocketListen listen = new CQSocketListen(data, id);
            //listen.OnListenState += Listen_OnListenState;
            listen.Logger = this;
            listen.OnNewClient += Listen_OnNewClient;
            this.LogAccept(LogStates_Accept.Create, "", 0, listen);
            listen.Open();
            this.m_AcceptSockets.Add(data, listen);
            return result;
        }

        //public delegate bool ListentStateChangeDelegate(CQSocketListen_Address listen_addres, ListenStates state);
        //public event ListentStateChangeDelegate OnListentStateChange;
        //private bool Listen_OnListenState(CQSocketListen listen)
        //{
        //    if(this.OnListentStateChange != null)
        //    {
        //        this.OnListentStateChange(listen.Addrss, listen.ListenState);
        //    }
        //    return true;
        //}

        public enum Request_ServiceStates
        {
            Request,
            Service_Begin,
            Service_End,
            Response,
            End
        }


        //public delegate bool ServiceChangeDelegate(CQHttpRequest req, IQHttpService service, Request_ServiceStates isadd);
        //public event ServiceChangeDelegate OnServiceChange;
        //private void ServiceChange(CQHttpRequest req, IQHttpService service, Request_ServiceStates isadd)
        //{
        //    if (this.OnServiceChange != null)
        //    {
        //        this.OnServiceChange(req, service, isadd);
        //    }
        //}

       
        List<CQRouterData> m_Services = new List<CQRouterData>();
        public bool Open(List<CQSocketListen_Address> address, List<IQHttpService> services, List<IQCacheIDProvider> cacheproviders, bool adddefault=true)
        {
            bool result = true;

            for (int i = 0; i < address.Count; i++)
            {
                this.OpenListen(address[i]);
            }
            for (int i = 0; i < services.Count; i++)
            {
                List<CQRouterData> rrs = CQRouterData.CreateRouterData(services[i]);
                if (rrs.Count > 0)
                {
                    this.m_Services.AddRange(rrs);
                }
            }

            if (adddefault == true)
            {
                CQHttpDefaultService ds = new CQHttpDefaultService();
                List<CQRouterData> rrs = CQRouterData.CreateRouterData(ds);
                if(rrs.Count > 0)
                {
                    this.m_Services.AddRange(rrs);
                }
                //var dnAttribute = ds.GetType().GetCustomAttributes(typeof(CQServiceSetting), true).FirstOrDefault();
                //if (dnAttribute != null)
                //{
                //    CQServiceSetting setting = dnAttribute as CQServiceSetting;
                //    CQRouterData rd = new CQRouterData();
                //    rd.Urls.AddRange(setting.Methods);
                //    rd.Service = ds.GetType();
                //    this.m_Service.Add(rd);


                //}
            }
            if(cacheproviders.Count == 0)
            {
                this.m_CacheIDProviders.Add("", new CQCacheID_Default());
                this.m_Caches.Add("", new Dictionary<string, IQCache>());
            }
            else
            {
                for (int i = 0; i < cacheproviders.Count; i++)
                {
                    this.m_CacheIDProviders.Add(cacheproviders[i].NickName, cacheproviders[i]);
                    this.m_Caches.Add(cacheproviders[i].NickName, new Dictionary<string, IQCache>());
                }
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

        virtual public bool CacheControl<T>(CacheOperates op, string id, ref T cache, string manager_id = "default") where T : CQCacheBase, new()
        {
            bool result = true;
            Monitor.Enter(this.m_CacheManagersLock);
            switch (op)
            {
                case CacheOperates.Get:
                    {
                        //CQCacheManager manager = null;
                        if((this.m_Caches.ContainsKey(manager_id) == true) && (this.m_Caches[manager_id].ContainsKey(id)==true))
                        {
                            cache = this.m_Caches[manager_id][id] as T;
                        }
                        else
                        {
                            result = false;
                        }
                        //if (this.m_CacheManagers.ContainsKey(manager_id) == true)
                        //{
                        //    manager = this.m_CacheManagers[manager_id];
                        //}
                        //if(manager != null)
                        //{
                        //    cache = manager.Get<T>(id, true);
                        //}
                    }
                    break;
                case CacheOperates.Create:
                    {
                        if (this.m_CacheIDProviders.ContainsKey(manager_id) == true)
                        {
                            cache = new T();
                            cache.ID = this.m_CacheIDProviders[manager_id].NextID();
                            this.m_Caches[manager_id].Add(cache.ID, cache);
                            //CQCacheManager manager = this.m_CacheManagers[manager_id];
                            //if(manager != null)
                            //{
                            //    cache = manager.Create<T>(id);
                            //    this.LogCache(LogStates_Cache.CreateCahce, DateTime.Now, manager_id, cache.ID, "");
                            //}
                            //else
                            //{
                            //    result = false;
                            //}
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    break;
                case CacheOperates.Destory:
                    {

                    }
                    break;
            }
            Monitor.Exit(this.m_CacheManagersLock);
            return result;
        }

        public bool LogProcess(LogStates_Process state, string handler_id, string process_id, DateTime time, CQHttpRequest request, CQHttpResponse response)
        {
            if(this.Logger != null)
            {
                this.Logger.LogProcess(state, handler_id, process_id, time, request, response);
            }
            //System.Diagnostics.Trace.WriteLine(string.Format("State:{0} Handler:{1} Process:{2} time:{3}"
            //    , state
            //    , handler_id
            //    , process_id
            //    , time.ToString("yyyy/MM/dd HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
            return true;
        }

        public bool LogCache(LogStates_Cache state, DateTime time, string manager_id, string cache_id, string name)
        {
            if (this.Logger != null)
            {
                this.Logger.LogCache(state, time,manager_id, cache_id, name);
            }
            //System.Diagnostics.Trace.WriteLine(string.Format("State:{0} Handler:{1} Process:{2} time:{3}"
            //   , state
            //   , id
            //   , name
            //   , time.ToString("yyyy/MM/dd HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo)));
            return true;
        }
        public bool LogAccept(LogStates_Accept state, string ip, int port, CQSocketListen obj)
        {
            if (this.Logger != null)
            {
                this.Logger.LogAccept(state, ip, port, obj);
            }
            return true;
        }

        Dictionary<string, IQCacheIDProvider> m_CacheIDProviders = new Dictionary<string, IQCacheIDProvider>();
        //public bool CacheIDControl(CacheIDProviderTypes op, string nickname, out string id, IQCacheIDProvider provider)
        //{
        //    bool result = true;
        //    id = "";
        //    switch(op)
        //    {
        //        case CacheIDProviderTypes.GetID:
        //            {
        //                if (this.m_CacheIDProviders.ContainsKey(nickname) == true)
        //                {
        //                    id = this.m_CacheIDProviders[nickname].NextID();
        //                }
        //            }
        //            break;
        //        case CacheIDProviderTypes.ResetID:
        //            {
        //                if (this.m_CacheIDProviders.ContainsKey(nickname) == true)
        //                {
        //                    this.m_CacheIDProviders[nickname].ResetID(id);
        //                }
        //            }
        //            break;
        //    }

        //    return result;
        //}

        //virtual public bool CacheManger_Registered<T>(string name = "default") where T : CQCacheManager, new()
        //{
        //    bool result = true;
        //    //Monitor.Enter(this.m_CacheManagersLock);
        //    //if (this.m_CacheManagers.ContainsKey(name) == false)
        //    //{
        //    //    T aa = new T();
        //    //    aa.Logger = this;
        //    //    this.m_CacheManagers.Add(name, aa);
        //    //    this.LogCache(LogStates_Cache.CreateManager, DateTime.Now, name, "", "");
        //    //}
        //    //Monitor.Exit(this.m_CacheManagersLock);
        //    return result;
        //}

        Dictionary<string, Dictionary<string, IQCache>> m_Caches = new Dictionary<string, Dictionary<string, IQCache>>();
        //public bool CacheManger_Registered(string name = "default")
        //{
        //    if(this.m_Caches.ContainsKey(name) == false)
        //    {
        //        this.m_Caches.Add(name, new Dictionary<string, IQCache>());
        //    }
            
        //    return true;
        //}
    }
}
