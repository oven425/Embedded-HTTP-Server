#define Async_Args
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.ComponentModel;

namespace QNetwork.Http.Server
{
    public class CQTCPHandler
    {
        public MemoryStream RecvData { set; get; }
        protected ReaderWriterLockSlim m_SocketLock;
        byte[] m_RecvBuf;
        SocketAsyncEventArgs m_RecvArgs;
        protected SocketAsyncEventArgs m_SendArgs;
        protected byte[] m_SendBuf;
        public bool IsEnd { get { return this.m_IsEnd; } }
        protected Socket m_Socket;
        protected bool m_IsEnd;
        public string ID { get { return this.m_ID; } }
        string m_ID;
        public delegate bool ParseDeleagte(Stream data);
        public event ParseDeleagte OnParse;
        Queue<IQResponseReader> m_SendDatas = new Queue<IQResponseReader>();
        BackgroundWorker m_Thread;
        public CQTCPHandler(Socket socket)
        {
            this.RecvData = new MemoryStream();
            this.m_SocketLock = new ReaderWriterLockSlim();
            this.m_ID = Guid.NewGuid().ToString("N");
            this.m_Socket = socket;
        }

        void m_RecvArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            this.m_SocketLock.EnterReadLock();
            if ((e.SocketError != SocketError.Success) || (e.BytesTransferred == 0) || (this.m_IsEnd == true))
            {
                this.m_IsEnd = true;
            }
            else
            {
                this.ParseRequest(e.Buffer, e.BytesTransferred);
                while ((this.m_IsEnd == false) && (this.m_Socket.ReceiveAsync(this.m_RecvArgs) == false))
                {
                    if ((e.SocketError != SocketError.Success) || (e.BytesTransferred == 0))
                    {
                        this.m_IsEnd = true;
                        break;
                    }
                    else
                    {
                        this.ParseRequest(e.Buffer, e.BytesTransferred);
                    }
                }
            }
            this.m_SocketLock.ExitReadLock();
        }

        public bool AddSend(IQResponseReader data)
        {
            bool result = true;
#if Async_Args
            if (this.m_SendArgs.LastOperation == SocketAsyncOperation.None)
            {
                Stream resp1 = null;
                Monitor.Enter(this.m_SendRespsLock);
                if(this.m_SendDatas.Count > 0)
                {
                    this.m_SendDatas.Enqueue(data);
                    this.m_CurrentResp = this.m_SendDatas.Dequeue();
                }
                else
                {
                    this.m_CurrentResp = data;
                }
                
                Monitor.Exit(this.m_SendRespsLock);

            }
            Monitor.Enter(this.m_SendLock);
            this.Send();
            Monitor.Exit(this.m_SendLock);
#else
            if (this.m_Thread_Send.IsBusy == false)
            {
                this.m_Thread_Send.RunWorkerAsync();
            }
#endif
            return result;
        }

        protected virtual bool ParseRequest(byte[] data, int size)
        {
            this.RecvData.Position = this.RecvData.Length;
            this.RecvData.Write(data, 0, size);
            if (this.OnParse != null)
            {
                this.OnParse(this.RecvData);
            }
            return true;
        }

        public virtual bool Open(byte[] data, int len)
        {
            bool result = true;
            this.m_RecvArgs = new SocketAsyncEventArgs();
            this.m_RecvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(m_RecvArgs_Completed);
            this.m_RecvBuf = new byte[this.m_Socket.ReceiveBufferSize];
            this.m_RecvArgs.SetBuffer(this.m_RecvBuf, 0, this.m_RecvBuf.Length);

            this.m_SendArgs = new SocketAsyncEventArgs();
            this.m_SendArgs.Completed += m_SendArgs_Completed;
            this.m_SendBuf = new byte[this.m_Socket.SendBufferSize];
            ThreadPool.QueueUserWorkItem(o =>
            {
                if (len > 0)
                {
                    this.ParseRequest(data, len);
                }
                while (this.m_Socket.ReceiveAsync(this.m_RecvArgs) == false)
                {
                    if (this.m_RecvArgs.SocketError == SocketError.Success)
                    {
                        this.ParseRequest(data, len);
                    }
                    else
                    {
                        this.m_IsEnd = true;
                    }

                }
            });
            return result;
        }
        object m_SendRespsLock = new object();
        bool m_IsSending = false;
        bool Send()
        {
            bool result = true;
            if (this.m_IsSending == true)
            {
                return result;
            }
            if (this.m_CurrentResp.IsEnd == true)
            {
                IQResponseReader resp = null;
                Monitor.Enter(this.m_SendRespsLock);
                if (this.m_SendDatas.Count > 0)
                {
                    resp = this.m_SendDatas.Dequeue();
                }
                Monitor.Exit(this.m_SendRespsLock);
                if (resp != null)
                {
                    //this.m_CurrentResp.Set(resp);
                }
            }
            if (this.m_CurrentResp.IsEnd == false)
            {
                int send_len = this.m_CurrentResp.Read(this.m_SendBuf, 0, this.m_SendBuf.Length);
                //System.Diagnostics.Trace.Write(Encoding.UTF8.GetString(m_SendBuf, 0, send_len));
                try
                {
                    this.m_SocketLock.EnterReadLock();
                    this.m_SendArgs.SetBuffer(this.m_SendBuf, 0, send_len);
                    while (true)
                    {
                        bool is_pending = this.m_Socket.SendAsync(this.m_SendArgs);
                        if (is_pending == true)
                        {
                            this.m_IsSending = true;
                            break;
                        }
                        else
                        {
                            if (this.m_SendArgs.SocketError != SocketError.Success)
                            {
                                this.m_IsSending = false;
                                this.m_IsEnd = true;
                                break;
                            }
                        }
                    }
                }
                catch (Exception ee)
                {
                    System.Diagnostics.Trace.WriteLine(ee.Message);
                    System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                }
                finally
                {
                    this.m_SocketLock.ExitReadLock();
                }
            }

            return result;
        }

        public EndPoint RemoteEndPoint
        {
            get
            {
                return this.m_Socket.RemoteEndPoint;
            }
        }
        object m_SendLock = new object();
        IQResponseReader m_CurrentResp;
        void m_SendArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            Monitor.Enter(this.m_SendLock);
            this.m_IsSending = false;
            if (e.SocketError == SocketError.Success)
            {
                this.Send();
            }
            else
            {
                this.m_IsEnd = true;
            }
            Monitor.Exit(this.m_SendLock);
        }

        public virtual bool CloseRecv()
        {
            bool result = true;
            try
            {
                this.m_SocketLock.ExitReadLock();
                if (this.m_RecvArgs != null)
                {
                    this.m_RecvArgs.Dispose();
                    this.m_RecvArgs = null;
                }
                this.m_RecvBuf = null;
                if(this.RecvData != null)
                {
                    this.RecvData.Close();
                    this.RecvData.Dispose();
                    this.RecvData = null;
                }
            }
            catch(Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
            }
            finally
            {
                this.m_SocketLock.ExitReadLock();
            }
            
            return result;
        }

        public virtual bool Close()
        {
            bool result = true;
            Monitor.Enter(this.m_SendRespsLock);
            for(int i=0; i <this.m_SendDatas.Count; i++)
            {
                this.m_SendDatas.ElementAt(i).Dispose();
            }
            this.m_SendDatas.Clear();
            Monitor.Exit(this.m_SendRespsLock);
            
            this.m_SocketLock.EnterWriteLock();
            try
            {
                this.m_IsEnd = true;

                if (this.m_RecvArgs != null)
                {
                    this.m_RecvArgs.Dispose();
                    this.m_RecvArgs = null;
                }
                this.m_RecvBuf = null;
                if(this.m_SendArgs != null)
                {
                    this.m_SendArgs.Dispose();
                    this.m_SendArgs = null;
                }
                this.m_SendBuf = null;
                
                if (this.m_Socket != null)
                {
                    this.m_Socket.Close();
                    this.m_Socket = null;
                }
            }
            catch (Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                result = false;
            }
            finally
            {
                this.m_SocketLock.ExitWriteLock();
            }
            return result;
        }
    }
}
