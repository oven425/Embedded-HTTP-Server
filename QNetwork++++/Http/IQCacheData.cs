using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace QNetwork.Http.Server.Cache
{
    public class CQCacheBase
    {
        public CQCacheBase()
        {
            this.ID = Guid.NewGuid().ToString();
        }

        public CQCacheBase(string id)
        {
            this.ID = id;
        }
        public string ID { internal set; get; }
        public virtual bool IsTimeOut(TimeSpan timeout)
        {
            return true;
        }
        public string IsUse { protected set; get; }
    }

    public class CQCacheManager
    {
        public string NickName { set; get; }
        protected object m_CachesLock = new object();
        public Dictionary<string, CQCacheBase> Caches { protected set; get; }

        public CQCacheManager()
        {
            this.NickName = "default";
            this.Caches = new Dictionary<string, CQCacheBase>();
        }

        virtual protected T Create<T>(string id) where T : CQCacheBase, new()
        {
            T aa = null;
            aa = new T();
            aa.ID = id;
            return aa;
        }

        virtual public T Get<T>(string id, bool not_exist_build) where T : CQCacheBase, new()
        {
            Monitor.Enter(this.m_CachesLock);
            T aa = null;
            if (this.Caches.ContainsKey(id) == true)
            {
                aa = this.Caches[id] as T;
            }
            else
            {
                if (not_exist_build == true)
                {
                    if (string.IsNullOrEmpty(id) == true)
                    {
                        aa = new T();
                    }
                    else
                    {
                        aa = new T();
                        aa.ID = id;
                    }
                    this.Caches.Add(aa.ID, aa);
                }
            }

            Monitor.Exit(this.m_CachesLock);
            return aa;
        }

        virtual public bool TimeOut()
        {
            bool result = true;
            Monitor.Enter(this.m_CachesLock);
            List<CQCacheBase> tt = this.Caches.Values.Where(x => x.IsTimeOut(TimeSpan.FromMinutes(1)) == true).ToList();
            for (int i = 0; i < tt.Count; i++)
            {
                this.Caches.Remove(tt[i].ID);
            }

            Monitor.Exit(this.m_CachesLock);

            return result;
        }
    }
}
