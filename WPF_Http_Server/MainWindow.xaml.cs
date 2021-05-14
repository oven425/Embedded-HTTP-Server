using System;
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
using QSoft.Server.Http.Extention;
using System.Linq.Expressions;
using QQTest;
using System.Web.Script.Serialization;
using System.ComponentModel;

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

        void Sleep(TimeSpan data)
        {
            DateTime now = DateTime.Now;
            while(true)
            {
                if((DateTime.Now - now)>= data)
                {
                    return;
                }
            }
        }

        public void TT(string data)
        {

        }

        Dictionary<string, Func<object[], object>> m_Funcs = new Dictionary<string, Func<object[], object>>();
        public void Test(string path, Func<string, string> process)
        {
            
            var method = process.GetType().GetMethods()[0];
            var args = System.Linq.Expressions.Expression.Parameter(typeof(object[]), "args");
            var parameters = method.GetParameters()
                .Select((x, index) =>
                    System.Linq.Expressions.Expression.Convert(
                        System.Linq.Expressions.Expression.ArrayIndex(args, System.Linq.Expressions.Expression.Constant(index)),
                    x.ParameterType))
                .ToArray();

            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object[], object>>(
                        System.Linq.Expressions.Expression.Convert(
                            System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.New(process.Target.GetType()), process.Method, parameters),
                            typeof(object)),
                        args).Compile();
            m_Funcs[path] = lambda;
        }

        public void Test<T>(string path, Func<T, string> process)
        {

            var method = process.GetType().GetMethods()[0];
            var args = System.Linq.Expressions.Expression.Parameter(typeof(object[]), "args");
            var parameters = method.GetParameters()
                .Select((x, index) =>
                    System.Linq.Expressions.Expression.Convert(
                        System.Linq.Expressions.Expression.ArrayIndex(args, System.Linq.Expressions.Expression.Constant(index)),
                    x.ParameterType))
                .ToArray();

            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object[], object>>(
                        System.Linq.Expressions.Expression.Convert(
                            System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.New(process.Target.GetType()), process.Method, parameters),
                            typeof(object)),
                        args).Compile();
            m_Funcs[path] = lambda;
        }

        class MyClass
        {
            public int MyProperty { get; set; }
        }

        string TFunc(int a, int b)
        {
            return (a + b).ToString();
        }
        MainUI m_MainUI;
        int m_Int = 0;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(this.m_MainUI == null)
            {
                this.DataContext = this.m_MainUI = new MainUI();
            }
            RowData rd = new RowData();
            rd.Index = 1;
            rd.Name = "BBen";

            JavaScriptSerializer js = new JavaScriptSerializer();
            string json_str = js.Serialize(rd);
            System.Diagnostics.Trace.WriteLine(json_str);
            //System.Xml.Serialization.XmlSerializer xml = new System.Xml.Serialization.XmlSerializer(typeof(RowData));
            //using (MemoryStream mm = new MemoryStream())
            //{
            //    xml.Serialize(mm, rd);
            //    System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(mm.ToArray()));
            //}
            ////Test("1", (str) =>
            ////{
            ////    return str;
            ////});
            //Test<int>("1", (data) =>
            //{
            //    return data.ToString();
            //});

            //var hhh = this.m_Funcs["1"].Invoke(new object[] { 123});
            //var result = new HogeController().HugaAction(1, 10);

            //var controllerName = "WPF_Http_Server.HogeController";
            //var actionName = "HugaAction";

            //var instance = Activator.CreateInstance(Type.GetType(controllerName));
            //var result = (string)Type.GetType(controllerName).GetMethod(actionName).Invoke(instance, new object[] { 1, 10 });


            //var controllerName = "WPF_Http_Server.HogeController";
            //var actionName = "HugaAction";
            //var type = Type.GetType(controllerName);
            //var method = type.GetMethod(actionName);

            //var instance = Activator.CreateInstance(type);
            ////var methodDelegate = (Func<int, int, string>)Delegate.CreateDelegate(typeof(Func<int, int, string>), instance, method);
            //var methodDelegate = (Func<HogeController, int, int, string>)Delegate.CreateDelegate(typeof(Func<HogeController, int, int, string>), method);
            //var result = methodDelegate((HogeController)Activator.CreateInstance(type), 10, 20);


            //var x = System.Linq.Expressions.Expression.Parameter(typeof(int), "x");
            //var y = System.Linq.Expressions.Expression.Parameter(typeof(int), "y");
            //var lambda = System.Linq.Expressions.Expression.Lambda<Func<int, int, string>>(
            //    System.Linq.Expressions.Expression.Call( // .HugaAction(x, y)
            //        System.Linq.Expressions.Expression.New(type), // new HogeController()
            //        method,
            //        x, y),
            //    x, y) // (x, y) => 
            //    .Compile();
            //var result = lambda(5, 10);

            //var cache = new ConcurrentDictionary<Tuple<string, string>, Delegate>();
            //var dynamicDelegate = cache.GetOrAdd(Tuple.Create(controllerName, actionName), _ =>
            //{
            //    // パラメータはMethodInfoから動的に作る
            //    var parameters = method.GetParameters().Select(x =>
            //            System.Linq.Expressions.Expression.Parameter(x.ParameterType, x.Name))
            //        .ToArray();

            //    return System.Linq.Expressions.Expression.Lambda(
            //            System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.New(type), method, parameters),
            //        parameters).Compile();
            //});
            //var result = dynamicDelegate.DynamicInvoke(new object[] { 10, 20 });

            //var cache = new ConcurrentDictionary<string, Func<object[], object>>();
            //var args = System.Linq.Expressions.Expression.Parameter(typeof(object[]), "args");
            //var parameters = method.GetParameters()
            //    .Select((x, index) =>
            //        System.Linq.Expressions.Expression.Convert(
            //            System.Linq.Expressions.Expression.ArrayIndex(args, System.Linq.Expressions.Expression.Constant(index)),
            //        x.ParameterType))
            //    .ToArray();

            //var lambda = System.Linq.Expressions.Expression.Lambda<Func<object[], object>>(
            //            System.Linq.Expressions.Expression.Convert(
            //                System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.New(type), method, parameters),
            //                typeof(object)),
            //            args).Compile();
            //var result = lambda.Invoke(new object[] { "1.1", (float)2.2 });



            //return;
            //WebRequest.DefaultWebProxy = null;
            //WebRequest webreq = WebRequest.Create("https://cctvn.freeway.gov.tw/abs2mjpg/bmjpg?camera=10000&0.21707882011757307&t1968=0.8138169668544286");
            //try
            //{
            //    var resp = webreq.GetResponse();
            //    var stream = resp.GetResponseStream();
            //    byte[] readbuf = new byte[81920];
            //    while (true)
            //    {
            //        int read_len = stream.Read(readbuf, 0, readbuf.Length);

            //        System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(readbuf, 0, read_len));
            //    }
            //}
            //catch (Exception ee)
            //{
            //    System.Diagnostics.Trace.WriteLine(ee.Message);
            //}
            

            Server server = new Server();
            try
            {
                //server.Get<RowData>("/get/json", (context, data) =>
                //{
                //    return Result.Josn<DateTime>(DateTime.Now);
                //});

                //server.Get<RowData>("/get/xml", (context, data) =>
                //{
                //    return Result.Xml<DateTime>(DateTime.Now);
                //});

                //server.Get<RowData>("/get/jpg", (context, data) =>
                //{
                //    return Result.Stream(File.OpenRead("../../1.jpg"));
                //});

                //server.Get<RowData>("/sse", (context, data) =>
                //{
                //    return Result.Stream(File.OpenRead("../../events.html"));
                //});
                server.Get<RowData>("/get/test", (context, data) => this.Test_get(context, data));
                server.Get<RowData>("/sse/test", (context, data) =>
                {
                    //this.m_Int = 0;
                    context.Response.ContentType = "text/event-stream";
                    context.Response.Headers["Connection"] = "keep-alive";
                    context.Response.Headers["Cache-Control"] = "no-cache";
                    //this.Dispatcher.Invoke(() =>
                    //{
                    //    //m_Events.Add(context.Response);
                    //});
                    
                    //Task.Run(async () =>
                    //{
                    //    while (true)
                    //    {
                    //        for (int i = 0; i < m_Events.Count; i++)
                    //        {
                    //            string msg = $"id: 123\ndata: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\n\n";

                    //            m_Events.ElementAt(i).Write(msg);
                    //        }
                    //        await Task.Delay(1000);
                    //    }
                    //});
                    return Result.Hanlded;
                });
                server.Get<RowData>("/get/mjpg", (context, data) =>
                {
                    context.Response.ContentType = "multipart/x-mixed-replace;boundary=--myboundary";
                    while (true)
                    {
                        for (int i = 1; i <= 3; i++)
                        {
                            FileStream fs = File.OpenRead($"../../{i}.jpg");
                            context.Response.Write("--myboundary\r\n",false);
                            context.Response.Write("Content-Type:image/jpeg\r\n", false);
                            context.Response.Write($"Content-Length:{fs.Length}\r\n\r\n", false);
                            //data.Response.OutputStream.Write(jpg, 0, jpg.Length);
                            context.Response.Write(fs, false);
                            fs.Close();
                            fs.Dispose();
                            context.Response.Write("\r\n", false);
                            System.Threading.Thread.Sleep(1000);
                        }
                    }

                    return Result.Hanlded;
                });

                server.Post<RowData>("/post/t", (http, data) =>
                {
                    System.Diagnostics.Trace.WriteLine(data);
                    return new JsonReuslt<DateTime>(DateTime.Now);
                });

                server.Start("127.0.0.1", 3456);
            }
            catch(Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
            }
        }
        ConcurrentBag<HttpListenerResponse> m_Events = new ConcurrentBag<HttpListenerResponse>();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        public Result Test_get(HttpListenerContext context, RowData data)
        {
            return Result.Josn<DateTime>(DateTime.Now);
        }
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

    public class HogeController
    {
        public string HugaAction(int x, int y)
        {
            return (x + y).ToString();
        }

        public double TakoAction(string s, float f)
        {
            return double.Parse(s) * f;
        }
    }

    public class MultiPatStream
    {
        ConcurrentBag<HttpListenerResponse> Clients = new ConcurrentBag<HttpListenerResponse>();

        public string Bondary { set; get; } = "--myboundary";
        public void Write(Stream data, string content_type)
        {
            byte[] buf = new byte[8192];
            long length = data.Length - data.Position;
            long oldpos = data.Position;
            foreach(var oo in this.Clients)
            {
                data.Position = oldpos;
                oo.Write(Bondary);
                oo.Write("Content-Type:image/jpeg");
                oo.Write($"Content-Length:{length}\r\n\r\n");

                oo.Write(data);
                oo.Write("\r\n\r\n");
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
}
