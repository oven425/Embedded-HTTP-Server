using QNetwork.Http.Server.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace QNetwork.Http.Server.Cache
{
    public interface IQCache
    {
        string ID {  get; }
        bool IsTimeOut(TimeSpan timeout);
    }
    public class CQCacheBase: IQCache
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
        public string IsUse { protected set; get; }
        protected DateTime m_BeginTime;

        #region IDisposable Support
        private bool disposedValue = false; // 偵測多餘的呼叫

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)。
                }

                // TODO: 釋放非受控資源 (非受控物件) 並覆寫下方的完成項。
                // TODO: 將大型欄位設為 null。

                disposedValue = true;
            }
        }

        // TODO: 僅當上方的 Dispose(bool disposing) 具有會釋放非受控資源的程式碼時，才覆寫完成項。
        // ~CQCacheBase() {
        //   // 請勿變更這個程式碼。請將清除程式碼放入上方的 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 加入這個程式碼的目的在正確實作可處置的模式。
        public void Dispose()
        {
            // 請勿變更這個程式碼。請將清除程式碼放入上方的 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果上方的完成項已被覆寫，即取消下行的註解狀態。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class CQCacheManager
    {
        public string NickName { set; get; }
        protected object m_CachesLock = new object();
        public Dictionary<string, CQCacheBase> Caches { protected set; get; }
        public IQHttpServer_Log Logger { set; get; }
        public CQCacheManager()
        {
            this.NickName = "default";
            this.Caches = new Dictionary<string, CQCacheBase>();
        }

        virtual public T Create<T>(string id) where T : CQCacheBase, new()
        {
            T aa = null;
            aa = new T();
            if (string.IsNullOrEmpty(id) == false)
            {
                aa.ID = id;
            }
            if(this.Caches.ContainsKey(id) == true)
            {
                this.Caches[id] = aa;
            }
            else
            {
                this.Caches.Add(aa.ID, aa);
            }
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
           
            List<CQCacheBase> tt = this.Caches.Values.Where(x => x.IsTimeOut(TimeSpan.FromSeconds(10)) == true).ToList();
            
            for (int i = 0; i < tt.Count; i++)
            {
                tt[i].Dispose();
                Monitor.Enter(this.m_CachesLock);
                if(this.Logger != null)
                {
                    this.Logger.LogCache(LogStates_Cache.DestoryCache, DateTime.Now, this.NickName, tt[i].ID, "");
                }
                this.Caches.Remove(tt[i].ID);
                Monitor.Exit(this.m_CachesLock);
            }

            return result;
        }
    }
}
