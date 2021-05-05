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


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            Uri uri = new Uri(@"test\a\b\c", UriKind.RelativeOrAbsolute);
            var sss = uri.Segments;
            Server server = new Server();
            try
            {
                server.Get("/test", (HttpListenerContext data) =>
                {
                    string str = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff");
                    data.Response.Write(str);
                    data.Response.Close();
                    return false;
                });
                server.Get("/test/file", (HttpListenerContext data) =>
                {
                    string str = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff");
                    byte[] buf = Encoding.UTF8.GetBytes(str);
                    data.Response.ContentLength64 = buf.Length;
                    data.Response.OutputStream.Write(buf, 0, buf.Length);
                    return false;
                });
                server.Get("/sse", (data) =>
                {
                    string responseString = File.ReadAllText("../../events.html");
                    data.Response.Write(responseString);
                    return false;
                });

                server.Get("/sse/test", (data) =>
                {
                    data.Response.ContentType = "text/event-stream";
                    data.Response.Headers["Connection"] = "keep-alive";
                    data.Response.Headers["Cache-Control"] = "no-cache";
                    m_Events.Add(data.Response);
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            for (int i = 0; i < m_Events.Count; i++)
                            {
                                string msg = $"id: 123\ndata: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\n\n";

                                m_Events.ElementAt(i).Write(msg);
                            }
                            await Task.Delay(1000);
                        }
                    });
                    return true;
                });
                server.Get("/mjpg", (data) =>
                {
                    data.Response.ContentType = "multipart/x-mixed-replace;boundary=--myboundary";
                    while (true)
                    {
                        for (int i = 1; i <= 3; i++)
                        {
                            FileStream fs = File.OpenRead($"../../{i}.jpg");
                            data.Response.Write("--myboundary\r\n");
                            data.Response.Write("Content-Type:image/jpeg\r\n");
                            data.Response.Write($"Content-Length:{fs.Length}\r\n\r\n");
                            //data.Response.OutputStream.Write(jpg, 0, jpg.Length);
                            data.Response.Write(fs);
                            fs.Close();
                            fs.Dispose();
                            data.Response.Write("\r\n");
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                    
                    


                    return true;
                });
                server.Start("127.0.0.1", 3456);
            }
            catch(Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
            }
        }
        ConcurrentBag<HttpListenerResponse> m_Events = new ConcurrentBag<HttpListenerResponse>();
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
