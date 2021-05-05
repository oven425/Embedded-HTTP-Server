using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QSoft.Server.Http
{
    public class Server
    {
        HttpListener m_Listener = new HttpListener();
        public void Start(string ip, int port)
        {
            //HttpListener listener = new HttpListener();
            //listener.Prefixes.Add("http://127.0.0.1:3456");
            //listener.AuthenticationSchemes = AuthenticationSchemes.Digest;
            //listener.Realm = "testrealm@host.com";
            //listener.Start();
            //var context = listener.GetContext();

            string domain = Environment.UserDomainName;
            string hostname = Dns.GetHostName();
            this.m_Listener.Prefixes.Add($"http://{ip}:{port}/");
            //this.m_Listener.AuthenticationSchemeSelectorDelegate = new AuthenticationSchemeSelector(AuthenticationSchemeForClient);
            this.m_Listener.Realm = "testrealm@host.com";
            //this.m_Listener.AuthenticationSchemes = AuthenticationSchemes.Digest;
            this.m_Listener.Start();

            Task.Run(async () =>
            {
                while (true)
                {
                    var req = await this.m_Listener.GetContextAsync();
                    if (req.Request.HttpMethod.ToUpperInvariant() == "GET" && this.m_Gets.ContainsKey(req.Request.Url.LocalPath) == true)
                    {
                        if (this.m_Gets[req.Request.RawUrl](req) == true)
                        {

                        }
                    }
                }
            });
        }

        AuthenticationSchemes AuthenticationSchemeForClient(HttpListenerRequest request)
        {
            // Do not authenticate local machine requests.
            if (request.RemoteEndPoint.Address.Equals(IPAddress.Loopback))
            {
                return AuthenticationSchemes.Digest;
            }
            else
            {
                return AuthenticationSchemes.IntegratedWindowsAuthentication;
            }
        }

        ConcurrentDictionary<string, Func<HttpListenerContext, bool>> m_Gets = new ConcurrentDictionary<string, Func<HttpListenerContext, bool>>();
        public void Get(string path, Func<HttpListenerContext, bool> data)
        {
            this.m_Gets.AddOrUpdate(path, data, (k, v) => data);
        }
    }

    public class Session
    {
        public bool IsHandled { set; get; }
        public HttpListenerRequest Request { set; get; }
        public HttpListenerResponse Response { set; get; }
    }

    
} 

namespace QSoft.Server.Http.Extention
{
    static public class HttpListenerResponseEx
    {
        public static void Write(this HttpListenerResponse src, string data, string content_type)
        {
            if(string.IsNullOrWhiteSpace(content_type) == false)
            {

            }
            byte[] writebuf = Encoding.UTF8.GetBytes(data);
            src.OutputStream.Write(writebuf, 0, writebuf.Length);
        }

        public static void Write(this HttpListenerResponse src, byte[] data)
        {
            src.OutputStream.Write(data, 0, data.Length);
        }

        public static void Write(this HttpListenerResponse src, string data)
        {
            byte[] writebuf = Encoding.UTF8.GetBytes(data);
            src.OutputStream.Write(writebuf, 0, writebuf.Length);
        }

        public static void Write(this HttpListenerResponse src, Stream data)
        {
            byte[] read_buf = new byte[8192];
            while(true)
            {
                int read_len = data.Read(read_buf, 0, read_buf.Length);
                src.OutputStream.Write(read_buf, 0, read_len);
                if(read_len != read_buf.Length)
                {
                    return;
                }
            }
        }
    }
}
