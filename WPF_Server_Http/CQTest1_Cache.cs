using QNetwork.Http.Server;
using QNetwork.Http.Server.Cache;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    public class CQCache_Playback : CQCacheBase
    {
        CQTCPHandler m_TCPHandler;
        BackgroundWorker m_Thread;
        public CQCache_Playback()
        {
            this.m_Thread = new BackgroundWorker();
            this.m_Thread.DoWork += M_Thread_DoWork;
        }

        public CQCache_Playback(string id)
            : base(id)
        {

        }

        string m_Command;
        private void M_Thread_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (true)
                {
                    StringBuilder strb = new StringBuilder();
                    strb.AppendLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo));
                    strb.AppendLine(string.Format("Command:{0}", this.m_Command));
                    byte[] bb = Encoding.ASCII.GetBytes(strb.ToString());
                    this.m_TCPHandler.Send(bb, bb.Length);
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
            }
            finally
            {

            }
        }

        private bool M_TCPHandler_OnParse(System.IO.Stream data)
        {
            byte[] bb = new byte[data.Length];
            data.Position = 0;
            data.Read(bb, 0, bb.Length);
            this.m_Command = Encoding.ASCII.GetString(bb);
            return true;
        }

        public bool Open(CQTCPHandler tcp_handler, CQHttpRequest request)
        {
            bool result = true;
            this.m_TCPHandler = tcp_handler;
            this.m_TCPHandler.OnParse += M_TCPHandler_OnParse;
            if(this.m_Thread.IsBusy == false)
            {
                this.m_Thread.RunWorkerAsync();
            }
            return result;
        }


        public override bool IsTimeOut(TimeSpan timeout)
        {
            return base.IsTimeOut(timeout);
        }
    }
}
