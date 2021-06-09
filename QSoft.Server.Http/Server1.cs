using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using QSoft.Server.Http1.Extension;

namespace QSoft.Server.Http1
{
    public class Server1
    {
        void Add(object obj)
        {
            Dictionary<string, List<Action>> actions = new Dictionary<string, List<Action>>();
            var methods = obj.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | BindingFlags.InvokeMethod).Where(x => x.IsGenericMethod == false);
            foreach (var oo in methods)
            {
                var attrb = oo.GetCustomAttribute<HttpMethodSetting>();
                if (attrb != null && attrb.Ignore==false)
                {
                    Action ac = new Action();
                    ac.Method = oo;
                    ac.Target = obj;
                    ac.Setting = attrb;
                    if (string.IsNullOrWhiteSpace(ac.Setting.Path) == true)
                    {
                        ac.Setting.Path = $"/{oo.Name}";
                    }
                    if (actions.ContainsKey(ac.Setting.Method) == false)
                    {
                        actions.Add(ac.Setting.Method, new List<Action>());
                    }
                    actions[ac.Setting.Method].Add(ac);
                   
                    ac.Params = ac.Method.GetParameters().ToDictionary(x => x.Name, x => x.ParameterType);
                    ac.ContextCount = ac.Params.Values.Count(x => x == typeof(HttpListenerContext));
                    ac.Args = new object[ac.Params.Count];
                }
            }
            this.m_Actions.AddRange(actions.SelectMany(x => x.Value));
        }

        List<Action> m_Actions = new List<Action>();
        public DirectoryInfo Statics { private set; get; }
        HttpListener m_Listener = new HttpListener();
        public ReturnTypes DefaultReturnType { private set; get; }
        public void Strat(string ip, int port, List<object> actions, DirectoryInfo statics, ReturnTypes return_default = ReturnTypes.Json)
        {
            this.DefaultReturnType = return_default;
            this.Statics = statics;
            this.m_Listener.Prefixes.Add($"http://{ip}:{port}/");
            this.m_Listener.Start();
            int UserCount = 0;
            int maxount = 10;
            foreach(var action in actions)
            {
                this.Add(action);
            }


            //Task.Run(() =>
            Task.Factory.StartNew(()=>
            {
                while (true)
                {
                    var context = this.m_Listener.GetContext();
                    Console.WriteLine($"{context.Request.HttpMethod} {context.Request.Url.LocalPath}");

                    if (Interlocked.CompareExchange(ref UserCount, maxount, maxount) >= maxount)
                    {
                        context.Response.ToManyRequest();
                    }
                    else
                    {
                        Interlocked.Increment(ref UserCount);
                        try
                        {
                            Task.Run(() =>
                            {
                                System.Diagnostics.Trace.WriteLine($"usercount:{UserCount} {DateTime.Now.ToString("HH:mm:ss")}");
                                try
                                {
                                    switch (context.Request.HttpMethod)
                                    {
                                        case "GET":
                                            {
                                                this.Process_Get(context);
                                            }
                                            break;
                                        case "POST":
                                            {
                                                this.Process_Post(context);
                                            }
                                            break;
                                    }


                                }
                                catch (Exception ee)
                                {
                                    Console.WriteLine(ee.Message);
                                    Console.WriteLine(ee.StackTrace);
                                }
                                finally
                                {
                                    Interlocked.Decrement(ref UserCount);
                                }

                            });
                        }
                        catch(Exception ee)
                        {
                            Console.WriteLine(ee.Message);
                            Console.WriteLine(ee.StackTrace);
                        }
                    }
                }
            });
        }

        void Process_Get(HttpListenerContext context)
        {
            var actions = this.m_Actions.Where(x => x.Setting.Path == context.Request.Url.LocalPath && x.Setting.Method.Equals(context.Request.HttpMethod, StringComparison.OrdinalIgnoreCase));
            if (actions.Count() == 0)
            {
                if (this.Statics != null&& File.Exists(this.Statics.FullName + context.Request.Url.LocalPath) == true)
                {
                    context.Response.Write(File.OpenRead(this.Statics.FullName + context.Request.Url.LocalPath));
                }
                else
                {
                    context.Response.NotFind();
                }
            }
            else
            {
               
                Dictionary<string, string> query = context.Request.QueryString.ToDictionary();
                foreach (var action in actions)
                {
                    Array.Clear(action.Args, 0, action.Args.Length);
                    var pars = action.Method.GetParameters();
                    for (int i = 0; i < pars.Length; i++)
                    {
                        if (pars[i].ParameterType == typeof(HttpListenerContext))
                        {
                            action.Args[i] = context;
                        }
                        else if (Type.GetTypeCode(pars[i].ParameterType) == TypeCode.Object)
                        {
                            action.Args[i] = context.Request.QueryString.Deserialize(pars[i].ParameterType);
                        }
                        else if (query.ContainsKey(pars[i].Name) == true)
                        {
                            object arg = null;
                            if (pars[i].ParameterType.TryParse(query[pars[i].Name], out arg) == true)
                            {
                                action.Args[i] = arg;
                            }
                        }
                    }
                }
                //var ac1 = actions.OrderBy(x => x.Args.Count(y => y == null));
                var ac1 = actions.Where(x => x.Args.All(y => y != null)).OrderByDescending(x => x.Args.Length);
                this.Send_Resp(context, ac1.FirstOrDefault());
            }
        }

        void Send_Resp(HttpListenerContext context, Action ac)
        {
            if(ac== null)
            {
                context.Response.BadRequest();
                return;
            }
            try
            {
                object hr = null;
                if (ac.Method.ReturnType.IsGenericType == true && ac.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    Task<object> taskhr = (Task<object>)ac.Method.Invoke(ac.Target, ac.Args);
                    hr = taskhr.Result;
                }
                else
                {
                    hr = ac.Method.Invoke(ac.Target, ac.Args);
                }

                if (hr != null)
                {
                    ReturnTypes rt = ac.Setting.ReturnType;
                    if (rt == ReturnTypes.Default) rt = this.DefaultReturnType;
                    switch (rt)
                    {
                        case ReturnTypes.Json:
                            {
                                context.Response.WriteJson(hr);
                            }
                            break;
                        case ReturnTypes.Xml:
                            {
                                context.Response.WriteXml(hr);
                            }
                            break;
                    }
                }
            }
            catch (TargetInvocationException ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                if (ee.InnerException != null)
                {
                    System.Diagnostics.Trace.WriteLine(ee.InnerException.Message);
                    System.Diagnostics.Trace.WriteLine(ee.InnerException.StackTrace);
                }
            }
            catch (Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
            }
        }

        void Process_Post(HttpListenerContext context)
        {
            var actions = this.m_Actions.Where(x => x.Setting.Path == context.Request.Url.LocalPath && x.Setting.Method.Equals(context.Request.HttpMethod, StringComparison.OrdinalIgnoreCase));
            if(actions.Count() == 0)
            {
                context.Response.NotFind();
                return;
            }
            if (context.Request.ContentLength64 > 0)
            {
                if (context.Request.ContentType == "application/xml")
                {
                    MemoryStream mm = new MemoryStream();
                    try
                    {
                        context.Request.InputStream.CopyTo(mm);
                        foreach (var action in actions)
                        {
                            Array.Clear(action.Args, 0, action.Args.Length);
                            var pars = action.Method.GetParameters();
                            for (int i = 0; i < pars.Length; i++)
                            {
                                if (pars[i].ParameterType == typeof(HttpListenerContext))
                                {
                                    action.Args[i] = context;
                                }
                                else if (Type.GetTypeCode(pars[i].ParameterType) == TypeCode.Object)
                                {
                                    mm.Position = 0;
                                    System.Xml.Serialization.XmlSerializer xml = new System.Xml.Serialization.XmlSerializer(pars[i].ParameterType);
                                    action.Args[i] = xml.Deserialize(mm);
                                }
                            }
                        }
                    }
                    catch(Exception ee)
                    {
                        System.Diagnostics.Trace.WriteLine(ee.Message);
                        System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                    }
                    finally
                    {
                        mm.Close();
                        mm.Dispose();
                        mm = null;
                    }
                }
                else if (context.Request.ContentType == "application/json")
                {
                    try
                    {
                        string data_str = context.Request.ReadString();
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        foreach (var action in actions)
                        {
                            Array.Clear(action.Args, 0, action.Args.Length);
                            var pars = action.Method.GetParameters();
                            for (int i = 0; i < pars.Length; i++)
                            {
                                if (pars[i].ParameterType == typeof(HttpListenerContext))
                                {
                                    action.Args[i] = context;
                                }
                                else if (Type.GetTypeCode(pars[i].ParameterType) == TypeCode.Object)
                                {
                                    action.Args[i] = js.Deserialize(data_str, pars[i].ParameterType);
                                }
                            }
                        }
                    }
                    catch(Exception ee)
                    {
                        System.Diagnostics.Trace.WriteLine(ee.Message);
                        System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                    }
                }
                else if (context.Request.ContentType == "application/x-www-form-urlencoded")
                {
                    try
                    {
                        string data_str = context.Request.ReadString();
                        data_str = HttpUtility.UrlDecode(data_str);
                        Dictionary<string, string> query = HttpUtility.ParseQueryString(data_str).ToDictionary();
                        foreach (var action in actions)
                        {
                            Array.Clear(action.Args, 0, action.Args.Length);
                            var pars = action.Method.GetParameters();
                            for (int i = 0; i < pars.Length; i++)
                            {
                                if (pars[i].ParameterType == typeof(HttpListenerContext))
                                {
                                    action.Args[i] = context;
                                }
                                else if (Type.GetTypeCode(pars[i].ParameterType) == TypeCode.Object)
                                {
                                    action.Args[i] = context.Request.QueryString.Deserialize(pars[i].ParameterType);
                                }
                                else if (query.ContainsKey(pars[i].Name) == true)
                                {
                                    object arg = null;
                                    if (pars[i].ParameterType.TryParse(query[pars[i].Name], out arg) == true)
                                    {
                                        action.Args[i] = arg;
                                    }

                                }
                            }
                        }
                    }
                    catch(Exception ee)
                    {
                        System.Diagnostics.Trace.WriteLine(ee.Message);
                        System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                        
                    }
                }
            }
            var ac1 = actions.Where(x => x.Args.All(y => y != null)).OrderByDescending(x => x.Args.Length);
            this.Send_Resp(context, ac1.FirstOrDefault());
        }
    }

    public class Action
    {
        public HttpMethodSetting Setting { set; get; }
        public MethodInfo Method { set; get; }
        public object Target { set; get; }
        public int ContextCount { set; get; }
        public Dictionary<string, Type> Params { set; get; } = new Dictionary<string, Type>();
        public object[] Args { set; get; }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class HttpRouter:Attribute
    {
        public string Path { set; get; } = "/";
        public bool UseClassName { set; get; }
    }

    public enum ReturnTypes
    {
        Default,
        Xml,
        Json,
        Custom
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple =false)]
    public class HttpMethodSetting:Attribute
    {
        public string Method { set; get; } = "GET";
        public string Path { set; get; }
        public ReturnTypes ReturnType { set; get; } = ReturnTypes.Default;
        public bool Ignore { set; get; }

    }

    public class ServerSentEvent
    {
        HttpListenerResponse m_Resp;
        public string ID { set; get; }
        JavaScriptSerializer m_Json = new JavaScriptSerializer();
        bool m_IsClosed = false;
        public ServerSentEvent(HttpListenerResponse resp, string id)
        {
            this.ID = id;
            this.m_Resp = resp;
            this.m_Resp.ContentType = "text/event-stream";
            this.m_Resp.Headers["Connection"] = "keep-alive";
            this.m_Resp.Headers["Cache-Control"] = "no-cache";
        }

        public void Close()
        {
            try
            {
                if (this.IsClose == false)
                {
                    this.m_Resp.OutputStream.Close();
                }
            }
            catch(Exception ee)
            {
                this.m_IsClosed = true;
                Console.WriteLine(ee.Message);
                Console.WriteLine(ee.StackTrace);
            }
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
            catch (ObjectDisposedException ee)
            {
                this.m_IsClosed = true;
                Console.WriteLine(ee.Message);
                Console.WriteLine(ee.StackTrace);
            }
            catch(Exception ee)
            {
                this.m_IsClosed = true;
                Console.WriteLine(ee.Message);
                Console.WriteLine(ee.StackTrace);
            }
        }

        public void WriteJson(object data)
        {
            if(this.IsClose==true)
            {
                return;
            }

            string json_str = this.m_Json.Serialize(data);
            try
            {
                if (this.m_Resp.OutputStream != null)
                {
                    string msg = $"id:{this.ID}\ndata:{json_str}\n\n";
                    this.m_Resp.Write(msg, false);
                }
            }
            catch (ObjectDisposedException ee)
            {
                this.m_IsClosed = true;
                Console.WriteLine(ee.Message);
                Console.WriteLine(ee.StackTrace);
            }
            catch(Exception ee)
            {
                this.m_IsClosed = true;
                Console.WriteLine(ee.Message);
                Console.WriteLine(ee.StackTrace);
            }
        }
        [Obsolete("There is a problem with the function, it is recommended not to use it")]
        async public Task WriteJsonAsync(object data)
        {
            if (this.m_Json == null)
            {
                this.m_Json = new JavaScriptSerializer();
            }
            //this.m_Json = new JavaScriptSerializer();
            if(this.IsClose == true)
            {
                return;
            }
            string json_str = this.m_Json.Serialize(data);
            try
            {
                string msg = $"id:{this.ID}\ndata:{json_str}\n\n";
                await this.m_Resp.WriteAsync(msg, false);
            }
            catch (ObjectDisposedException ee)
            {
                this.m_IsClosed = true;
                Console.WriteLine(ee.Message);
                Console.WriteLine(ee.StackTrace);
            }
            catch(Exception ee)
            {
                this.m_IsClosed = true;
                Console.WriteLine(ee.Message);
                Console.WriteLine(ee.StackTrace);
            }
        }
        public bool IsClose
        {
            get
            {
                if(this.m_IsClosed == false)
                {
                    try
                    {
                        var len = this.m_Resp.OutputStream!=null;
                        //Console.WriteLine($"this.m_Resp.OutputStream:{this.m_Resp.OutputStream}");
                    }
                    catch(Exception ee)
                    {
                        this.m_IsClosed = true;
                        Console.WriteLine(ee.Message);
                        Console.WriteLine(ee.StackTrace);
                    }
                }
                return this.m_IsClosed;
            }
        }
    }

    public class MultiPatStream
    {
        public MultiPatStream(HttpListenerResponse resp, string bondary = "--myboundary")
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

        public void Write(FileStream data, string content_type = "")
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

namespace QSoft.Server.Http1.Extension
{
    public static class NameValueCollectionEx
    {
        public static object Deserialize(this NameValueCollection src, Type type)
        {
            object obj = null;
            var pps = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.CanWrite == true);
            var excp = pps.Select(x => x.Name).Except(src.AllKeys);
            if (excp.Count() == pps.Count())
            {
                
            }
            else
            {
                obj = Activator.CreateInstance(type);
                foreach (var pp in pps)
                {
                    if (src.AllKeys.Any(x => x == pp.Name) == true)
                    {
                        object dst;
                        if (pp.PropertyType.TryParse(src[pp.Name], out dst) == true)
                        {
                            pp.SetValue(obj, dst);
                        }
                    }
                }
            }
            return obj;
        }

        public static Dictionary<string,string> ToDictionary(this NameValueCollection src)
        {
            Dictionary<string, string> query = new Dictionary<string, string>();
            foreach (var oo in src.AllKeys)
            {
                query[oo] = src[oo];
            }
            return query;
        }
    }
    static public class HttpListenerRequestEx
    {
        public static string ReadString(this HttpListenerRequest src)
        {
            string dst = "";
            if (src.HasEntityBody == true && src.ContentLength64 > 0)
            {
                //using (MemoryStream mm = new MemoryStream())
                //{
                //    src.InputStream.CopyTo(mm);
                //    byte[] bb = mm.ToArray();
                //    dst = HttpUtility.UrlDecode(bb, src.ContentEncoding);
                //    var fixedResult = Uri.EscapeUriString(dst);
                //}

                System.IO.StreamReader reader = new System.IO.StreamReader(src.InputStream, src.ContentEncoding);
                dst = reader.ReadToEnd();
                dst = Uri.UnescapeDataString(dst);
            }
            return dst;
        }
    }
    static public class HttpListenerResponseEx
    {
        public static void BadRequest(this HttpListenerResponse src)
        {
            src.StatusCode = 400;
            src.OutputStream.Close();
        }
        public static void NotFind(this HttpListenerResponse src)
        {
            src.StatusCode = 404;
            src.OutputStream.Close();
        }
        public static void ToManyRequest(this HttpListenerResponse src)
        {
            src.StatusCode = 429;
            src.OutputStream.Close();
        }
        public static void WriteXml(this HttpListenerResponse src, object data)
        {
            if (data == null) return;
            src.ContentType = "application/xml";
            System.Xml.Serialization.XmlSerializer xml = new System.Xml.Serialization.XmlSerializer(data.GetType());
            using (MemoryStream mm = new MemoryStream())
            {
                xml.Serialize(mm, data);
                mm.Position = 0;
                src.Write(mm);
            }
        }
        [Obsolete("There is a problem with the function, it is recommended not to use it")]
        async public static Task WriteXmlAsync(this HttpListenerResponse src, object data)
        {
            if (data == null) return;
            src.ContentType = "application/xml";
            System.Xml.Serialization.XmlSerializer xml = new System.Xml.Serialization.XmlSerializer(data.GetType());
            using (MemoryStream mm = new MemoryStream())
            {
                xml.Serialize(mm, data);
                mm.Position = 0;
                await src.WriteAsync(mm);
            }
        }
        public static void WriteJson(this HttpListenerResponse src, object data)
        {
            if (data == null) return;
            src.ContentType = "application/json";
            JavaScriptSerializer json = new JavaScriptSerializer();
            src.Write(json.Serialize(data));
        }
        [Obsolete("There is a problem with the function, it is recommended not to use it")]
        async public static Task WriteJsonAsync(this HttpListenerResponse src, object data)
        {
            if (data == null) return;
            src.ContentType = "application/json";
            JavaScriptSerializer json = new JavaScriptSerializer();
            await src.WriteAsync(json.Serialize(data));
        }

        public static void Write(this HttpListenerResponse src, string data, string content_type)
        {
            if (string.IsNullOrWhiteSpace(content_type) == false)
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
        [Obsolete("There is a problem with the function, it is recommended not to use it")]
        async public static Task WriteAsync(this HttpListenerResponse src, byte[] data)
        {
            src.ContentLength64 = data.Length;
           await  src.OutputStream.WriteAsync(data, 0, data.Length);
        }

        public static void Write(this HttpListenerResponse src, string data, bool autolength = true)
        {
            byte[] writebuf = Encoding.UTF8.GetBytes(data);
            if (autolength == true)
            {
                src.ContentLength64 = writebuf.Length;
            }
            src.OutputStream.Write(writebuf, 0, writebuf.Length);
        }
        [Obsolete("There is a problem with the function, it is recommended not to use it")]
        async public static Task WriteAsync(this HttpListenerResponse src, string data, bool autolength = true)
        {
            byte[] writebuf = Encoding.UTF8.GetBytes(data);
            if (autolength == true)
            {
                src.ContentLength64 = writebuf.Length;
            }
            await src.OutputStream.WriteAsync(writebuf, 0, writebuf.Length);
        }

        public static void Write(this HttpListenerResponse src, FileStream data, bool autolength = true, bool completeandclose=true)
        {
            byte[] read_buf = new byte[8192];
            if (autolength == true)
            {
                src.ContentLength64 = data.Length - data.Position;
                src.ContentType = MimeMapping.GetMimeMapping(data.Name);
            }
            
            while (true)
            {
                int read_len = data.Read(read_buf, 0, read_buf.Length);
                src.OutputStream.Write(read_buf, 0, read_len);
                if (read_len != read_buf.Length)
                {
                    if(completeandclose == true)
                    {
                        data.Close();
                        data.Dispose();
                    }
                    return;
                }
            }
        }
        [Obsolete("There is a problem with the function, it is recommended not to use it")]
        async public static Task WriteAsync(this HttpListenerResponse src, FileStream data, bool autolength = true, bool completeandclose = true)
        {
            byte[] read_buf = new byte[8192];
            if (autolength == true)
            {
                src.ContentLength64 = data.Length - data.Position;
                src.ContentType = MimeMapping.GetMimeMapping(data.Name);
            }

            while (true)
            {
                int read_len = await data.ReadAsync(read_buf, 0, read_buf.Length);
                await src.OutputStream.WriteAsync(read_buf, 0, read_len);
                if (read_len != read_buf.Length)
                {
                    if (completeandclose == true)
                    {
                        data.Close();
                        data.Dispose();
                    }
                    return;
                }
            }
        }

        public static void Write(this HttpListenerResponse src, Stream data, bool autolength = true, bool autoclose=true)
        {
            byte[] read_buf = new byte[8192];
            if (autolength == true)
            {
                src.ContentLength64 = data.Length - data.Position;
            }

            while (true)
            {
                int read_len = data.Read(read_buf, 0, read_buf.Length);
                src.OutputStream.Write(read_buf, 0, read_len);
                if (read_len != read_buf.Length)
                {
                    if (autoclose == true)
                    {
                        data.Close();
                        data.Dispose();
                    }
                    return;
                }
            }
        }
        [Obsolete("There is a problem with the function, it is recommended not to use it")]
        async public static Task WriteAsync(this HttpListenerResponse src, Stream data, bool autolength = true, bool autoclose = true)
        {
            byte[] read_buf = new byte[8192];
            if (autolength == true)
            {
                src.ContentLength64 = data.Length - data.Position;
            }

            while (true)
            {
                int read_len = await data.ReadAsync(read_buf, 0, read_buf.Length);
                await src.OutputStream.WriteAsync(read_buf, 0, read_len);
                if (read_len != read_buf.Length)
                {
                    if (autoclose == true)
                    {
                        data.Close();
                        data.Dispose();
                    }
                    return;
                }
            }
        }
    }

    public static class TypeEx
    {
        public static bool TryParse(this Type src, string data, out object dst)
        {
            dst = null;
            bool result = true;
            switch(Type.GetTypeCode(src))
            {
                case TypeCode.Boolean:
                    {
                        bool hr;
                        result = bool.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.Char:
                    {
                        Char hr;
                        result = Char.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.SByte:
                    {
                        sbyte hr;
                        result = sbyte.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.Byte:
                    {
                        byte hr;
                        result = byte.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.Int16:
                    {
                        Int16 hr;
                        result = Int16.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.UInt16:
                    {
                        UInt16 hr;
                        result = UInt16.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.Int32:
                    {
                        Int32 hr;
                        result = Int32.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.UInt32:
                    {
                        UInt32 hr;
                        result = UInt32.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.Int64:
                    {
                        Int64 hr;
                        result = Int64.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.UInt64:
                    {
                        UInt64 hr;
                        result = UInt64.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.Double:
                    {
                        Double hr;
                        result = Double.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.Decimal:
                    {
                        Decimal hr;
                        result = Decimal.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.DateTime:
                    {
                        DateTime hr;
                        result = DateTime.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.Single:
                    {
                        Single hr;
                        result = Single.TryParse(data, out hr);
                        dst = hr;
                    }
                    break;
                case TypeCode.String:
                    {
                        dst = data;
                    }
                    break;
                default:
                    {
                        result = false;
                    }
                    break;
            }

            return result;
        }
    }
}
