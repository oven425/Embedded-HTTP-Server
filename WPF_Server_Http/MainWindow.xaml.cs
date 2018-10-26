using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;
using QNetwork.Http.Server;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using WPF_Server_Http.UIData;
using System.Web.Script.Serialization;
using QNetwork.Http.Server.Accept;
using QNetwork.Http.Server.Service;
using WPF_Server_Http.Define;
using static QNetwork.Http.Server.CQHttpServer;
using QNetwork.Http.Server.Cache;

namespace WPF_Server_Http
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        CQHttpServer m_TestServer = new CQHttpServer();
        CQMainUI m_MainUI;
        public MainWindow()
        {
            InitializeComponent();
            //CQCache1 bb = this.Create<CQCache1>();


        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //JavaScriptSerializer js = new JavaScriptSerializer();
            //string str = js.Serialize(new CQAA());
            if (this.m_MainUI == null)
            {
                this.DataContext = this.m_MainUI = new CQMainUI();
                // 取得本機名稱
                string strHostName = Dns.GetHostName();
                // 取得本機的IpHostEntry類別實體，用這個會提示已過時
                //IPHostEntry iphostentry = Dns.GetHostByName(strHostName);

                // 取得本機的IpHostEntry類別實體，MSDN建議新的用法
                IPHostEntry iphostentry = Dns.GetHostEntry(strHostName);

                // 取得所有 IP 位址
                foreach (IPAddress ipaddress in iphostentry.AddressList)
                {
                    // 只取得IP V4的Address
                    if (ipaddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        CQSocketListen_Address net_address = new CQSocketListen_Address() { IP = ipaddress.ToString(), Port = 3333 };
                        this.m_MainUI.AddressList.Add(new CQListenAddress() { Address = net_address });
                    }
                }
                if (this.m_MainUI.AddressList.Any(x => x.Address.ToEndPint().ToString() == "127.0.0.1") == false)
                {
                    CQSocketListen_Address net_address = new CQSocketListen_Address() { IP = "127.0.0.1", Port = 3333 };
                    this.m_MainUI.AddressList.Add(new CQListenAddress() { Address = net_address });
                }
                this.m_TestServer.OnListentStateChange += M_TestServer_OnListentStateChange;
                this.m_TestServer.OnServiceChange += M_TestServer_OnServiceChange;
                //this.m_TestServer.Open(this.m_MainUI.AddressList.Select(x=>x.Address).ToList(), new List<IQHttpService>() { new CQHttpService_Test(), new CQHttpService_Playback(),new CQHttpService_WebSocket() } , true);
                this.m_TestServer.Open(this.m_MainUI.AddressList.Select(x => x.Address).ToList(), new List<IQHttpService>() { new CQHttpService_Test(), new CQHttpService_Playback() }, true);
            }
        }

        private bool M_TestServer_OnServiceChange(CQHttpRequest req, IQHttpService service, Request_ServiceStates isadd)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                switch (isadd)
                {
                    case Request_ServiceStates.Request:
                        {
                            this.m_MainUI.Request_Services.Add(new CQRequest_Service() { Request = req });
                        }
                        break;
                    case Request_ServiceStates.Service_Begin:
                        {
                            var vv = this.m_MainUI.Request_Services.FirstOrDefault(x => x.Request == req);
                            if (vv != null)
                            {
                                vv.Service = service;
                            }
                        }
                        break;
                    case Request_ServiceStates.Response:
                        {

                        }
                        break;
                    case Request_ServiceStates.End:
                        {

                        }
                        break;
                }

            }));
            return true;
        }

        private bool M_TestServer_OnListentStateChange(CQSocketListen_Address listen_addres, ListenStates state)
        {
            var vv = this.m_MainUI.AddressList.Where(x => x.Address == listen_addres);
            foreach (var oo in vv)
            {
                oo.ListenState = state;
            }
            return true;
        }

        private void button_add_listen_Click(object sender, RoutedEventArgs e)
        {
            CQSocketListen_Address ssd = new CQSocketListen_Address() { IP = this.m_MainUI.Listen_IP, Port = this.m_MainUI.Listen_Port };
            CQListenAddress address = new CQListenAddress() { Address = ssd };
            this.m_TestServer.OpenListen(ssd);
        }

        private void checkbox_listen_control_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            CQListenAddress address = checkbox.DataContext as CQListenAddress;
            if (checkbox != null)
            {
                if (address.IsOpen == true)
                {
                    this.m_TestServer.OpenListen(address.Address);
                }
                else
                {
                    this.m_TestServer.CloseListen(address.Address);

                }
            }
        }
    }

    //public class CQHttpService_WebSocket : IQHttpService
    //{
    //    public IQHttpServer_Extension Extension { set; get; }
    //    List<string> m_Methods = new List<string>();
    //    public List<string> Methods => this.m_Methods;
    //    public CQHttpService_WebSocket()
    //    {
    //        this.m_Methods.Add("/WEBSOCKET");
    //        this.m_Methods.Add("/WEBSOCKET_TEST");
    //    }
    //    public bool CloseHandler(List<string> handlers)
    //    {
    //        return true;
    //    }

    //    public void Dispose()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool Process(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code, out CQCacheBase cache)
    //    {
    //        cache = null;
    //        resp = null;
    //        process_result_code = 0;
    //        bool result = true;
    //        switch (req.URL.LocalPath.ToUpperInvariant())
    //        {
    //            case "/WEBSOCKET":
    //                {
    //                    resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
    //                    resp.Content = new FileStream("../WebTest/Websocket.html", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
    //                    resp.ContentLength = resp.Content.Length;
    //                    resp.ContentType = "text/html; charset=utf-8";
    //                    process_result_code = ServiceProcessResults.OK;
    //                    resp.Set200();
    //                }
    //                break;
    //            case "/WEBSOCKET_TEST":
    //                {
    //                    //[4] = {[Upgrade, websocket]}
    //                    if ((req.Headers.ContainsKey("UPGRADE") == true) && (req.Headers["UPGRADE"] == "websocket"))
    //                    {
    //                        CQWebSocket websocket = new CQWebSocket();
    //                        CQTCPHandler handler;
    //                        this.Extension.ControlTransfer(req.HandlerID, out handler);
    //                        websocket.Open(handler, req.HeaderRaw, req.HeaderRaw.Length);
    //                    }
    //                }
    //                break;

    //        }

    //        return result;
    //    }

    //    public bool TimeOut_Cache()
    //    {
    //        return true;
    //    }

    //    public bool Process_Cache(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
    //    {
    //        bool result = true;
    //        process_result_code = ServiceProcessResults.None;
    //        resp = null;
    //        switch(req.URL.LocalPath.ToUpperInvariant())
    //        {
    //            //case "/WEBSOCKET_TEST":
    //            //    {
    //            //        //[4] = {[Upgrade, websocket]}
    //            //        if ((req.Headers.ContainsKey("UPGRADE") == true) && (req.Headers["UPGRADE"] == "websocket"))
    //            //        {
    //            //            CQWebSocket websocket = new CQWebSocket();
    //            //            CQTCPHandler handler;
    //            //            this.Extension.ControlTransfer(req.HandlerID, out handler);
    //            //            websocket.Open(handler, req.HeaderRaw, req.HeaderRaw.Length);
    //            //        }
    //            //    }
    //            //    break;
    //        }
    //        return result;
    //    }
    //}

    public class CQHttpService_Test : IQHttpService
    {
        BackgroundWorker m_Thread_PushT;
        //int m_ID = 1;
        List<string> m_PushHandlers = new List<string>();
        List<string> m_NewPushHandlers = new List<string>();
        List<string> m_Methods = new List<string>();

        public IQHttpServer_Extension Extension { set; get; }

        public List<string> Methods => this.m_Methods;
        //static Dictionary<string, CQCacheBase> m_Caches;
        //static CQHttpService_Test()
        //{
        //    m_Caches = new Dictionary<string, CQCacheBase>();
        //}

        public CQHttpService_Test()
        {
            this.m_Thread_PushT = new BackgroundWorker();
            this.m_Thread_PushT.DoWork += new DoWorkEventHandler(m_Thread_PushT_DoWork);
            this.m_Methods.Add("/PUSH");
            this.m_Methods.Add("/EVENT4");
            this.m_Methods.Add("/TEST");
            this.m_Methods.Add("/TEST1");
        }

        void m_Thread_PushT_DoWork(object sender, DoWorkEventArgs e)
        {
            int fileindex = 0;
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

                    //string str = string.Format("PushTest {0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo));
                    //byte[] buf = Encoding.ASCII.GetBytes(str);

                    //resp.Content.Write(buf, 0, buf.Length);
                    ////byte[] bb1 = Encoding.UTF8.GetBytes("\r\n\r\n");
                    ////resp.Content.Write(bb1, 0, bb1.Length);
                    //resp.ContentType = "text/plain";
                    //resp.ContentLength = -1;

                    resp.Content.Write(files[fileindex], 0, files[fileindex].Length);
                    resp.ContentType = "image/jpeg";
                    resp.ContentLength = files[fileindex].Length;
                    fileindex = fileindex + 1;
                    if (fileindex >= files.Count)
                    {
                        fileindex = 0;
                    }


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
            switch (req.URL.LocalPath.ToUpperInvariant())
            {
                case "/PUSHT":
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
                            this.Extension.CacheControl(CacheOperates.Get, param["ID"], ref cc, false, "Test1");
                        }
                        else
                        {
                            this.Extension.CacheControl(CacheOperates.Get, "", ref cc, true, "Test1");
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
                            this.Extension.CacheControl(CacheOperates.Get, param["ID"], ref cc, false);
                        }
                        else
                        {
                            this.Extension.CacheControl(CacheOperates.Get, "", ref cc);
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
