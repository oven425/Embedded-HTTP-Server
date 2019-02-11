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
using QNetwork;
using QNetwork.Http.Server.Log;
using WPF_Server_Http.Service;
using QNetwork.Http.Server.Protocol;
using QNetwork.Http.Server.Router;

namespace WPF_Server_Http
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    /// 
    [CQServiceRoot(LifeType = LifeTypes.Singleton, Root ="/web")]
    public partial class MainWindow : Window, IQHttpServer_Log,IQHttpService
    {
        CQHttpServer m_TestServer = new CQHttpServer();
        CQMainUI m_MainUI;

        public IQHttpServer_Log Logger { set; get; }
        public IQHttpServer_Extension Extension { set; get; }

        public MainWindow()
        {
            InitializeComponent();
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
                //this.m_TestServer.OnServiceChange += M_TestServer_OnServiceChange;
                //this.m_TestServer.OnHttpHandlerChange += M_TestServer_OnHttpHandlerChange;

                List<IQHttpService> services = new List<IQHttpService>();
                services.Add(new CQHttpService_Test());
                services.Add(new CQHttpService_Playback());
                services.Add(new CQHttpService_WebSocket());
                services.Add(new CQHttpService_ServerOperate());
                services.Add(new CQHttpService_WebMediaPlayer());
                services.Add(this);
                this.m_TestServer.Logger = this;

                List<IQCacheIDProvider> cache_providers = new List<IQCacheIDProvider>();
                cache_providers.Add(new CQCacheID_Default());
                this.m_TestServer.Open(this.m_MainUI.AddressList.Select(x => x.Address).ToList(), services, cache_providers, true);
            }
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

        object m_LogLock = new object();
        public bool LogProcess(LogStates_Process state, IQSession session, string handler_id, string process_id, DateTime time, CQHttpRequest request, CQHttpResponse response)
        {
            Monitor.Enter(this.m_LogLock);
            switch(state)
            {
                case LogStates_Process.CreateHandler:
                    {
                        CQProcess process = new CQProcess();
                        process.HandlerID = handler_id;
                        process.CreateHandler = time;
                        this.Dispatcher.Invoke(new Action(()=>
                        {
                            this.m_MainUI.ProcessList.Add(process);
                        }));
                    }
                    break;
                case LogStates_Process.CreateRequest:
                    {
                        CQProcess process = new CQProcess();
                        process.HandlerID = handler_id;
                        process.ProcessID = process_id;
                        process.Request = request.URL.LocalPath;
                        process.CreateRequest = time;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.m_MainUI.ProcessList.Add(process);
                        }));
                    }
                    break;
                case LogStates_Process.ProcessRequest:
                    {
                        var vv = this.m_MainUI.ProcessList.Where(x => x.ProcessID == process_id);
                        foreach (var oo in vv)
                        {
                            oo.ProcessRequest = time;
                        }
                    }
                    break;
                case LogStates_Process.CreateResponse:
                    {
                        var vv = this.m_MainUI.ProcessList.Where(x => x.ProcessID == process_id);
                        foreach (var oo in vv)
                        {
                            oo.CreateResponse = time;
                        }
                    }
                    break;
                case LogStates_Process.ProcessResponse:
                    {
                        var vv = this.m_MainUI.ProcessList.Where(x => x.ProcessID == process_id);
                        foreach (var oo in vv)
                        {
                            oo.ProcessResponse = time;
                        }
                    }
                    break;
                case LogStates_Process.SendResponse:
                    {
                        var vv = this.m_MainUI.ProcessList.Where(x => x.ProcessID == process_id);
                        foreach (var oo in vv)
                        {
                            oo.SendResponse = time;
                        }
                    }
                    break;
                case LogStates_Process.SendResponse_Compelete:
                    {
                        var vv = this.m_MainUI.ProcessList.Where(x => x.ProcessID == process_id);
                        foreach (var oo in vv)
                        {
                            oo.SendResponse_Compelete = time;
                        }
                    }
                    break;
                case LogStates_Process.DestoryRequest:
                    {

                    }
                    break;
                case LogStates_Process.DestoryResponse:
                    {

                    }
                    break;
                case LogStates_Process.DestoryHandler:
                    {
                        var vv = this.m_MainUI.ProcessList.Where(x => x.HandlerID == handler_id).ToList();
                        foreach (var oo in vv)
                        {
                            oo.DestoryHandler = time;
                        }
                    }
                    break;
            }
            Monitor.Exit(this.m_LogLock);
            return true;
        }

        public bool LogAccept(LogStates_Accept state, string ip, int port, CQSocketListen obj)
        {
            if(state == LogStates_Accept.Create)
            {

            }
            else
            {

            }
            var vv = this.m_MainUI.AddressList.Where(x => x.Address == obj.Addrss);
            foreach (var oo in vv)
            {
                oo.ListenState = state;
            }
            return true;
        }

        public bool LogCache(LogStates_Cache state, DateTime time, string manager_id, string cache_id, string name)
        {
            switch(state)
            {
                case LogStates_Cache.CreateManager:
                    {
                        CQCache cache = new CQCache();
                        cache.ManagerID = manager_id;
                        cache.Name = name;
                        cache.Begin = time;
                        cache.End = DateTime.MaxValue;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.m_MainUI.Caches.Add(cache);
                        }));
                    }
                    break;
                case LogStates_Cache.CreateCahce:
                    {
                        CQCache cache = new CQCache();
                        cache.ManagerID = manager_id;
                        cache.CacheID = cache_id;
                        cache.Name = name;
                        cache.Begin = time;
                        cache.End = DateTime.MaxValue;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.m_MainUI.Caches.Add(cache);
                        }));
                    }
                    break;
                case LogStates_Cache.DestoryCache:
                    {
                        var vv = this.m_MainUI.Caches.Where(x => x.CacheID == cache_id);
                        foreach (var oo in vv)
                        {
                            oo.End = time;
                        }
                    }
                    break;
                case LogStates_Cache.DestoryManager:
                    {
                        var vv = this.m_MainUI.Caches.Where(x => x.ManagerID == manager_id);
                        foreach(var oo in vv)
                        {
                            oo.End = time;
                        }
                    }
                    break;
            }
            return true;
        }

        [CQServiceMethod("/OperateAccept")]
        public bool OperateAccept(string handlerid, CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            process_result_code = 0;
            resp = null;
            bool result = true;
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string str = serializer.Serialize(this.m_MainUI.AddressList);
            process_result_code = ServiceProcessResults.OK;
            this.m_Count++;
            resp = new CQHttpResponse();
            resp.Set200();
            resp.BuildContentFromString(str);
            return result;
        }

        [CQServiceMethod("/GetAccepters", IsEnableCORS =true)]
        public bool GetAccepters(string handlerid, CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            process_result_code = 0;
            resp = null;
            bool result = true;
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string str = serializer.Serialize(this.m_MainUI.AddressList);
            process_result_code = ServiceProcessResults.OK;
            resp = new CQHttpResponse();
            resp.Set200();
            resp.BuildContentFromString(str);
            return result;
        }

        [CQServiceMethod("/GetRouters", IsEnableCORS = true)]
        public bool GetRouters(string handlerid, CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            process_result_code = 0;
            resp = null;
            bool result = true;
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                List<CQRouterData> routers;
                this.m_TestServer.GetRouters(out routers);


                string str = serializer.Serialize(routers.Select(x =>new { x.Url, x.LifeType, x.CurrentUse}));
                process_result_code = ServiceProcessResults.OK;
                resp = new CQHttpResponse();
                resp.Set200();
                resp.BuildContentFromString(str);
            }
            catch (Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
            }
            return result;
        }
        

        int m_Count = 0;
        [CQServiceMethod("/Count")]
        public bool TEST(string handlerid, CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            process_result_code = 0;
            resp = null;
            bool result = true;
            System.Threading.Thread.Sleep(5000);
            process_result_code = ServiceProcessResults.OK;
            this.m_Count++;
            resp = new CQHttpResponse();
            resp.Set200();
            resp.Connection = Connections.KeepAlive;
            string str = string.Format("Time:{0}\r\nCount:{1}"
                , DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)
                , this.m_Count);
            resp.BuildContentFromString(str);

            return result;
        }

        public bool RegisterCacheManager()
        {
            return true;
            //throw new NotImplementedException();
        }

        public bool CloseHandler(List<string> handlers)
        {
            return true;
            //throw new NotImplementedException();
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }

    
}
