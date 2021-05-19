using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                        Task.Run(async () =>
                        {
                            bool compelete = false;

                            System.Diagnostics.Trace.WriteLine($"usercount:{UserCount}");
                            try
                            {
                                var acs = this.m_Actions.Where(x => x.Setting.Method == context.Request.HttpMethod && x.Setting.Path == context.Request.Url.LocalPath);
                                if (context.Request.HttpMethod.ToUpperInvariant() == "GET")
                                {
                                    var pas = acs.First().Method.GetParameters();
                                    var ppps = pas.Select(x => new { x.Name, x.ParameterType });
                                    foreach(var oo in ppps)
                                    {

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
