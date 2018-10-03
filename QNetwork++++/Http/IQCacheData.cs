using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public Dictionary<string, CQCacheBase> Caches { protected set; get; }
        public T Create<T>() where T : CQCacheBase, new()
        {
            return new T();
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
