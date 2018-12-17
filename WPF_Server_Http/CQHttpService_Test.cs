using QNetwork.Http.Server;
using QNetwork.Http.Server.Log;
using QNetwork.Http.Server.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace WPF_Server_Http
{
    public class CQHttpService_Test : IQHttpService
    {
        BackgroundWorker m_Thread_PushT;
        //int m_ID = 1;
        List<string> m_PushHandlers = new List<string>();
        List<string> m_NewPushHandlers = new List<string>();
        List<string> m_Methods = new List<string>();

        public IQHttpServer_Extension Extension { set; get; }

        public List<string> Methods => this.m_Methods;

        public IQHttpServer_Log Logger { set; get; }

        //static Dictionary<string, CQCacheBase> m_Caches;
        //static CQHttpService_Test()
        //{
        //    m_Caches = new Dictionary<string, CQCacheBase>();
        //}

        public CQHttpService_Test()
        {
            this.m_Thread_PushT = new BackgroundWorker();
            this.m_Thread_PushT.DoWork += new DoWorkEventHandler(m_Thread_PushT_DoWork);
            this.m_Methods.Add("/Push");
            this.m_Methods.Add("/EVENT4");
            this.m_Methods.Add("/TEST");
            this.m_Methods.Add("/TEST1");
            this.m_Methods.Add("/PostTest");
        }

        void m_Thread_PushT_DoWork(object sender, DoWorkEventArgs e)
        {
            //int fileindex = 0;
            List<byte[]> files = new List<byte[]>();
            files.Add(File.ReadAllBytes("../WebTest/Image/RR.jpg"));
            files.Add(File.ReadAllBytes("../WebTest/Image/GG.jpg"));
            files.Add(File.ReadAllBytes("../WebTest/Image/BB.jpg"));
            List<CQHttpResponse> resps = new List<CQHttpResponse>();
            while (true)
            {
                resps.Clear();
                for (int i = 0; i < this.m_NewPushHandlers.Count; i++)
                {
                    CQHttpResponse resp = new CQHttpResponse(this.m_NewPushHandlers[i], "");
                    resp.Set200();
                    resp.Connection = Connections.KeepAlive;
                    resp.ContentType = "multipart/x-mixed-replace;boundary=\"QQQ\"";
                    resp.ContentLength = -1;
                    resp.Headers.Add("Cache-Control", "no-cache");
                    resp.Headers.Add("Pragma", "no-cache");
                    resp.Headers.Add("Expires", "0");

                    resps.Add(resp);
                    this.m_PushHandlers.Add(this.m_NewPushHandlers[i]);
                }
                this.m_NewPushHandlers.Clear();
                for (int i = 0; i < this.m_PushHandlers.Count; i++)
                {
                    CQHttpResponse resp = new CQHttpResponse(this.m_PushHandlers[i], "", CQHttpResponse.BuildTypes.MultiPart);
                    resp.Content = new MemoryStream();

                    string str = string.Format("PushTest {0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo));
                    byte[] buf = Encoding.ASCII.GetBytes(str);

                    resp.Content.Write(buf, 0, buf.Length);
                    resp.ContentType = "text/plain";
                    resp.ContentLength = buf.Length;

                    //resp.Content.Write(files[fileindex], 0, files[fileindex].Length);
                    //resp.ContentType = "image/jpeg";
                    //resp.ContentLength = files[fileindex].Length;
                    //fileindex = fileindex + 1;
                    //if (fileindex >= files.Count)
                    //{
                    //    fileindex = 0;
                    //}


                    resp.Content.Position = 0;
                    resps.Add(resp);

                }
                if (resps.Count > 0)
                {
                    this.Extension.SendMultiPart(resps);
                }
                System.Threading.Thread.Sleep(1000);
                if (this.m_PushHandlers.Count == 0)
                {
                    break;
                }
            }
        }

        public bool Process(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            process_result_code = 0;
            resp = null;
            bool result = true;
            switch (req.URL.LocalPath)
            {
                case "/PostTest":
                    {
                        process_result_code = ServiceProcessResults.OK;
                        resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
                        resp.Content = new MemoryStream();
                        byte[] bb = new byte[8192];
                        while (true)
                        {
                            int read_len = req.Content.Read(bb, 0, bb.Length);

                            resp.Content.Write(bb, 0, read_len);
                            if (read_len != bb.Length)
                            {
                                break;
                            }
                        }
                        resp.ContentLength = resp.Content.Length;
                        resp.Content.Position = 0;
                        resp.Connection = Connections.KeepAlive;
                        resp.Set200();
                    }
                    break;
                case "/Push":
                    {
                        this.m_NewPushHandlers.Add(req.HandlerID);
                        if (this.m_Thread_PushT.IsBusy == false)
                        {
                            this.m_Thread_PushT.RunWorkerAsync();
                        }

                        process_result_code = ServiceProcessResults.PassToPushService;

                    }
                    break;
                case "/EVENT4":
                    {
                        process_result_code = ServiceProcessResults.OK;
                        resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
                        resp.Set200();
                        resp.Connection = Connections.Close;
                    }
                    break;
                case "/TEST1":
                    {
                        process_result_code = ServiceProcessResults.OK;
                        CQCache1 cc = null;
                        Dictionary<string, string> param;
                        CQHttpRequest.Parse(req.URL.Query, out param);
                        if (param.ContainsKey("ID") == true)
                        {
                            this.Extension.CacheControl(CacheOperates.Get, param["ID"], ref cc, "Test1");
                        }
                        else
                        {
                            this.Extension.CacheControl(CacheOperates.Get, "", ref cc, "Test1");
                        }
                        if (cc == null)
                        {
                            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
                            resp.Set200();
                            resp.Connection = Connections.KeepAlive;
                            string str = string.Format("Time:{0}\r\nID:{1} not exist"
                                , DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)
                                , param["ID"]);
                            resp.BuildContentFromString(str);
                        }
                        else
                        {
                            cc.Count++;
                            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
                            resp.Set200();
                            resp.Connection = Connections.KeepAlive;
                            string str = string.Format("Time:{0}\r\nID:{1}\r\nCount:{2}"
                                , DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)
                                , cc.ID
                                , cc.Count);
                            resp.BuildContentFromString(str);
                        }
                    }
                    break;
                case "/TEST":
                    {
                        process_result_code = ServiceProcessResults.OK;
                        CQCache1 cc = null;
                        Dictionary<string, string> param;
                        CQHttpRequest.Parse(req.URL.Query, out param);
                        if (param.ContainsKey("ID") == true)
                        {
                            this.Extension.CacheControl(CacheOperates.Get, param["ID"], ref cc, "Test1");
                        }
                        else
                        {
                            this.Extension.CacheControl(CacheOperates.Create, "", ref cc, "Test1");
                        }

                        if (cc == null)
                        {
                            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
                            resp.Set200();
                            resp.Connection = Connections.KeepAlive;
                            string str = string.Format("Time:{0}\r\nID:{1} not exist"
                                , DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)
                                , param["ID"]);
                            resp.BuildContentFromString(str);
                        }
                        else
                        {
                            cc.Count++;
                            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
                            resp.Set200();
                            resp.Connection = Connections.KeepAlive;
                            string str = string.Format("Time:{0}\r\nID:{1}\r\nCount:{2}"
                                , DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)
                                , cc.ID
                                , cc.Count);
                            resp.BuildContentFromString(str);
                        }
                    }
                    break;
            }
            return result;
        }

        public bool TimeOut_Cache()
        {
            bool result = true;
            //Monitor.Enter(this.m_CachesLock);
            //List<string> keys = new List<string>();
            //for (int i = 0; i < this.m_Caches.Count; i++)
            //{
            //    if (this.m_Caches.ElementAt(i).Value.IsTimeOut(TimeSpan.FromSeconds(20)) == true)
            //    {
            //        keys.Add(this.m_Caches.ElementAt(i).Key);
            //    }
            //}
            //foreach (string key in keys)
            //{
            //    this.m_Caches.Remove(key);
            //}
            //Monitor.Exit(this.m_CachesLock);
            return result;
        }

        public bool CloseHandler(List<string> handlers)
        {
            for (int i = 0; i < handlers.Count; i++)
            {
                this.m_NewPushHandlers.RemoveAll(x => x == handlers[i]);
                this.m_PushHandlers.RemoveAll(x => x == handlers[i]);
            }
            return true;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool RegisterCacheManager()
        {
            bool result = true;
            this.Extension.CacheManger_Registered<CQCacheManager_Test1>("Test1");
            return result;
        }
    }
}
