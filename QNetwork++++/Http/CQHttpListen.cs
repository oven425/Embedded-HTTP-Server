//#define Accept_Sync
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace QNetwork.Http.Server
{
    public class CQNetAddress
    {
        public string IP { set; get; }
        public int Port { set; get; }
    }
    public class CQSocketListen
    {
        Socket m_Socket;
        SocketAsyncEventArgs m_AcceptArgs;
        byte[] m_AcceptBuf;
        public EndPoint Address { set; get; }
        public delegate bool NewClientDelegate(Socket socket, byte[] data, int len);
        public event NewClientDelegate OnNewClient;
#if Accept_Sync
        BackgroundWorker m_Thread_Accept;
#endif
        public bool Open()
        {
            bool result = true;
            this.m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.m_Socket.Bind(this.Address);
            this.m_Socket.Listen(10);
            this.m_AcceptBuf = new byte[8192];
#if Accept_Sync
            this.m_Thread_Accept = new BackgroundWorker();
            this.m_Thread_Accept.DoWork += new DoWorkEventHandler(m_Thread_Accept_DoWork);
            this.m_Thread_Accept.RunWorkerAsync();
#else
            this.m_AcceptBuf = new byte[this.m_Socket.ReceiveBufferSize];
            this.m_AcceptArgs = new SocketAsyncEventArgs();
            this.m_AcceptArgs.SetBuffer(this.m_AcceptBuf, 0, this.m_AcceptBuf.Length);
            this.m_AcceptArgs.Completed += m_AcceptArgs_Completed;

            if (this.m_Socket.AcceptAsync(this.m_AcceptArgs) == false)
            {
                this.m_AcceptArgs_Completed(this.m_Socket, this.m_AcceptArgs);
            }
#endif
            return result;
        }

        void m_Thread_Accept_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                bool isbreak = false;
                try
                {
                    Socket clinet = this.m_Socket.Accept();
                    int recv_len = clinet.Receive(this.m_AcceptBuf);
                    this.NewClient(clinet, this.m_AcceptBuf, recv_len);
                }
                catch (Exception ee)
                {
                    isbreak = true;
                }

                if (isbreak == true)
                {
                    break;
                }
            }
        }

        void NewClient(Socket socket, byte[] buf, int len)
        {
            if(this.OnNewClient != null)
            {
                this.OnNewClient(socket, buf, len);
            }
        }
        private void m_AcceptArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            string str = Encoding.ASCII.GetString(e.Buffer, 0, e.BytesTransferred);
            //System.Diagnostics.Trace.WriteLine(str);
            this.NewClient(e.AcceptSocket, this.m_AcceptBuf, e.BytesTransferred);
            this.m_AcceptArgs.AcceptSocket = null;
                if (this.m_Socket.AcceptAsync(this.m_AcceptArgs) == false)
                {
                    this.m_AcceptArgs_Completed(this.m_Socket, this.m_AcceptArgs);
                }
            }

        public bool Close()
        {
            bool result = true;
            if (this.m_Socket != null)
            {
                this.m_Socket.Close();
                this.m_Socket = null;
            }
            if (this.m_AcceptArgs != null)
            {
                this.m_AcceptArgs.Dispose();
                this.m_AcceptArgs = null;
            }
            this.m_AcceptBuf = null;
            return result;
        }
    }
}
