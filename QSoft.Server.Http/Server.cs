using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using QSoft.Server.Http.Extention;

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
                    var context = await this.m_Listener.GetContextAsync();
                    
                    if (context.Request.HttpMethod.ToUpperInvariant() == "GET" && this.m_Gets.ContainsKey(context.Request.Url.LocalPath) == true)
                    {
                        ActionData ad = this.m_Gets[context.Request.Url.LocalPath];
                        object obj = null;
                        string data_str = context.Request.ReadString();
                        obj = Activator.CreateInstance(ad.DataType);
                        var pps = ad.DataType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.CanWrite == true);
                        foreach (var pp in pps)
                        {
                            if (context.Request.QueryString.AllKeys.Any(x => x == pp.Name) == true)
                            {
                                pp.SetValue(obj, ConvertProperty(context.Request.QueryString[pp.Name], pp));
                            }
                        }
                        try
                        {
                            object hr = ad.Action.Invoke(new object[] { context, obj });
                            Result result = hr as Result;
                            result.Invoke(context.Response);
                        }
                        catch(Exception ee)
                        {
                            System.Diagnostics.Trace.WriteLine(ee.Message);
                        }
                        
                    }
                    else if (context.Request.HttpMethod.ToUpperInvariant() == "POST" && this.m_Posts.ContainsKey(context.Request.Url.LocalPath) == true)
                    {
                        ActionData ad = this.m_Posts[context.Request.Url.LocalPath];
                        object obj = null;
                        if (context.Request.ContentLength64 > 0)
                        {

                            if (context.Request.ContentType == "application/xml")
                            {
                                System.Xml.Serialization.XmlSerializer xml = new System.Xml.Serialization.XmlSerializer(ad.DataType);
                                obj = xml.Deserialize(context.Request.InputStream);
                            }
                            else if (context.Request.ContentType == "application/json")
                            {
                                string data_str = context.Request.ReadString();
                                JavaScriptSerializer js = new JavaScriptSerializer();
                                obj = js.Deserialize(data_str, ad.DataType);
                            }
                            else if (context.Request.ContentType == "application/x-www-form-urlencoded")
                            {
                                string data_str = context.Request.ReadString();
                                var vv = HttpUtility.ParseQueryString(data_str);
                                obj = Activator.CreateInstance(ad.DataType);
                                var pps = ad.DataType.GetProperties(BindingFlags.Instance|BindingFlags.Public).Where(x=>x.CanWrite==true);
                                foreach(var pp in pps)
                                {
                                    if(vv.AllKeys.Any(x => x ==pp.Name) == true)
                                    {
                                        pp.SetValue(obj, ConvertProperty(vv[pp.Name], pp));
                                    }
                                }
                            }
                            object hr = this.m_Posts[context.Request.Url.LocalPath].Action.Invoke(new object[] { context, obj });
                            Result result = hr as Result;
                            result.Invoke(context.Response);
                        }
                    }
                }
            });
        }

        object ConvertProperty(string src, PropertyInfo info)
        {
            object dst = null;
            switch (Type.GetTypeCode(info.PropertyType))
            {
                case TypeCode.Decimal:
                    {
                        Decimal a = 0;
                        Decimal.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.Boolean:
                    {
                        bool a = false;
                        bool.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.Byte:
                    {
                        Byte a = 0;
                        Byte.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.Char:
                    {
                        Char a = Char.MinValue;
                        Char.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.SByte:
                    {
                        sbyte a = 0;
                        sbyte.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.Single:
                    {
                        float a = 0;
                        float.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.Double:
                    {
                        double a = 0;
                        double.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.String:
                    {
                        dst = src;
                    }
                    break;
                case TypeCode.Int16:
                    {
                        Int16 a = 0;
                        Int16.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.Int32:
                    {
                        Int32 a = 0;
                        Int32.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.Int64:
                    {
                        Int64 a = 0;
                        Int64.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.UInt16:
                    {
                        UInt16 a = 0;
                        UInt16.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.UInt32:
                    {
                        UInt32 a = 0;
                        UInt32.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.UInt64:
                    {
                        UInt64 a = 0;
                        UInt64.TryParse(src, out a);
                        dst = a;
                    }
                    break;
                case TypeCode.DateTime:
                    {
                        DateTime a = DateTime.MinValue;
                        DateTime.TryParse(src, out a);
                        dst = a;
                    }
                    break;
            }
            return dst;
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

        Dictionary<string, ActionData> m_Gets = new Dictionary<string, ActionData>();
        public void Get<T>(string path, Func<HttpListenerContext, T, Result> process, Action<Exception> fail = null)where T:class
        {
            
            var args = System.Linq.Expressions.Expression.Parameter(typeof(object[]), "args");
            var parameters = process.Method.GetParameters()
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
            ActionData action = new ActionData();
            action.Action = lambda;
            action.Path = path;
            action.DataType = parameters[1].Type;
            action.Fail = fail;
            this.m_Gets[path] = action;
        }

        public void Get(string path, Func<HttpListenerContext, List<string>, Result> process, Action<Exception> fail = null)
        {
            this.Get<List<string>>(path, process, fail);
        }

        Dictionary<string, ActionData> m_Posts = new Dictionary<string, ActionData>();
        public void Post<T>(string path, Func<HttpListenerContext, T, Result> process, Action<Exception> fail = null)
        {
            var args = System.Linq.Expressions.Expression.Parameter(typeof(object[]), "args");
            var parameters = process.Method.GetParameters()
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
            ActionData action = new ActionData();
            action.Action = lambda;
            action.Path = path;
            action.DataType = parameters[1].Type;
            action.Fail = fail;
            this.m_Posts[path] = action;

        }
    }   
    public class ActionData
    {
        public Type DataType { set; get; }
        public string Path { set; get; }
        public Func<object[], object> Action { set; get; }
        public Action<Exception> Fail { set; get; }
    }

    //public interface IResult
    //{
    //    void Invoke(HttpListenerResponse resp);
    //    bool IsHanlded { set; get; }
    //}

    public class Result
    {
        virtual public void Invoke(HttpListenerResponse resp)
        {
            if(this.IsHanlded == false)
            {
                resp.Close();
            }
        }
        protected bool IsHanlded { set; get; }

        public static Result Hanlded { get; } = new Result() { IsHanlded = true };
        public static JsonReuslt<T> Josn<T>(T data) { return new JsonReuslt<T>(data); }
        public static XmlReuslt<T> Xml<T>(T data) { return new XmlReuslt<T>(data); }
        public static StreamResult Stream(Stream data, string contenttype="", bool auto_close=true) { return new StreamResult(data, contenttype, auto_close); }
    }

    public class StringResult:Result
    {
        string m_Data;
        public StringResult(string data)
        {
            this.m_Data = data;
        }
        public override void Invoke(HttpListenerResponse resp)
        {
            resp.Write(this.m_Data);
        }
    }

    public class StreamResult : Result
    {
        public StreamResult(Stream stream, string contenttype, bool auto_close)
        {
            this.m_IsAutoClose = auto_close;
            this.m_ContentType = contenttype;
            this.m_Data = stream;
            if (string.IsNullOrWhiteSpace(this.m_ContentType) == true)
            {
                FileStream fs = this.m_Data as FileStream;
                if(fs != null)
                {
                    FileInfo finfo = new FileInfo(fs.Name);
                    switch(finfo.Extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                        case ".png":
                            {
                                this.m_ContentType = $"image/{finfo.Extension.TrimStart('.')}";
                            }
                            break;
                        case ".ini":
                            {
                                this.m_ContentType = $"text/plain";
                            }
                            break;
                        case ".xml":
                        case ".json":
                            {
                                this.m_ContentType = $"application/{finfo.Extension.TrimStart('.')}";
                            }
                            break;
                        case ".html":
                        case ".css":
                        case ".js":
                            {
                                this.m_ContentType = $"text/{finfo.Extension.TrimStart('.')}";
                            }
                            break;
                        default:
                            {
                                this.m_ContentType = "application/octet-stream";
                            }
                            break;
                    }
                }
            }
            
        }
        Stream m_Data;
        string m_ContentType;
        bool m_IsAutoClose;
        override public void Invoke(HttpListenerResponse resp)
        {
            resp.ContentType = this.m_ContentType;
            resp.Write(this.m_Data);
            if(this.m_IsAutoClose == true)
            {
                this.m_Data.Close();
                this.m_Data.Dispose();
                this.m_Data = null;
            }
        }
    }

    public class JsonReuslt<T> : Result
    {
        public JsonReuslt(T data)
        {
            this.m_Data = data;
        }
        T m_Data;
        
        override public void Invoke(HttpListenerResponse resp)
        {
            resp.ContentType = "application/json";
            JavaScriptSerializer js = new JavaScriptSerializer();
            var json = js.Serialize(this.m_Data);
            resp.Write(json);
        }
    }

    public class XmlReuslt<T> : Result
    {
        public XmlReuslt(T data)
        {
            this.m_Data = data;
        }
        T m_Data;

        override public void Invoke(HttpListenerResponse resp)
        {
            resp.ContentType = "application/xml";
            System.Xml.Serialization.XmlSerializer xml = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (MemoryStream mm = new MemoryStream())
            {
                xml.Serialize(mm, this.m_Data);
                mm.Position = 0;
                resp.Write(mm);
            }
        }
    }
} 

namespace QSoft.Server.Http.Extention
{
    static public class HttpListenerRequestEx
    {
        public static string ReadString(this HttpListenerRequest src)
        {
            int read_len = 0;
            byte[] read_buf = new byte[8192];
            string dst = "";
            using (MemoryStream mm = new MemoryStream())
            {
                while (true)
                {
                    read_len = src.InputStream.Read(read_buf, 0, read_buf.Length);
                    if (read_len > 0)
                    {
                        mm.Write(read_buf, 0, read_len);
                    }
                    if (mm.Length == src.ContentLength64)
                    {
                        dst = src.ContentEncoding.GetString(mm.ToArray());
                        break;
                    }
                }
            }
            return dst;
        }
    }
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
            src.ContentLength64 = data.Length;
            src.OutputStream.Write(data, 0, data.Length);
        }

        public static void Write(this HttpListenerResponse src, string data, bool autolength=true)
        {
            byte[] writebuf = Encoding.UTF8.GetBytes(data);
            if(autolength == true)
            {
                src.ContentLength64 = writebuf.Length;
            }
            src.OutputStream.Write(writebuf, 0, writebuf.Length);
        }

        public static void Write(this HttpListenerResponse src, Stream data, bool autolength = true)
        {
            byte[] read_buf = new byte[8192];
            if(autolength == true)
            {
                src.ContentLength64 = data.Length - data.Position;
            }
            
            while (true)
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
