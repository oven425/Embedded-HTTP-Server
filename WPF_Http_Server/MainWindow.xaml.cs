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
using QSoft.Server.Http.Extension;
using System.Linq.Expressions;
using QQTest;
using System.Web.Script.Serialization;
using System.ComponentModel;
using System.Reflection;
using QSoft.Server.Http1;

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

        [HttpMethodSetting()]
        public object Add(int x, int y)
        {
            return 1;
        }
        [HttpMethodSetting()]
        public object Add(int x)
        {
            return 1;
        }
        [HttpMethodSetting()]
        public object Add(string x, string y)
        {
            return 1;
        }
        [HttpMethodSetting()]
        public object Add(string x)
        {
            return 1;
        }

        [HttpMethodSetting()]
        public object Add()
        {
            return 1;
        }

        private static void CreateInstallCert(int expDate, string password, string issuedBy)
        {
            // Create/install certificate
            using (var powerShell = System.Management.Automation.PowerShell.Create())
            {
                var notAfter = DateTime.Now.AddYears(expDate).ToLongDateString();
                var assemPath = Assembly.GetCallingAssembly().Location;
                var fileInfo = new FileInfo(assemPath);
                var saveDir = System.IO.Path.Combine(fileInfo.Directory.FullName, "CertDir");
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                // This adds certificate to Personal and Intermediate Certification Authority
                var rootAuthorityName = "My-RootAuthority";
                var rootFriendlyName = "My Root Authority";
                var rootAuthorityScript =
                    $"$rootAuthority = New-SelfSignedCertificate" +
                    $" -DnsName '{rootAuthorityName}'" +
                    $" -NotAfter '{notAfter}'" +
                    $" -CertStoreLocation cert:\\LocalMachine\\My" +
                    $" -FriendlyName '{rootFriendlyName}'" +
                    $" -KeyUsage DigitalSignature,CertSign";
                powerShell.AddScript(rootAuthorityScript);

                // Export CRT file
                var rootAuthorityCrtPath = System.IO.Path.Combine(saveDir, "MyRootAuthority.crt");
                var exportAuthorityCrtScript =
                    $"$rootAuthorityPath = 'cert:\\localMachine\\my\\' + $rootAuthority.thumbprint;" +
                    $"Export-Certificate" +
                    $" -Cert $rootAuthorityPath" +
                    $" -FilePath {rootAuthorityCrtPath}";
                powerShell.AddScript(exportAuthorityCrtScript);

                // Export PFX file
                var rootAuthorityPfxPath = System.IO.Path.Combine(saveDir, "MyRootAuthority.pfx");
                var exportAuthorityPfxScript =
                    $"$pwd = ConvertTo-SecureString -String '{password}' -Force -AsPlainText;" +
                    $"Export-PfxCertificate" +
                    $" -Cert $rootAuthorityPath" +
                    $" -FilePath '{rootAuthorityPfxPath}'" +
                    $" -Password $pwd";
                powerShell.AddScript(exportAuthorityPfxScript);

                // Create the self-signed certificate, signed using the above certificate
                var gatewayAuthorityName = "My-Service";
                var gatewayFriendlyName = "My Service";
                var gatewayAuthorityScript =
                    $"$rootcert = ( Get-ChildItem -Path $rootAuthorityPath );" +
                    $"$gatewayCert = New-SelfSignedCertificate" +
                    $" -DnsName '{gatewayAuthorityName}'" +
                    $" -NotAfter '{notAfter}'" +
                    $" -certstorelocation cert:\\localmachine\\my" +
                    $" -Signer $rootcert" +
                    $" -FriendlyName '{gatewayFriendlyName}'" +
                    $" -KeyUsage KeyEncipherment,DigitalSignature";
                powerShell.AddScript(gatewayAuthorityScript);

                // Export new certificate public key as a CRT file
                var myGatewayCrtPath = System.IO.Path.Combine(saveDir, "MyGatewayAuthority.crt");
                var exportCrtScript =
                    $"$gatewayCertPath = 'cert:\\localMachine\\my\\' + $gatewayCert.thumbprint;" +
                    $"Export-Certificate" +
                    $" -Cert $gatewayCertPath" +
                    $" -FilePath {myGatewayCrtPath}";
                powerShell.AddScript(exportCrtScript);

                // Export the new certificate as a PFX file
                var myGatewayPfxPath = System.IO.Path.Combine(saveDir, "MyGatewayAuthority.pfx");
                var exportPfxScript =
                    $"Export-PfxCertificate" +
                    $" -Cert $gatewayCertPath" +
                    $" -FilePath {myGatewayPfxPath}" +
                    $" -Password $pwd"; // Use the previous password
                powerShell.AddScript(exportPfxScript);

                powerShell.Invoke();
            }
        }

        MainUI m_MainUI;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Server1 server1 = new Server1();
            server1.Add(this);
            server1.Strat("127.0.0.1", 3456);
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

            JavaScriptSerializer js = new JavaScriptSerializer();
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
                server.Get<RowData>("/get/json", async (context, data) =>
                {
                    await Task.Delay(1);
                    return Result.Json(DateTime.Now);
                });

                //server.Get<RowData>("/get/json", (context, data) => Get_Json(context, data));
                server.Get<RowData>("/get/xml", (context, data) => Get_xml(context, data));

                server.Get("/get/jpg", (context, query) =>
                {
                    return Result.Stream(File.OpenRead("../../1.jpg"));
                });

                server.Get("/events.html", (context, data) =>
                {
                    return Result.Stream(File.OpenRead($"{server.Statics.FullName}events.html"));
                });

                server.Get<RowData>("/sse/test", (context, data) =>
                {
                    //context.Response.ContentType = "text/event-stream";
                    //context.Response.Headers["Connection"] = "keep-alive";
                    //context.Response.Headers["Cache-Control"] = "no-cache";
                    //m_Events.Add( context.Response);

                    //Task.Run(async () =>
                    //{
                    //    while (true)
                    //    {
                    //        for (int i = 0; i < m_Events.Count; i++)
                    //        {
                    //            string msg = $"id: 123\ndata: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\n\n";

                    //            m_Events.ElementAt(i).Write(msg, false);
                    //        }
                    //        await Task.Delay(1000);
                    //    }
                    //});
                    this.m_Events.Add(new ServerSentEvent(context.Response, DateTime.Now.ToString("HHmmssfff")));
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            for (int i = 0; i < m_Events.Count; i++)
                            {
                                string msg = $"id: 123\ndata: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\n\n";

                                this.m_Events.ElementAt(i).WriteMessage(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                            }
                            await Task.Delay(1000);
                        }
                    });
                    return Result.Hanlded;
                });
                server.Get<RowData>("/get/mjpg", (context, data) =>
                {
                    this.m_MultiParts.Add(new MultiPatStream(context.Response));
                    while (true)
                    {
                        foreach(var oo in this.m_MultiParts)
                        {
                            for (int i = 1; i <= 3; i++)
                            {
                                try
                                {
                                    FileStream fs = File.OpenRead($"../../{i}.jpg");
                                    oo.Write(fs, "image/jpeg");
                                    //context.Response.Write("--myboundary\r\n", false);
                                    //context.Response.Write("Content-Type:image/jpeg\r\n", false);
                                    //context.Response.Write($"Content-Length:{fs.Length}\r\n\r\n", false);
                                    ////data.Response.OutputStream.Write(jpg, 0, jpg.Length);
                                    //context.Response.Write(fs, false);
                                    //fs.Close();
                                    //fs.Dispose();
                                    //context.Response.Write("\r\n", false);
                                }
                                catch (Exception ee)
                                {
                                    System.Diagnostics.Trace.WriteLine(ee.Message);
                                }
                                
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                        
                    }
                    //context.Response.ContentType = "multipart/x-mixed-replace;boundary=--myboundary";
                    //while (true)
                    //{
                    //    for (int i = 1; i <= 3; i++)
                    //    {
                    //        FileStream fs = File.OpenRead($"../../{i}.jpg");
                    //        context.Response.Write("--myboundary\r\n",false);
                    //        context.Response.Write("Content-Type:image/jpeg\r\n", false);
                    //        context.Response.Write($"Content-Length:{fs.Length}\r\n\r\n", false);
                    //        //data.Response.OutputStream.Write(jpg, 0, jpg.Length);
                    //        context.Response.Write(fs, false);
                    //        fs.Close();
                    //        fs.Dispose();
                    //        context.Response.Write("\r\n", false);
                    //        System.Threading.Thread.Sleep(1000);
                    //    }
                    //}

                    return Result.Hanlded;
                });

                server.Post<RowData>("/post/t", (http, data) =>
                {
                    //System.Diagnostics.Trace.WriteLine(data);
                    return Result.String($"Index:{data.Index} Name:{data.Name}");
                });

                server.Start("127.0.0.1", 3456, new DirectoryInfo("../../webdata/"));
            }
            catch(Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
            }
        }
        ConcurrentBag<ServerSentEvent> m_Events = new ConcurrentBag<ServerSentEvent>();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public Result Get_Json(HttpListenerContext context, RowData data)
        {
            return Result.Json(DateTime.Now);
        }

        async public Task<Result> Get_xml(HttpListenerContext context, RowData data)
        {
            await Task.Delay(1);
            return Result.Json(DateTime.Now);
        }
        ConcurrentBag<MultiPatStream> m_MultiParts = new ConcurrentBag<MultiPatStream>();
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

    
}
