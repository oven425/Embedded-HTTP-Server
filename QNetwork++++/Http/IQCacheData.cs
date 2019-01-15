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
}
