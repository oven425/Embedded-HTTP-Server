using QNetwork.Http.Server;
using QNetwork.Http.Server.Cache;
using QNetwork.Http.Server.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace WPF_Server_Http
{
    public class CQHttpService_Playback : IQHttpService
    {
        List<string> m_Methods;
        CQTCPHandler m_TCPHandler;
        BackgroundWorker m_Thread;
        bool m_IsEnd = false;
        public CQHttpService_Playback()
        {
            this.m_Thread = new BackgroundWorker();
            this.m_Thread.DoWork += M_Thread_DoWork;
            this.m_Methods = new List<string>();
            this.m_Methods.Add("/PLAYBACK");
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
                this.m_IsEnd = true;
            }
            finally
            {

            }
        }

        public IQHttpServer_Extension Extension { set; get; }

        public List<string> Methods => this.m_Methods;

        public bool CloseHandler(List<string> handlers)
        {
            return true;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool Process(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            bool result = true;
            process_result_code = ServiceProcessResults.None;
            resp = null;
            switch (req.URL.LocalPath.ToUpperInvariant())
            {
                case "/PLAYBACK":
                    {
                        string query_str = req.URL.Query;
                        if (string.IsNullOrEmpty(query_str) == true)
                        {
                            process_result_code = ServiceProcessResults.ControlTransfer;
                            CQTCPHandler tcp;
                            this.Extension.ControlTransfer(req.HandlerID, out tcp);
                            this.m_TCPHandler = tcp;
                            this.m_TCPHandler.OnParse += M_TCPHandler_OnParse;
                            if (this.m_Thread.IsBusy == false)
                            {
                                this.m_Thread.RunWorkerAsync();
                            }

                            //this.m_TCPHandler.AddSend(resp_reader);
                            //this.m_IsEnd = true;

                            //CQRecordPlaybackT tt = new CQRecordPlaybackT(tcp, (++this.m_SessionID).ToString());
                            //tt.Open();
                        }
                        else
                        {
                            //query_str = query_str.Remove(0, 1);
                            //if (this.m_Caches.ContainsKey(query_str) == true)
                            //{
                            //    CQRecordPlaybackT ppt = this.m_Caches[query_str] as CQRecordPlaybackT;
                            //    ppt.Control(query_str);
                            //}
                        }
                    }
                    break;
            }

            return result;
        }

        private bool M_TCPHandler_OnParse(System.IO.Stream data)
        {
            byte[] bb = new byte[data.Length];
            data.Position = 0;
            data.Read(bb, 0, bb.Length);
            this.m_Command = Encoding.ASCII.GetString(bb);
            return true;
        }

        public bool RegisterCacheManager()
        {
            bool result = true;


            return result;
        }
    }
}
