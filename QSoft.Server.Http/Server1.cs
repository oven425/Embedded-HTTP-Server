using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QSoft.Server.Http1.Extension;

namespace QSoft.Server.Http1
{
    public class Server1
    {
        public void Add(object obj)
        {
            var methods = obj.GetType().GetMethods(System.Reflection.BindingFlags.Instance| System.Reflection.BindingFlags.Public| BindingFlags.InvokeMethod);
            foreach(var oo in methods)
            {
                var attrb = oo.GetCustomAttribute<HttpMethodSetting>();
                if(attrb != null)
                {
                    Action ac = new Action();
                    ac.Method = oo;
                    ac.Target = obj;
                    ac.Setting = attrb;
                    if(string.IsNullOrWhiteSpace(ac.Setting.Path) == true)
                    {
                        ac.Setting.Path = $"/{oo.Name}";
                    }
                    this.m_Actions.Add(ac);
                }
            }
        }

        List<Action> m_Actions = new List<Action>();

        HttpListener m_Listener = new HttpListener();
        public void Strat(string ip, int port)
        {
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
                                var acs = this.m_Actions.Where(x => x.Setting.Method == context.Request.HttpMethod && x.Setting.Path == context.Request.Url.LocalPath);
                                acs = acs.Where(x=>x.Method.GetParameters().Length == context.Request.QueryString.Count).OrderBy(x => x.Method.GetParameters().Count(y => y.ParameterType == typeof(string)));
                                
                                Dictionary<string, string> query = new Dictionary<string, string>();

                                foreach(var oo in context.Request.QueryString.AllKeys)
                                {
                                    query[oo] = context.Request.QueryString[oo];
                                }
                                
                                foreach (var ac in acs)
                                {
                                    object[] args = new object[query.Count];
                                    var pars = ac.Method.GetParameters();
                                    for(int i=0; i<pars.Length; i++)
                                    {
                                        if(query.ContainsKey(pars[i].Name) == true)
                                        {
                                            object arg = null;
                                            if(pars[i].ParameterType.TryParse(query[pars[i].Name], out arg)==true)
                                            {
                                                args[i] = arg;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    if(args.All(x=>x!=null) == true)
                                    {
                                        try
                                        {
                                            ac.Method.Invoke(ac.Target, args);
                                        }
                                        catch(TargetInvocationException ee)
                                        {
                                            System.Diagnostics.Trace.WriteLine(ee.Message);
                                            System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                                            if(ee.InnerException != null)
                                            {
                                                System.Diagnostics.Trace.WriteLine(ee.InnerException.Message);
                                                System.Diagnostics.Trace.WriteLine(ee.InnerException.StackTrace);
                                            }
                                        }
                                        catch(Exception ee)
                                        {
                                            System.Diagnostics.Trace.WriteLine(ee.Message);
                                            System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                                        }
                                        
                                        break;
                                    }
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

    }

    public class Action
    {
        public HttpMethodSetting Setting { set; get; }
        public MethodInfo Method { set; get; }
        public object Target { set; get; }
    }

    

    public class HttpMethodSetting:Attribute
    {
        public string Method { set; get; } = "GET";
        public string Path { set; get; }
    }


}

namespace QSoft.Server.Http1.Extension
{
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
