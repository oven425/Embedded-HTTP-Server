using QNetwork.Http.Server.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace WPF_Server_Http
{
    public class CQCacheManager_Test1:CQCacheManager
    {
        int m_ID = 1;
        public override T Get<T>(string id, bool not_exist_build)
        {
            T aa = null;
            Monitor.Enter(this.m_CachesLock);
            if (this.Caches.ContainsKey(id) == true)
            {
                aa = this.Caches[id] as T;
            }
            else
            {
                if(not_exist_build == true)
                {
                    aa = this.Create<T>((this.m_ID++).ToString());
                    this.Caches.Add(aa.ID, aa);
                }
            }
            Monitor.Exit(this.m_CachesLock);
            return aa;
        }
    }

    public class CQCache_Test:CQCacheBase
    {
        public CQCache_Test()
        {

        }

        
    }

    public class CQCache1 : CQCacheBase
    {
        public CQCache1()
        {

        }

        public CQCache1(string id)
            : base(id)
        {

        }

        public int Count { set; get; }

        public override bool IsTimeOut(TimeSpan timeout)
        {
            return base.IsTimeOut(timeout);
        }
    }
}
