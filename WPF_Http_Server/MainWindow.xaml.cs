﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QSoft.Server.Http;
using QSoft.Server.Http.Extension;
using System.Linq.Expressions;
using QQTest;
using System.Web.Script.Serialization;
using System.ComponentModel;
using System.Reflection;
using QSoft.Server.Http1;
using System.Dynamic;

namespace WPF_Http_Server
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        [HttpMethodSetting(Method = "POST")]
        async public Task<object> Setting(string ip, int port, HttpListenerContext context)
        {
            
            await Task.Delay(1000);
            return new
            {
                ip = ip,
                port = port,
                time = DateTime.Now,
                name = "object Setting(string ip, int port, HttpListenerContext context)"
            };
        }
        [HttpMethodSetting(Method = "POST")]
        public object Setting(string ip, HttpListenerContext context)
        {
            return new
            {
                ip = ip,
                time = DateTime.Now,
                name = "object Setting(string ip, HttpListenerContext context)"
            };
        }

        [HttpMethodSetting(Method = "POST")]
        public object Setting(int port, HttpListenerContext context)
        {
            return new
            {
                port = port,
                time = DateTime.Now,
                name = "object Setting(int port, HttpListenerContext context)"
            };
        }

        [HttpMethodSetting(Method = "POST")]
        public object Setting(Address port)
        {
            return new
            {
                port = port,
                time = DateTime.Now,
                name = "object Setting(Address port)"
            };
        }

        MainUI m_MainUI;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Address add = new Address() { IP = "789.456.123.1", Port=999999 };
            System.Xml.Serialization.XmlSerializer xml = new System.Xml.Serialization.XmlSerializer(add.GetType());
            using (MemoryStream mm = new MemoryStream())
            {
                xml.Serialize(mm, add);
                System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(mm.ToArray()));
            }
            JavaScriptSerializer js = new JavaScriptSerializer();
            int x = 10;
            string aaa = js.Serialize(add);
            List<string> names1 = new List<string>() { "A", "B" };
            List<string> names2 = new List<string>() { "A", "B", "C" };
            var excp = names1.Except(names2);
            Server1 server1 = new Server1();
            server1.Strat("127.0.0.1", 3456, new List<object>() { this }, new DirectoryInfo("../../webdata/"));
            return;
            //CreateInstallCert(100, "AA", "");
            DirectoryInfo dir = new DirectoryInfo("../../webdata/");
            string pp = dir.FullName.Replace(dir.Parent.FullName, "").Trim('\\');
            if (this.m_MainUI == null)
            {
                this.DataContext = this.m_MainUI = new MainUI();
            }
            RowData rd = new RowData();
            rd.Index = 1;
            rd.Name = "BBen";

            
            string json_str = js.Serialize(rd);
            System.Diagnostics.Trace.WriteLine(json_str);

            //MemoryStream mm = new MemoryStream();
            //mm.Dispose();
            //if(mm.Length is ObjectDisposedException)
            //{

            //}
            Server server = new Server();
            try
            {
                //server.Get<RowData>("/get/json", async (context, data) =>
                //{
                //    await Task.Delay(1);
                //    return Result.Json(DateTime.Now);
                //});

                ////server.Get<RowData>("/get/json", (context, data) => Get_Json(context, data));
                //server.Get<RowData>("/get/xml", (context, data) => Get_xml(context, data));

                //server.Get("/get/jpg", (context, query) =>
                //{
                //    return Result.Stream(File.OpenRead("../../1.jpg"));
                //});

                //server.Get("/events.html", (context, data) =>
                //{
                //    return Result.Stream(File.OpenRead($"{server.Statics.FullName}events.html"));
                //});

                //server.Get<RowData>("/sse/test", (context, data) =>
                //{
                //    //context.Response.ContentType = "text/event-stream";
                //    //context.Response.Headers["Connection"] = "keep-alive";
                //    //context.Response.Headers["Cache-Control"] = "no-cache";
                //    //m_Events.Add( context.Response);

                //    //Task.Run(async () =>
                //    //{
                //    //    while (true)
                //    //    {
                //    //        for (int i = 0; i < m_Events.Count; i++)
                //    //        {
                //    //            string msg = $"id: 123\ndata: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\n\n";

                //    //            m_Events.ElementAt(i).Write(msg, false);
                //    //        }
                //    //        await Task.Delay(1000);
                //    //    }
                //    //});
                //    this.m_Events.Add(new ServerSentEvent(context.Response, DateTime.Now.ToString("HHmmssfff")));
                //    Task.Run(async () =>
                //    {
                //        while (true)
                //        {
                //            for (int i = 0; i < m_Events.Count; i++)
                //            {
                //                string msg = $"id: 123\ndata: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\n\n";

                //                this.m_Events.ElementAt(i).WriteMessage(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                //            }
                //            await Task.Delay(1000);
                //        }
                //    });
                //    return Result.Hanlded;
                //});
                //server.Get<RowData>("/get/mjpg", (context, data) =>
                //{
                //    this.m_MultiParts.Add(new MultiPatStream(context.Response));
                //    while (true)
                //    {
                //        foreach(var oo in this.m_MultiParts)
                //        {
                //            for (int i = 1; i <= 3; i++)
                //            {
                //                try
                //                {
                //                    FileStream fs = File.OpenRead($"../../{i}.jpg");
                //                    oo.Write(fs, "image/jpeg");
                //                    //context.Response.Write("--myboundary\r\n", false);
                //                    //context.Response.Write("Content-Type:image/jpeg\r\n", false);
                //                    //context.Response.Write($"Content-Length:{fs.Length}\r\n\r\n", false);
                //                    ////data.Response.OutputStream.Write(jpg, 0, jpg.Length);
                //                    //context.Response.Write(fs, false);
                //                    //fs.Close();
                //                    //fs.Dispose();
                //                    //context.Response.Write("\r\n", false);
                //                }
                //                catch (Exception ee)
                //                {
                //                    System.Diagnostics.Trace.WriteLine(ee.Message);
                //                }
                                
                //                System.Threading.Thread.Sleep(1000);
                //            }
                //        }
                        
                //    }
                //    //context.Response.ContentType = "multipart/x-mixed-replace;boundary=--myboundary";
                //    //while (true)
                //    //{
                //    //    for (int i = 1; i <= 3; i++)
                //    //    {
                //    //        FileStream fs = File.OpenRead($"../../{i}.jpg");
                //    //        context.Response.Write("--myboundary\r\n",false);
                //    //        context.Response.Write("Content-Type:image/jpeg\r\n", false);
                //    //        context.Response.Write($"Content-Length:{fs.Length}\r\n\r\n", false);
                //    //        //data.Response.OutputStream.Write(jpg, 0, jpg.Length);
                //    //        context.Response.Write(fs, false);
                //    //        fs.Close();
                //    //        fs.Dispose();
                //    //        context.Response.Write("\r\n", false);
                //    //        System.Threading.Thread.Sleep(1000);
                //    //    }
                //    //}

                //    return Result.Hanlded;
                //});

                //server.Post<RowData>("/post/t", (http, data) =>
                //{
                //    //System.Diagnostics.Trace.WriteLine(data);
                //    return Result.String($"Index:{data.Index} Name:{data.Name}");
                //});

                //server.Start("127.0.0.1", 3456, new DirectoryInfo("../../webdata/"));
            }
            catch(Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
            }
        }
        ConcurrentBag<QSoft.Server.Http1.ServerSentEvent> m_Events = new ConcurrentBag<QSoft.Server.Http1.ServerSentEvent>();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="context"></param>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //public Result Get_Json(HttpListenerContext context, RowData data)
        //{
        //    return Result.Json(DateTime.Now);
        //}

        //async public Task<Result> Get_xml(HttpListenerContext context, RowData data)
        //{
        //    await Task.Delay(1);
        //    return Result.Json(DateTime.Now);
        //}
        ConcurrentBag<QSoft.Server.Http1.MultiPatStream> m_MultiParts = new ConcurrentBag<QSoft.Server.Http1.MultiPatStream>();
    }

    public class MainUI : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void Upate(string name)
        {
            if(this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    } 
}
namespace QQTest
{
    public class RowData
    {   
        public int Index { set; get; }
        public string Name { set; get; }

    }

    public class Address
    {
        public string IP { set; get; }
        public int Port { set; get; }
    }
    
}
