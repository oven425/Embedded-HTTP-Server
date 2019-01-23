using QNetwork.Http.Server.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace QNetwork.Http.Server.Router
{
    public class CQRouterData
    {
        public LifeTypes LifeType { set; get; }
        public string Url { set; get; }
        public IQHttpService Service{set;get;}
        public MethodInfo Method { set; get; }
        public int UseLimit { set; get; }
        public int CurrentUse { set; get; }
        object m_UseLock = new object();
        public CQRouterData()
        {
            this.LifeType = LifeTypes.Transient;
        }

        public bool Enter()
        {
            bool result = false;
            Monitor.Enter(this.m_UseLock);
            if(this.CurrentUse<=this.UseLimit)
            {
                this.CurrentUse = this.CurrentUse + 1;
                result = true;
            }
            Monitor.Exit(this.m_UseLock);
            return result;
        }

        public bool Exit()
        {
            Monitor.Enter(this.m_UseLock);
            this.CurrentUse = this.CurrentUse - 1;
            if(this.CurrentUse < 0)
            {
                this.CurrentUse = 0;
            }
            Monitor.Exit(this.m_UseLock);
            return true;
        }

        public static List<CQRouterData> CreateRouterData(IQHttpService data)
        {
            List<CQRouterData> rrs = new List<CQRouterData>();
            Type type = data.GetType();
            object[] roots = type.GetCustomAttributes(typeof(CQServiceRoot), true);
            LifeTypes lifetype = LifeTypes.Transient;
            string root_path = "";
            CQServiceRoot root = (CQServiceRoot)roots.FirstOrDefault(x => x.GetType() == typeof(CQServiceRoot));
            if (root != null)
            {
                lifetype = root.LifeType;
                root_path = root.Root;
            }
            MethodInfo[] methods = type.GetMethods();
            for(int i=0; i<methods.Length; i++)
            {
                object[] attrs = methods[i].GetCustomAttributes(typeof(CQServiceMethod), true);
                if(attrs.Length > 0)
                {
                    CQServiceMethod service_method = attrs[0] as CQServiceMethod;
                    if(service_method != null)
                    {
                        CQRouterData rr = new CQRouterData();
                        rr.LifeType = lifetype;
                        rr.Url = string.Format("{0}{1}", root_path, service_method.LocalPath);
                        rr.Service = data;
                        rr.Method = methods[i];
                        rr.UseLimit = service_method.UseLimit;
                        rrs.Add(rr);
                    }
                }
            }
            return rrs;
        }
    }
}
