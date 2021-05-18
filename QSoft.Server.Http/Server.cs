using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using QSoft.Server.Http.Extention;

namespace QSoft.Server.Http
{
    //cd C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64

    //makecert -n "CN=vMargeCA" -r -sv vMargeCA.pvk vMargeCA.cer

    //makecert -sk vMargeSignedByCA -iv vMargeCA.pvk -n "CN=vMargeSignedByCA" -ic vMargeCA.cer vMargeSignedByCA.cer -sr localmachine -ss My

    //netsh http add sslcert ipport=0.0.0.0:8443 certhash=e56befc8decaf48998357e25f9d0a8a3f32fd0cb appid={D87014A7-0FF2-4CFC-A3FB-4975F5E2843E}


    //netsh http delete sslcert 0.0.0.0:8443
    public class Server
    {
        public void Stop()
        {
            this.m_Listener.Stop();
            this.m_Listener.Close();
        }
        HttpListener m_Listener = new HttpListener();
        public DirectoryInfo Statics { protected set; get; } = null;
        public void Start(string ip, int port, DirectoryInfo staticsfolder =null)
        {
            this.Statics = staticsfolder;
            string hostname = Dns.GetHostName();
            //String[] prefixes = { "http://*:8089/", "https://*:8443/" };
            //foreach (var oo in prefixes)
            //{
            //    this.m_Listener.Prefixes.Add(oo);
            //}
            this.m_Listener.Prefixes.Add($"http://{ip}:{port}/");
            this.m_Listener.Start();
            int UserCount = 0;
            int maxount = 5;
            Task.Run(() =>
            {
                while (true)
                {
                    var context = this.m_Listener.GetContext();
                    if (Interlocked.CompareExchange(ref UserCount, maxount, maxount) >= maxount)
                    {
                        HttpFailResult.TooManyRequests().Invoke(context.Response);
                    }
                    else
                    {
                        Interlocked.Increment(ref UserCount);
                        Task.Run(async() =>
                        {
                            bool compelete = false;

                            System.Diagnostics.Trace.WriteLine($"usercount:{UserCount}");
                            try
                            {
                                if (context.Request.HttpMethod.ToUpperInvariant() == "GET")
                                {
                                    compelete = await Process_Get(context);
                                }
                                else if (context.Request.HttpMethod.ToUpperInvariant() == "POST" && this.m_Posts.ContainsKey(context.Request.Url.LocalPath) == true)
                                {
                                    compelete = await Process_Post(context);
                                }

                            }
                            catch (Exception ee)
                            {
                                System.Diagnostics.Trace.WriteLine(ee.Message);
                                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                                if (compelete == false)
                                {
                                    HttpFailResult.BadRequest($"{ee.Message}\r\n{ee.StackTrace}").Invoke(context.Response);
                                }
                            }
                            finally
                            {
                                if (compelete == false)
                                {
                                    HttpFailResult.BadRequest().Invoke(context.Response);
                                }
                            }
                            Interlocked.Decrement(ref UserCount);
                        });
                    }
                }
            });
        }

        async Task<bool> Process_Get(HttpListenerContext context)
        {
            bool compelete = false;
            if (this.m_Gets.ContainsKey(context.Request.Url.LocalPath) == true)
            {
                ActionData ad = this.m_Gets[context.Request.Url.LocalPath];
                object obj = null;
                if (ad.DataType == typeof(NameValueCollection))
                {
                    obj = context.Request.QueryString;
                }
                else
                {
                    obj = this.ToObject(ad.DataType, context.Request.QueryString);
                }
                if (ad.Method.ReturnType.IsGenericType == true && ad.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    Task<Result> hr = (Task<Result>)ad.Method.Invoke(ad.Target, new object[] { context, obj });
                    Result result = await hr;
                    result.Invoke(context.Response);
                }
                else
                {
                    object hr = ad.Method.Invoke(ad.Target, new object[] { context, obj });
                    Result result = hr as Result;
                    result.Invoke(context.Response);
                }
                compelete = true;
            }
            if (compelete == false)
            {
                if (File.Exists(this.Statics.FullName + context.Request.Url.LocalPath) == true)
                {
                    Result.Stream(File.OpenRead(this.Statics.FullName + context.Request.Url.LocalPath)).Invoke(context.Response);
                    compelete = true;
                }
            }
            return compelete;
        }

        async Task<bool> Process_Post(HttpListenerContext context)
        {
            bool compelete = false;
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
                    obj = this.ToObject(ad.DataType, vv);
                }
                else if (context.Request.ContentType.IndexOf("multipart/form-data") == 0)
                {
                    obj = Activator.CreateInstance(ad.DataType);
                    var pps = ad.DataType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.CanWrite == true);
                    HttpMultipartFormParser multipart = new HttpMultipartFormParser(context.Request, context.Request.ContentEncoding);

                    foreach (var oo in multipart.ParseIntoElementList())
                    {
                        var pp = pps.FirstOrDefault(x => x.Name == oo.Name);
                        if (pp != null)
                        {
                            pp.SetValue(obj, pp.PropertyType.Convert(Encoding.UTF8.GetString(oo.Data)));
                        }
                    }
                }
                if (ad.Method.ReturnType.IsGenericType == true && ad.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    Task<Result> hr = (Task<Result>)ad.Method.Invoke(ad.Target, new object[] { context, obj });
                    Result result = await hr;
                    result.Invoke(context.Response);
                }
                else
                {
                    object hr = ad.Method.Invoke(ad.Target, new object[] { context, obj });
                    Result result = hr as Result;
                    result.Invoke(context.Response);
                }
                compelete = true;
            }

            return compelete;
        }

        object ToObject(Type type, NameValueCollection data)
        {
            object obj = Activator.CreateInstance(type);
            var pps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.CanWrite == true);
            foreach (var pp in pps)
            {
                if (data.AllKeys.Any(x => x == pp.Name) == true)
                {
                    try
                    {
                        pp.SetValue(obj, pp.PropertyType.Convert(data[pp.Name]));
                    }
                    catch(Exception ee)
                    {
                        throw new Exception($"{pp.Name} feild format fail");
                    }
                }
            }
            return obj;
        }

        void AddGets<T>(string path, MethodInfo method, object target, Action<Exception> fail)
        {
            var parameters = method.GetParameters();
            ActionData action = new ActionData();
            action.Path = path;
            action.DataType = parameters[1].ParameterType;
            action.Fail = fail;
            action.Method = method;
            action.Target = target;
            this.m_Gets[path] = action;
        }

        Dictionary<string, ActionData> m_Gets = new Dictionary<string, ActionData>();
        public void Get<T>(string path, Func<HttpListenerContext, T, Result> process, Action<Exception> fail = null)where T:class
        {
            this.AddGets<T>(path, process.Method, process.Target, fail);
        }

        public void Get<T>(string path, Func<HttpListenerContext, T, Task<Result>> process, Action<Exception> fail = null) where T : class
        {
            this.AddGets<T>(path, process.Method, process.Target, fail);
        }

        public void Get(string path, Func<HttpListenerContext, NameValueCollection, Result> process, Action<Exception> fail = null)
        {
            this.Get<NameValueCollection>(path, process, fail);
        }

        public void Get(string path, Func<HttpListenerContext, NameValueCollection, Task<Result>> process, Action<Exception> fail = null)
        {
            this.Get<NameValueCollection>(path, process, fail);
        }

        void AddPosts<T>(string path, MethodInfo method, object target, Action<Exception> fail)
        {
            var parameters = method.GetParameters();
            ActionData action = new ActionData();
            action.Path = path;
            action.DataType = parameters[1].ParameterType;
            action.Fail = fail;
            action.Method = method;
            action.Target = target;
            this.m_Posts[path] = action;
        }

        Dictionary<string, ActionData> m_Posts = new Dictionary<string, ActionData>();
        public void Post<T>(string path, Func<HttpListenerContext, T, Result> process, Action<Exception> fail = null)where T:class
        {
            this.AddPosts<T>(path, process.Method, process.Target, fail);
        }

        public void Post<T>(string path, Func<HttpListenerContext, T, Task<Result>> process, Action<Exception> fail = null) where T : class
        {
            this.AddPosts<T>(path, process.Method, process.Target, fail);
        }
    }

    public class ActionData
    {
        public Type DataType { set; get; }
        public string Path { set; get; }
        public Action<Exception> Fail { set; get; }
        public MethodInfo Method { set; get; }
        public object Target { set; get; }
    }

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
        public static JsonReuslt Json(object data) { return new JsonReuslt(data); }
        public static XmlReuslt Xml(object data) { return new XmlReuslt(data); }
        public static StreamResult Stream(Stream data, string contenttype="", bool auto_close=true) { return new StreamResult(data, contenttype, auto_close); }
        public static StringResult String(string data, string contenttype = "text/plain") { return new StringResult(data, contenttype); }
    }

    public class HttpFailResult : StringResult
    {
        int m_Code;
        string m_Description;
        public HttpFailResult(HttpStatusCode code, string data, string content_type)
            :base(data, content_type)
        {
            this.m_Code = (int)code;
            this.m_Description = System.Web.HttpWorkerRequest.GetStatusDescription((int)code);
        }
        public HttpFailResult(int code, string data, string content_type)
            : base(data, content_type)
        {
            this.m_Code = code;
            this.m_Description = System.Web.HttpWorkerRequest.GetStatusDescription((int)code);
        }
        public override void Invoke(HttpListenerResponse resp)
        {
            resp.StatusCode = this.m_Code;
            resp.StatusDescription = this.m_Description;
            base.Invoke(resp);
        }
        public static HttpFailResult BadRequest(string data = "", string contenttype = "text/plain") { return new HttpFailResult(HttpStatusCode.BadRequest, data, contenttype); }
        public static HttpFailResult NotFound(string data = "", string contenttype = "text/plain") { return new HttpFailResult(HttpStatusCode.NotFound, data, contenttype); }
        public static HttpFailResult TooManyRequests(string data = "", string contenttype = "text/plain") { return new HttpFailResult(429, data, contenttype); }
    }

    public class StringResult:Result
    {
        string m_Data;
        string m_ContentType;
        public StringResult(string data, string content_type)
        {
            this.m_ContentType = content_type;
            this.m_Data = data;
        }
        public override void Invoke(HttpListenerResponse resp)
        {
            if(string.IsNullOrEmpty(this.m_Data) == false)
            {
                resp.ContentType = this.m_ContentType;
                resp.Write(this.m_Data);
            }
            resp.OutputStream.Close();
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
                    this.m_ContentType = MimeMapping.GetMimeMapping(fs.Name);
                }
            }
        }
        protected Stream m_Data;
        protected string m_ContentType;
        protected bool m_IsAutoClose;
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
            resp.OutputStream.Close();
        }
    }

    public class JsonReuslt : Result
    {
        public JsonReuslt(object data)
        {
            this.m_Data = data;
        }
        object m_Data;
        
        override public void Invoke(HttpListenerResponse resp)
        {
            resp.ContentType = "application/json";
            JavaScriptSerializer js = new JavaScriptSerializer();
            var json = js.Serialize(this.m_Data);
            resp.Write(json);
            resp.OutputStream.Close();
        }
    }

    public class XmlReuslt : Result
    {
        public XmlReuslt(object data)
        {
            this.m_Data = data;
        }
        object m_Data;

        override public void Invoke(HttpListenerResponse resp)
        {
            resp.ContentType = "application/xml";
            System.Xml.Serialization.XmlSerializer xml = new System.Xml.Serialization.XmlSerializer(this.m_Data.GetType());
            using (MemoryStream mm = new MemoryStream())
            {
                xml.Serialize(mm, this.m_Data);
                mm.Position = 0;
                resp.Write(mm);
            }
            resp.OutputStream.Close();
        }
    }

    public class ServerSentEvent
    {
        HttpListenerResponse m_Resp;
        public string ID { set; get; }

        public ServerSentEvent(HttpListenerResponse resp, string id)
        {
            this.ID = id;
            this.m_Resp = resp;
            this.m_Resp.ContentType = "text/event-stream";
            this.m_Resp.Headers["Connection"] = "keep-alive";
            this.m_Resp.Headers["Cache-Control"] = "no-cache";
        }

        public void WriteMessage(string data)
        {
            
            try
            {
                if (this.m_Resp.OutputStream != null)
                {
                    string msg = $"id:{this.ID}\ndata:{data}\n\n";
                    this.m_Resp.Write(msg, false);
                }
            }
            catch(ObjectDisposedException ee)
            {

            }
        }
        public bool IsClose
        {
            get
            {
                return this.m_Resp == null || this.m_Resp.OutputStream == null;
            }
        }
    }

    public class MultiPatStream
    {
        public MultiPatStream(HttpListenerResponse resp, string bondary= "--myboundary")
        {
            this.m_Resp = resp;
            this.Bondary = bondary;
            this.m_Resp.ContentType = $"multipart/x-mixed-replace;boundary={this.Bondary}";
            
        }
        HttpListenerResponse m_Resp;
        public string Bondary { set; get; } = "--myboundary";
        public void Write(Stream data, string content_type)
        {
            byte[] buf = new byte[8192];
            long length = data.Length - data.Position;
            this.m_Resp.Write($"{Bondary}\r\n", false);
            this.m_Resp.Write($"Content-Type:{content_type}\r\n", false);
            this.m_Resp.Write($"Content-Length:{length}\r\n\r\n", false);

            this.m_Resp.Write(data, false);
            this.m_Resp.Write("\r\n", false);
        }

        public void Write(FileStream data, string content_type="")
        {
            byte[] buf = new byte[8192];

            long length = data.Length - data.Position;

            this.m_Resp.Write($"{Bondary}\r\n", false);
            this.m_Resp.Write($"Content-Type:{MimeMapping.GetMimeMapping(data.Name)}\r\n", false);
            this.m_Resp.Write($"Content-Length:{length}\r\n\r\n", false);

            this.m_Resp.Write(data, false);
            this.m_Resp.Write("\r\n", false);
        }
    }
} 

namespace QSoft.Server.Http.Extention
{
    static public class HttpListenerRequestEx
    {
        public static string ReadString(this HttpListenerRequest src)
        {
            string dst = "";
            if(src.HasEntityBody == true && src.ContentLength64>0)
            {
                System.IO.StreamReader reader = new System.IO.StreamReader(src.InputStream, src.ContentEncoding);
                dst = reader.ReadToEnd();
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

    public static class NameValueCollectionEx
    {
        public static T Get<T>(this NameValueCollection src, string data)
        {
            var vv = src[data];
            return (T)typeof(T).Convert(data);
        }
    }

    public static class TypeEx
    {
        public static object Convert(this Type src, string data)
        {
            object dst = null;
            switch (Type.GetTypeCode(src))
            {
                case TypeCode.Decimal:
                    {
                        dst = Decimal.Parse(data);
                    }
                    break;
                case TypeCode.Boolean:
                    {
                        dst = bool.Parse(data);
                    }
                    break;
                case TypeCode.Byte:
                    {
                        dst = Byte.Parse(data);
                    }
                    break;
                case TypeCode.Char:
                    {
                        dst = Char.Parse(data);
                    }
                    break;
                case TypeCode.SByte:
                    {
                        dst = sbyte.Parse(data);
                    }
                    break;
                case TypeCode.Single:
                    {
                        dst = float.Parse(data);
                    }
                    break;
                case TypeCode.Double:
                    {
                        dst = double.Parse(data);
                    }
                    break;
                case TypeCode.String:
                    {
                        dst = data;
                    }
                    break;
                case TypeCode.Int16:
                    {
                        dst = Int16.Parse(data);
                    }
                    break;
                case TypeCode.Int32:
                    {
                        dst = Int32.Parse(data);
                    }
                    break;
                case TypeCode.Int64:
                    {
                        dst = Int64.Parse(data);
                    }
                    break;
                case TypeCode.UInt16:
                    {
                        dst = UInt16.Parse(data);
                    }
                    break;
                case TypeCode.UInt32:
                    {
                        dst = UInt32.Parse(data);
                    }
                    break;
                case TypeCode.UInt64:
                    {
                        dst = UInt64.Parse(data);
                    }
                    break;
                case TypeCode.DateTime:
                    {
                        dst = DateTime.Parse(data);
                    }
                    break;
            }
            return dst;
        }
    }

    public class HttpMultipartFormParser
    {
        private string boundary;
        private byte[] _boundary;
        private byte[] _data;
        private int _length;
        private int _lineLength = -1;
        private int _lineStart = -1;
        private int _pos;
        private bool _lastBoundaryFound;
        private string _partContentType;
        private int _partDataLength = -1;
        private int _partDataStart = -1;
        private string _partFilename;
        private string _partName;
        private Encoding _encoding;

        public HttpMultipartFormParser(HttpListenerRequest request, Encoding encoding)
        {
            this._encoding = encoding;
            //Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW
            Regex regex = new Regex("boundary=(.*)$");

            Match match = regex.Match(request.ContentType);

            if (match.Success)
            {
                boundary = match.Groups[1].Value;
                _boundary = _encoding.GetBytes("--" + boundary);
            }

            Stream input = request.InputStream;

            //將上傳檔案儲存到記憶體
            BufferedStream br = new BufferedStream(input);

            MemoryStream ms = new MemoryStream();

            byte[] buffer = new byte[4096];

            int len = 0;

            while ((len = br.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, len);
            }

            _data = ms.ToArray();

            _length = _data.Length;

            ms.Close();
        }

        /// <summary>
        /// 獲取每一行資料
        /// </summary>
        /// <returns></returns>
        private bool GetNextLine()
        {
            int num = this._pos;

            this._lineStart = -1;

            while (num < this._length)
            {
                if (this._data[num] == 10)
                { // '\n'
                    this._lineStart = this._pos;
                    this._lineLength = num - this._pos;
                    this._pos = num + 1;

                    // ignore \r
                    if ((this._lineLength > 0) && (this._data[num - 1] == 13))
                    {
                        this._lineLength--;
                    }

                    break;
                }

                if (++num == this._length)
                {
                    this._lineStart = this._pos;
                    this._lineLength = num - this._pos;
                    this._pos = this._length;
                }
            }

            return (this._lineStart >= 0);
        }

        /// <summary>
        /// 當前行是否是分隔符行
        /// </summary>
        /// <returns></returns>
        private bool AtBoundaryLine()
        {
            int length = this._boundary.Length;

            if ((this._lineLength != length) && (this._lineLength != (length + 2)))
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                if (this._data[this._lineStart + i] != this._boundary[i])
                {
                    return false;
                }
            }

            if (this._lineLength != length)
            {
                // last boundary line? (has to end with "--")
                if ((this._data[this._lineStart + length] != 0x2d) || (this._data[(this._lineStart + length) + 1] != 0x2d))
                {
                    return false;
                }

                this._lastBoundaryFound = true;
            }

            return true;
        }

        /// <summary>
        /// 是否解析完畢
        /// </summary>
        /// <returns></returns>
        private bool AtEndOfData()
        {
            if (this._pos < this._length)
            {
                return this._lastBoundaryFound;
            }

            return true;
        }

        /// <summary>
        /// 從Content-Disposition:行抽取""中的內容
        /// </summary>
        /// <param name="l">行內容</param>
        /// <param name="pos"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private string ExtractValueFromContentDispositionHeader(string l, int pos, string name)
        {
            String pattern = name + "=\"";

            //:所在行位置+1
            int i1 = CultureInfo.InvariantCulture.CompareInfo.IndexOf(l, pattern, pos, CompareOptions.IgnoreCase);

            if (i1 < 0)
                return null;
            i1 += pattern.Length;

            int i2 = l.IndexOf('"', i1);
            if (i2 < 0)
                return null;
            if (i2 == i1)
                return String.Empty;

            return l.Substring(i1, i2 - i1);
        }

        /// <summary>
        /// 讀取頭部資訊
        /// </summary>
        private void ParsePartHeaders()
        {
            _partName = null;
            _partFilename = null;
            _partContentType = null;

            while (GetNextLine())
            {
                if (_lineLength == 0)
                    break;  // empty line signals end of headers ->\r\n

                // get line as String 
                byte[] lineBytes = new byte[_lineLength];

                Array.Copy(_data, _lineStart, lineBytes, 0, _lineLength);

                String line = _encoding.GetString(lineBytes);

                // parse into header and value
                int ic = line.IndexOf(':');
                if (ic < 0)
                    continue;   // not a header

                // remeber header 
                String header = line.Substring(0, ic);

                if (header.Equals("Content-Disposition"))
                {
                    // parse name and filename
                    _partName = ExtractValueFromContentDispositionHeader(line, ic + 1, "name");
                    _partFilename = ExtractValueFromContentDispositionHeader(line, ic + 1, "filename");
                }
                else if (header.Equals("Content-Type"))
                {
                    _partContentType = line.Substring(ic + 1).Trim();
                }
            }
        }

        /// <summary>
        /// 處理資料部分
        /// </summary>
        private void ParsePartData()
        {
            _partDataStart = _pos;
            _partDataLength = -1;

            while (GetNextLine())
            {
                if (AtBoundaryLine())
                {
                    // calc length: adjust to exclude [\r]\n before the separator
                    int iEnd = _lineStart - 1;
                    if (_data[iEnd] == 10)   // \n 
                        iEnd--;
                    if (_data[iEnd] == 13)   // \r 
                        iEnd--;

                    _partDataLength = iEnd - _partDataStart + 1;
                    break;
                }
            }
        }

        /// <summary>
        /// 解析資料為物件列表
        /// </summary>
        /// <returns></returns>
        public List<MultipartFormItem> ParseIntoElementList()
        {
            List<MultipartFormItem> itemList = new List<MultipartFormItem>();

            while (GetNextLine())
            {
                if (AtBoundaryLine())
                    break;
            }

            if (AtEndOfData())
                return itemList;

            do
            {
                // Parse current part's headers 
                ParsePartHeaders();

                if (AtEndOfData())
                    break;          // cannot stop after headers

                // Parse current part's data
                ParsePartData();

                if (_partDataLength == -1)
                    break;          // ending boundary not found

                // Remember the current part (if named)
                if (_partName != null)
                {
                    MultipartFormItem item = new MultipartFormItem();
                    item.Name = _partName;
                    item.Data = new byte[_partDataLength];

                    Buffer.BlockCopy(_data, _partDataStart, item.Data, 0, _partDataLength);

                    item.ContentType = _partContentType;

                    if (item.ContentType != null)
                    {
                        item.ItemType = FormItemType.File;
                    }

                    itemList.Add(item);
                }
            }
            while (!AtEndOfData());

            return itemList;
        }

        
    }
    public class MultipartFormItem
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public byte[] Data { get; set; }
        public string ContentType { get; set; }
        public FormItemType ItemType { get; set; }

        public override string ToString()
        {
            if (ItemType == FormItemType.File)
            {
                return Name + "=file[" + FileName + "][" + Data.Length + "]";
            }
            else
            {
                //if (encoding == null) encoding = Encoding.UTF8;
                //return encoding.GetString(item.Data);

                return $"{ this.Name}={ Encoding.UTF8.GetString(this.Data)}";
                //return Name + "=" + this.GetDataAsString();
            }
        }
    }

    public enum FormItemType
    {
        Text,
        File
    }
}

