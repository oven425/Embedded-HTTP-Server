using QNetwork.Http.Server.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QNetwork.Http.Server
{
    public class CQRouterData
    {
        public LifeTypes LifeType { set; get; }
        public string Url { set; get; }
        public Type Service { set; get; }
        public MethodInfo Method { set; get; }
        public CQRouterData()
        {
            this.LifeType = LifeTypes.Transient;
        }

        public static List<CQRouterData> CreateRouterData(object data)
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
                        rr.Service = type;
                        rr.Method = methods[i];
                        rrs.Add(rr);
                        //service_method.LocalPath = 
                    }
                }
            }
            return rrs;
        }
    }
}
