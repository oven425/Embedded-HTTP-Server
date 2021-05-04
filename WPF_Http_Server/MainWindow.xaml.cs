using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Server server = new Server();
            try
            {
                server.Get("/index", (HttpListenerContext data) => 
                {
                    return false;
                });
                server.Get("/test", (HttpListenerContext data) =>
                {
                    string str = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.ffff");
                    byte[] buf = Encoding.UTF8.GetBytes(str);
                    data.Response.ContentLength64 = buf.Length;
                    data.Response.OutputStream.Write(buf, 0, buf.Length);
                    return false;
                });
                server.Start("127.0.0.1", 3456);
            }
            catch(Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
            }
        }
    }
}
