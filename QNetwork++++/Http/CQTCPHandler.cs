using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
        protected string m_ID;
        public delegate bool ParseDeleagte(Stream data);
        public event ParseDeleagte OnParse;
        Queue<Stream> m_SendDatas = new Queue<Stream>();
        
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

        public bool AddSend(Stream data)
        {
            bool result = true;
            

            return result;
        }

        protected virtual bool ParseRequest(byte[] data, int size)
        {
            if(this.OnParse!= null)
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
            this.m_SendArgs.Completed += M_SendArgs_Completed;
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



        private void M_SendArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if ((e.SocketError != SocketError.Success) || (e.BytesTransferred > 0))
            {
                this.m_IsEnd = true;
            }
            else
            {

            }
        }

        public virtual bool Close()
        {
            bool result = true;
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
