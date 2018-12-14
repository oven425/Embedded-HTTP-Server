﻿using System;
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

namespace WPF_Server_Http
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window, IQHttpServer_Log
    {
        CQHttpServer m_TestServer = new CQHttpServer();
        CQMainUI m_MainUI;
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
                services.Add(new CQHttpService_Test());
                this.m_TestServer.Logger = this;
                this.m_TestServer.Open(this.m_MainUI.AddressList.Select(x => x.Address).ToList(), services, true);
            }
        }

        //Dictionary<CQSocketListen_Address, List<CQHandlerData>> m_Handler = new Dictionary<CQSocketListen_Address, List<CQHandlerData>>();

        //private bool M_TestServer_OnHttpHandlerChange(CQHttpHandler handler, bool isadd)
        //{
        //    if (isadd == true)
        //    {
        //        var vv = this.m_MainUI.AddressList.FirstOrDefault(x => x.Address == handler.Accept_Address);
        //        if(vv!=null)
        //        {
        //            this.Dispatcher.Invoke(new Action(() =>
        //            {
        //                vv.Handlers.Add(new CQHandlerData() { Handler = handler });
        //            }));
                    
        //        }
        //    }
        //    else
        //    {
        //        var vv = this.m_MainUI.AddressList.FirstOrDefault(x => x.Address == handler.Accept_Address);
        //        if (vv != null)
        //        {
        //            var hh = vv.Handlers.FirstOrDefault(x => x.Handler == handler);
        //            hh.End = DateTime.Now;
        //        }
        //    }
        //    return true;
        //}

        //private bool M_TestServer_OnServiceChange(CQHttpRequest req, IQHttpService service, Request_ServiceStates isadd)
        //{
        //    this.Dispatcher.Invoke(new Action(() =>
        //    {
        //        switch (isadd)
        //        {
        //            case Request_ServiceStates.Request:
        //                {
        //                    this.m_MainUI.Request_Services.Add(new CQRequest_Service() { Request = req });
        //                }
        //                break;
        //            case Request_ServiceStates.Service_Begin:
        //                {
        //                    var vv = this.m_MainUI.Request_Services.FirstOrDefault(x => x.Request == req);
        //                    if (vv != null)
        //                    {
        //                        vv.Service = service;
        //                    }
        //                }
        //                break;
        //            case Request_ServiceStates.Response:
        //                {

        //                }
        //                break;
        //            case Request_ServiceStates.End:
        //                {

        //                }
        //                break;
        //        }

        //    }));
        //    return true;
        //}

        //private bool M_TestServer_OnListentStateChange(CQSocketListen_Address listen_addres, ListenStates state)
        //{
        //    var vv = this.m_MainUI.AddressList.Where(x => x.Address == listen_addres);
        //    foreach (var oo in vv)
        //    {
        //        oo.ListenState = state;
        //    }
        //    return true;
        //}

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

        public bool LogProcess(LogStates_Process state, string handler_id, string process_id, DateTime time, CQHttpRequest request, CQHttpResponse response)
        {
            return true;
            //throw new NotImplementedException();
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
                        System.Diagnostics.Trace.WriteLine("");
                    }
                    break;
                case LogStates_Cache.CreateCahce:
                    {
                        System.Diagnostics.Trace.WriteLine("");
                    }
                    break;
                case LogStates_Cache.DestoryCache:
                    {
                        System.Diagnostics.Trace.WriteLine("");
                    }
                    break;
                case LogStates_Cache.DestoryManager:
                    {
                        System.Diagnostics.Trace.WriteLine("");
                    }
                    break;
            }
            return true;
        }
    }

    
}
