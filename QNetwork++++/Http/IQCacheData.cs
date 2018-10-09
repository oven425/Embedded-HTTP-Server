using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QNetwork.Http.Server
{
    public interface IQCacheData
    {
        string ID { get; }
        bool IsTimeOut(TimeSpan timeout);
        bool IsUse { set; get; }
    }

    public class CQCacheBase
    {
        public CQCacheBase()
        {
            this.ID = Guid.NewGuid().ToString();
        }
        public string ID { protected set; get; }
        public virtual bool IsTimeOut(TimeSpan timeout)
        {
            return true;
        }
        public string IsUse { protected set; get; }
    }

    public class CQCache1: CQCacheBase
    {
        public CQCache1()
        {

        }

        public override bool IsTimeOut(TimeSpan timeout)
        {
            return base.IsTimeOut(timeout);
        }
    }

    public class CQCacheManager
    {
        object m_CachesLock = new object();
        public Dictionary<string, CQCacheBase> Caches { protected set; get; }
        virtual public T Create<T>() where T : CQCacheBase, new()
        {
            Monitor.Enter(this.m_CachesLock);
            T aa = new T();
            this.Caches.Add(aa.ID, aa);
            Monitor.Exit(this.m_CachesLock);
            return aa;
        }

        public T Get<T>(string id)
        {
            T aa = default(T);
            
            return aa;
        }

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


    //public class CQCacheData : IQCacheData
    //{
    //    public CQCacheData(string id)
    //    {
    //        this.m_CreateTime = DateTime.Now;
    //        this.m_ID = id;
    //    }
    //    string m_ID;
    //    public string ID { get { return this.m_ID; } }
    //    DateTime m_CreateTime;
    //    public bool IsTimeOut(TimeSpan timeout)
    //    {
    //        bool result = true;
    //        if (DateTime.Now - this.m_CreateTime > timeout)
    //        {
    //            result = true;
    //        }
    //        else
    //        {
    //            result = false;
    //        }
    //        return result;
    //    }

    //    public object Data { set; get; }

    //}
}
