﻿using System;
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
            int maxount = 5;
            foreach(var action in actions)
            {
                this.Add(action);
            }
            Task.Run(() =>
            {
                while (true)
                {
                    var context = this.m_Listener.GetContext();
                    if (Interlocked.CompareExchange(ref UserCount, maxount, maxount) >= maxount)
                    {
                    }
                    else
                    {
                        Interlocked.Increment(ref UserCount);
                        Task.Run(() =>
                        {
                            bool compelete = false;

                            System.Diagnostics.Trace.WriteLine($"usercount:{UserCount}");
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
                                System.Diagnostics.Trace.WriteLine(ee.Message);
                                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                            }
                            finally
                            {
                                if (compelete == false)
                                {
                                }
                            }
                            Interlocked.Decrement(ref UserCount);
                        });
                    }
                }
            });
        }

        void Process_Get(HttpListenerContext context)
        {
            var actions = this.m_Actions.Where(x => x.Setting.Path == context.Request.Url.LocalPath && x.Setting.Method.Equals(context.Request.HttpMethod, StringComparison.OrdinalIgnoreCase));
            if (actions.Count() == 0)
            {
                if (File.Exists(this.Statics.FullName + context.Request.Url.LocalPath) == true)
                {
                    context.Response.Write(File.OpenRead(this.Statics.FullName + context.Request.Url.LocalPath));
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
                var ac1 = actions.OrderBy(x => x.Args.Count(y => y == null));
                this.Send_Resp(context, ac1.FirstOrDefault());
            }
        }

        void Send_Resp(HttpListenerContext context, Action ac)
        {
            if(ac== null)
            {
                context.Response.StatusCode = 400;
                context.Response.OutputStream.Close();
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
            if (context.Request.ContentLength64 > 0)
            {
                if (context.Request.ContentType == "application/xml")
                {
                    MemoryStream mm = new MemoryStream();
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
                    mm.Close();
                    mm.Dispose();
                }
                else if (context.Request.ContentType == "application/json")
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
                else if (context.Request.ContentType == "application/x-www-form-urlencoded")
                {
                    string data_str = context.Request.ReadString();
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
                
            }
            var ac1 = actions.OrderBy(x => x.Args.Count(y => y == null));
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

    public enum ReturnTypes
    {
        Default,
        Xml,
        Json,
        Custom
    }

    public class HttpMethodSetting:Attribute
    {
        public string Method { set; get; } = "GET";
        public string Path { set; get; }
        public ReturnTypes ReturnType { set; get; } = ReturnTypes.Default;
        public bool Ignore { set; get; }

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
                System.IO.StreamReader reader = new System.IO.StreamReader(src.InputStream, src.ContentEncoding);
                dst = reader.ReadToEnd();
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
        public static void WriteJson(this HttpListenerResponse src, object data)
        {
            if (data == null) return;
            src.ContentType = "application/json";
            JavaScriptSerializer json = new JavaScriptSerializer();
            src.Write(json.Serialize(data));
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

        public static void Write(this HttpListenerResponse src, string data, bool autolength = true)
        {
            byte[] writebuf = Encoding.UTF8.GetBytes(data);
            if (autolength == true)
            {
                src.ContentLength64 = writebuf.Length;
            }
            src.OutputStream.Write(writebuf, 0, writebuf.Length);
        }

        public static void Write(this HttpListenerResponse src, FileStream data, bool autolength = true)
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
