using QNetwork.Http.Server.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace QNetwork.Http.Server.Cache
{
    public interface IQCache: IDisposable
    {
        string ID {  get; }
        bool IsTimeOut(TimeSpan timeout);
        bool Keeping(TimeSpan time);
    }
    abstract public class CQCacheBase: IQCache
    {
        public CQCacheBase()
        {
            this.m_BeginTime = DateTime.Now;
            this.ID = Guid.NewGuid().ToString();
        }

        public CQCacheBase(string id)
        {
            this.m_BeginTime = DateTime.Now;
            this.ID = id;
        }

        public string ID { internal set; get; }
        public virtual bool IsTimeOut(TimeSpan timeout)
        {
            return true;
        }

        protected DateTime m_BeginTime;



        virtual public bool Keeping(TimeSpan time)
        {
            this.m_BeginTime = this.m_BeginTime + time;
            return true;
        }

        abstract public void Dispose();
    }

    

    //public class CQCacheManager
    //{
    //    public string NickName { set; get; }
    //    protected object m_CachesLock = new object();
    //    public Dictionary<string, CQCacheBase> Caches { protected set; get; }
    //    public IQHttpServer_Log Logger { set; get; }
    //    public CQCacheManager()
    //    {
    //        this.NickName = "default";
    //        this.Caches = new Dictionary<string, CQCacheBase>();
    //    }

    //    virtual public T Create<T>(string id) where T : CQCacheBase, new()
    //    {
    //        T aa = null;
    //        aa = new T();
    //        if (string.IsNullOrEmpty(id) == false)
    //        {
    //            aa.ID = id;
    //        }
    //        if(this.Caches.ContainsKey(id) == true)
    //        {
    //            this.Caches[id] = aa;
    //        }
    //        else
    //        {
    //            this.Caches.Add(aa.ID, aa);
    //        }
    //        return aa;
    //    }

    //    virtual public T Get<T>(string id, bool not_exist_build) where T : CQCacheBase, new()
    //    {
    //        Monitor.Enter(this.m_CachesLock);
    //        T aa = null;
    //        if (this.Caches.ContainsKey(id) == true)
    //        {
    //            aa = this.Caches[id] as T;
    //        }
    //        else
    //        {
    //            if (not_exist_build == true)
    //            {
    //                if (string.IsNullOrEmpty(id) == true)
    //                {
    //                    aa = new T();
    //                }
    //                else
    //                {
    //                    aa = new T();
    //                    aa.ID = id;
    //                }
    //                this.Caches.Add(aa.ID, aa);
    //            }
    //        }

    //        Monitor.Exit(this.m_CachesLock);
    //        return aa;
    //    }

    //    virtual public bool TimeOut()
    //    {
    //        bool result = true;
           
    //        List<CQCacheBase> tt = this.Caches.Values.Where(x => x.IsTimeOut(TimeSpan.FromSeconds(10)) == true).ToList();
            
    //        for (int i = 0; i < tt.Count; i++)
    //        {
    //            tt[i].Dispose();
    //            Monitor.Enter(this.m_CachesLock);
    //            if(this.Logger != null)
    //            {
    //                this.Logger.LogCache(LogStates_Cache.DestoryCache, DateTime.Now, this.NickName, tt[i].ID, "");
    //            }
    //            this.Caches.Remove(tt[i].ID);
    //            Monitor.Exit(this.m_CachesLock);
    //        }

    //        return result;
    //    }
    //}
}
