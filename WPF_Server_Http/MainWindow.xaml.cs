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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
                this.m_TestServer.OnListentStateChange += M_TestServer_OnListentStateChange;
                this.m_TestServer.Open(this.m_MainUI.AddressList.Select(x=>x.Address).ToList(), new List<CQHttpService>() { new CQHttpService_Test(), new CQHttpService_Playback() } , true);
            }
        }

        private bool M_TestServer_OnListentStateChange(CQSocketListen_Address listen_addres, ListenStates state)
        {
            var vv = this.m_MainUI.AddressList.Where(x => x.Address == listen_addres);
            foreach(var oo in vv)
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
                if(address.IsOpen == true)
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

    public class CQRecordPlaybackT : IQCacheData
    {
        bool m_IsEnd = false;
        string m_ID;
        CQTCPHandler m_TCPHandler;
        public CQRecordPlaybackT(CQTCPHandler tcp_handler, string id)
        {
            this.m_ID = id;
            this.m_TCPHandler = tcp_handler;
            this.m_TCPHandler.OnParse += M_TCPHandler_OnParse;
        }

        private bool M_TCPHandler_OnParse(Stream data)
        {

            return true;
            //throw new NotImplementedException();
        }

        public string ID { get { return this.m_ID; } }

        public bool IsTimeOut(TimeSpan timeout)
        {
            return this.m_IsEnd;
        }

        public bool Control(string cmd)
        {
            bool result = true;

            return result;
        }

        public bool Open()
        {
            bool result = true;
            string str = string.Format("CQRecordPlaybackT {0} {1}", this.ID, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo));
            byte[] buf = Encoding.ASCII.GetBytes(str);
            CQHttpResponse resp = new CQHttpResponse("");
            resp.Content = new MemoryStream();
            resp.Content.Write(buf, 0, buf.Length);
            resp.Content.Position = 0;
            resp.ContentLength = buf.Length;
            resp.ContentType = "text/plain";
            //string str_header = resp.ToString();
            //this.m_Socket.Send(Encoding.UTF8.GetBytes(str_header));
            //this.m_Socket.Send(buf);
            //this.m_Socket.Close();
            CQHttpResponseReader resp_reader = new CQHttpResponseReader();
            resp_reader.Set(resp);
            this.m_TCPHandler.AddSend(resp_reader);
            //this.m_IsEnd = true;
            return result;
        }

        public object Data { set; get; }
    }

    public class CQHttpService_Playback : CQHttpService
    {
        int m_SessionID = 0;
        public override bool Process(CQHttpRequest req, out CQHttpResponse resp, out int process_result_code)
        {
            bool result = true;
            process_result_code = 0;
            resp = null;
            switch (req.URL.LocalPath.ToUpperInvariant())
            {
                case "/PLAYBACK":
                    {
                        string query_str = req.URL.Query;
                        if(string.IsNullOrEmpty(query_str) == true)
                        {
                            process_result_code = (int)ServiceProcessResults.ControlTransfer;
                            CQTCPHandler tcp;
                            this.Extension.ControlTransfer(req.HandlerID, out tcp);
                            CQRecordPlaybackT tt = new CQRecordPlaybackT(tcp, (++this.m_SessionID).ToString());
                            this.m_Caches.Add(tt.ID, tt);
                            tt.Open();
                        }
                        else
                        {
                            query_str = query_str.Remove(0, 1);
                            if (this.m_Caches.ContainsKey(query_str) == true)
                            {
                                CQRecordPlaybackT ppt = this.m_Caches[query_str] as CQRecordPlaybackT;
                                ppt.Control(query_str);
                            }
                        }
                    }
                    break;
            }

            return result;
        }
    }

    //public class CQHttpService_WebSocket : CQHttpService
    //{
    //    public override IQHttpServer_ HttpServer_ { set; get; }
    //    public override bool Process(CQHttpRequest req, out CQHttpResponse resp, out int process_result_code)
    //    {
    //        resp = null;
    //        process_result_code = 0;
    //        bool result = true;
    //        switch(req.URL.LocalPath.ToUpperInvariant())
    //        {
    //            //case "/WEBSOCKET":
    //            //    {
    //            //        resp = new CQHttpResponse(req.HandlerID);
    //            //        resp.Content = new FileStream("Websocket.html", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
    //            //        resp.ContentLength = resp.Content.Length;
    //            //        resp.ContentType = "text/html; charset=utf-8";
    //            //        process_result_code = 1;
    //            //        resp.Set200();
    //            //    }
    //            //    break;
    //            case "/WEBSOCKET_TEST":
    //                {
    //                    //[4] = {[Upgrade, websocket]}
    //                    if ((req.Headers.ContainsKey("Upgrade") == true) && (req.Headers["Upgrade"] == "websocket"))
    //                    {
    //                        CQWebSocket websocket = new CQWebSocket();
    //                        Socket socket;
    //                        this.SendControlTransfer(req.HandlerID, out socket);
    //                        websocket.Open(socket, req.HeaderRaw, req.HeaderRaw.Length);
    //                        //SHA1 sha1 = new SHA1CryptoServiceProvider();//建立一個SHA1
    //                        //byte[] source = Encoding.Default.GetBytes("w4v7O6xFTi36lq3RNcgctw=="+ "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");//將字串轉為Byte[]
    //                        //byte[] crypto = sha1.ComputeHash(source);//進行SHA1加密
    //                        //string result = Convert.ToBase64String(crypto);//把加密後的字串從Byte[]轉為字串
    //                    }
    //                }
    //                break;
    //        }

    //        return result;
    //    }
    //}

    public class CQHttpService_Test : CQHttpService
    {
        BackgroundWorker m_Thread_PushT;
        int m_ID = 1;
        List<string> m_PushHandlers = new List<string>();
        List<string> m_NewPushHandlers = new List<string>();
        public CQHttpService_Test()
        {
            this.m_Thread_PushT = new BackgroundWorker();
            this.m_Thread_PushT.DoWork += new DoWorkEventHandler(m_Thread_PushT_DoWork);
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
                    CQHttpResponse resp = new CQHttpResponse(this.m_NewPushHandlers[i]);
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
                    CQHttpResponse resp = new CQHttpResponse(this.m_PushHandlers[i], CQHttpResponse.BuildTypes.MultiPart);
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

        public override bool Process(CQHttpRequest req, out CQHttpResponse resp, out int process_result_code)
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

                        process_result_code = (int)ServiceProcessResults.PassToPushService;

                    }
                    break;
                case "/EVENT4":
                    {
                        process_result_code = 1;
                        resp = new CQHttpResponse(req.HandlerID);
                        resp.Set200();
                        resp.Connection = Connections.Close;
                    }
                    break;
                case "/TEST":
                    {
                        process_result_code = 1;
                        CQTestData cc = null;
                        CQCacheData cache = null;
                        string query_str = "";
                        if (string.IsNullOrEmpty(req.URL.Query) == false)
                        {
                            query_str = req.URL.Query.Remove(0, 1);
                        }
                        if (this.m_Caches.ContainsKey(query_str) == true)
                        {
                            cache = this.m_Caches[query_str] as CQCacheData;
                            cc = cache.Data as CQTestData;
                        }
                        else
                        {
                            string id = this.m_ID.ToString();
                            this.m_ID++;
                            cache = new CQCacheData(id);
                            cache.Data = cc = new CQTestData();
                            this.m_Caches.Add(cache.ID, cache);
                        }
                        cc.Count++;
                        resp = new CQHttpResponse(req.HandlerID);
                        resp.Set200();
                        resp.Connection = Connections.KeepAlive;
                        string str = string.Format("ID:{0}\r\nCount:{1}", cache.ID, cc.Count);
                        byte[] str_buf = Encoding.ASCII.GetBytes(str);
                        resp.Content = new MemoryStream();
                        resp.Content.Write(str_buf, 0, str_buf.Length);
                        resp.Content.Position = 0;
                        resp.ContentLength = resp.Content.Length;
                    }
                    break;
            }
            return result;
        }

        public class CQTestData
        {
            public int Count { set; get; }
        }

        public override bool TimeOut_Cache()
        {
            bool result = true;
            Monitor.Enter(this.m_CachesLock);
            List<string> keys = new List<string>();
            for (int i = 0; i < this.m_Caches.Count; i++)
            {
                if (this.m_Caches.ElementAt(i).Value.IsTimeOut(TimeSpan.FromSeconds(20)) == true)
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

        public override bool CloseHandler(List<string> handlers)
        {
            for (int i = 0; i < handlers.Count; i++)
            {
                this.m_NewPushHandlers.RemoveAll(x => x == handlers[i]);
                this.m_PushHandlers.RemoveAll(x => x == handlers[i]);
            }
            return true;
        }
    }

    
}
