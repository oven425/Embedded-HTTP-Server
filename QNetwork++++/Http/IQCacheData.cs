using System;
using System.Collections.Generic;
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
        object m_CachesLock = new object();
        public Dictionary<string, CQCacheBase> Caches { protected set; get; }

        public CQCacheManager()
        {
            this.NickName = "default";
            this.Caches = new Dictionary<string, CQCacheBase>();
        }

        virtual public T Get<T>(string id) where T : CQCacheBase, new()
        {
            Monitor.Enter(this.m_CachesLock);
            T aa = null;
            if (this.Caches.ContainsKey(id) == true)
            {
                aa = this.Caches[id] as T;
            }
            else
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
            
            Monitor.Exit(this.m_CachesLock);
            return aa;
        }




        //virtual public T Create<T>() where T : CQCacheBase, new()
        //{
        //    Monitor.Enter(this.m_CachesLock);
        //    T aa = new T();
        //    this.Caches.Add(aa.ID, aa);
        //    Monitor.Exit(this.m_CachesLock);
        //    return aa;
        //}

        //public T Get<T>(string id)
        //{
        //    T aa = default(T);

        //    return aa;
        //}

        virtual public bool TimeOut()
        {
            bool result = true;
            Monitor.Enter(this.m_CachesLock);
            for(int i=0; i<this.Caches.Count; i++)
            {

            }

            Monitor.Exit(this.m_CachesLock);

            return result;
        }
    }
}
