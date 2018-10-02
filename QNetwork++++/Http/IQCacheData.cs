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

    public class CQCache_Test : IQCacheData
    {
        public CQCache_Test()
        {

        }
        public string ID => throw new NotImplementedException();

        public bool IsUse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsTimeOut(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
    }

    public interface IQCacheManager
    {

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
