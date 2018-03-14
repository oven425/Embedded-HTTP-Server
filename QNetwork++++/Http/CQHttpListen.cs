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
    public enum ListenStates
    {
        Closed,
        Opening,
        Fail,
        Normal,
    }
    public class CQSocketListen_Address
    {
        public string IP { set; get; }
        public int Port { set; get; }

        public override string ToString()
        {
            return string.Format("IP:{0} Port:{1}"
                , this.IP
                , this.Port);
        }

        EndPoint m_EndPoint;
        public EndPoint ToEndPint()
        {
            return new IPEndPoint(IPAddress.Parse(this.IP), this.Port);
        }
    }
    public class CQSocketListen
    {
        ListenStates m_ListenState;
        public ListenStates ListenState
        {
            get
            {
                return this.m_ListenState;
            }
        }
        Socket m_Socket;
        SocketAsyncEventArgs m_AcceptArgs;
        byte[] m_AcceptBuf;
        public EndPoint BindEndPoint { set; get; }
        CQSocketListen_Address m_Address;
        public CQSocketListen_Address Addrss
        {
            get
            {
                return this.m_Address;
            }
        }
        public delegate bool NewClientDelegate(Socket socket, byte[] data, int len);
        public event NewClientDelegate OnNewClient;
        public delegate bool ListenStateDelegate(CQSocketListen listen);
        public event ListenStateDelegate OnListenState;
        public CQSocketListen(CQSocketListen_Address address)
        {
            this.m_Address = address;
        }
#if Accept_Sync
        BackgroundWorker m_Thread_Accept;
#endif
        public bool Open()
        {
            bool result = true;

            this.m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                this.m_Socket.Bind(this.m_Address.ToEndPint());
                this.m_Socket.Listen(10);
            }
            catch(Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                result = false;
            }
            if(result == false)
            {
                this.m_ListenState = ListenStates.Fail;
                if(this.OnListenState != null)
                {
                    this.OnListenState(this);
                }
                return result;
            }
            else
            {
                this.m_ListenState = ListenStates.Normal;
                if (this.OnListenState != null)
                {
                    this.OnListenState(this);
                }
            }
            
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
            if ((e.SocketError == SocketError.Success) && (e.BytesTransferred > 0))
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
            else
            {
                if(this.m_ListenState != ListenStates.Closed)
                {

                }
            }
        }

        public bool Close()
        {
            bool result = true;
            this.m_ListenState = ListenStates.Closed;
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
