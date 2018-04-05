#define Async_Args
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace QNetwork.Http.Server
{
    public class CQHttpHandler: CQTCPHandler
    {
        public delegate bool NewRequestDelegate(CQHttpHandler hadler, List<CQHttpRequest> requests);
        public event NewRequestDelegate OnNewRequest;
        public CQHttpHandler(Socket socket)
            : base (socket)
        {
            this.m_Socket = socket;
            this.MaxHeaderSize = 8192;
        }


        object m_SendRespsLock = new object();
        Queue<CQHttpResponse> m_SendResps = new Queue<CQHttpResponse>();
        public bool SendResp(CQHttpResponse resp)
        {
            bool result = true;
            Monitor.Enter(this.m_SendRespsLock);
            this.m_SendResps.Enqueue(resp);
            Monitor.Exit(this.m_SendRespsLock);
#if Async_Args
            if (this.m_SendArgs.LastOperation == SocketAsyncOperation.None)
            {
                CQHttpResponse resp1 = null;
                Monitor.Enter(this.m_SendRespsLock);
                resp1 = this.m_SendResps.Dequeue();
                Monitor.Exit(this.m_SendRespsLock);
                if (resp1 != null)
                {
                    this.m_CurrentResp.Set(resp1);
                }
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
                CQHttpResponse resp = null;
                Monitor.Enter(this.m_SendRespsLock);
                if(this.m_SendResps.Count > 0)
                {
                    resp = this.m_SendResps.Dequeue();
                }
                Monitor.Exit(this.m_SendRespsLock);
                if (resp != null)
                {
                    this.m_CurrentResp.Set(resp);
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
                catch(Exception ee)
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

        object m_SendLock = new object();
        CQHttpResponseReader m_CurrentResp = new CQHttpResponseReader();
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

        MemoryStream m_Temp = new MemoryStream();
        enum ParseStates
        {
            Header,
            RecvContent
        }
        ParseStates m_ParseState;
        CQHttpRequest m_RecvRequest;
        long m_ContentLength = 0;
        byte[] m_ContentBuf = new byte[8192];
        protected override bool ParseRequest(byte[] data, int size)
        {
            bool result = true;
            List<CQHttpRequest> requests = new List<CQHttpRequest>();
            int findindex = 0;
            //if (this.m_Temp.Length != 0)
            {
                this.m_Temp.Seek(0, SeekOrigin.End);
                this.m_Temp.Write(data, 0, size);
            }
            bool isparse_end = false;
            while (isparse_end == false)
            {
                if(this.m_ParseState == ParseStates.Header)
                {
                    byte[] req_ = null;
                    int lastindex = 0;
                    byte[] ddata = this.m_Temp.ToArray();
                    for (int i = 0; i < ddata.Length - 3; i++)
                    {
                        if ((ddata[i] == '\r') && (ddata[i + 1] == '\n') && (ddata[i + 2] == '\r') && (ddata[i + 3] == '\n'))
                        {
                            findindex = i + 4;
                            req_ = new byte[findindex];
                            this.m_Temp.Position = 0;
                            this.m_Temp.Read(req_, 0, req_.Length);
                            this.m_Temp.SetLength(0);
                            if (findindex < ddata.Length)
                            {
                                this.m_Temp.Write(ddata, findindex, ddata.Length - findindex);
                                this.m_Temp.Position = 0;
                            }
                            break;
                        }
                    }
                    if (req_ != null)
                    {
                        string address = "127.0.0.1";
                        if (this.m_Socket == null)
                        {
                            return true;
                        }
                        if (this.m_Socket.RemoteEndPoint is IPEndPoint)
                        {
                            IPEndPoint ppoint = (IPEndPoint)this.m_Socket.RemoteEndPoint;
                            address = string.Format("{0}:{1}", ppoint.Address.ToString(), ppoint.Port);
                        }

                        CQHttpRequest req = new CQHttpRequest(this.m_ID, address);
                        
                        req.ParseHeader(req_, lastindex, findindex);

                        if ((req.Method == "POST") && (req.ContentLength > 0))
                        {
                            this.m_ParseState = ParseStates.RecvContent;
                            this.m_RecvRequest = req;
                            this.m_ContentLength = this.m_RecvRequest.ContentLength;
                            this.m_RecvRequest.Content = new MemoryStream();
                        }
                        else
                        {
                            requests.Add(req);
                        }


                    }
                    else
                    {
                        isparse_end = true;
                    }
                }
                else if(this.m_ParseState == ParseStates.RecvContent)
                {
                    this.m_Temp.Position = 0;
                    int recv_len = this.m_ContentBuf.Length;
                    if (recv_len > this.m_ContentLength)
                    {
                        recv_len = (int)this.m_ContentLength;
                    }
                    int read_len = this.m_Temp.Read(this.m_ContentBuf, 0, recv_len);
                    this.m_ContentLength = this.m_ContentLength - read_len;
                    this.m_RecvRequest.Content.Write(this.m_ContentBuf, 0, read_len);
                    if (this.m_ContentLength > 0)
                    {

                    }
                    else
                    {
                        this.m_ParseState = ParseStates.Header;
                        this.m_RecvRequest.Content.Position = 0;
                        requests.Add(this.m_RecvRequest);
                        if ((this.m_Temp.Length - this.m_Temp.Position) > 0)
                        {
                            byte[] ddata = this.m_Temp.ToArray();
                            this.m_Temp.Write(ddata, (int)this.m_Temp.Position, (int)(this.m_Temp.Length - this.m_Temp.Position));
                        }
                            
                    }
                }
                if((this.m_Temp.Length - this.m_Temp.Position) <=0)
                {
                    this.m_Temp.SetLength(0);
                    isparse_end = true;
                }
                
            
            }
            if(this.m_Temp.Length>this.MaxHeaderSize)
            {
                this.m_IsEnd = true;
            }
            else
            {
                if ((this.OnNewRequest != null) && (requests.Count > 0))
                {
                    this.OnNewRequest(this, requests);
                }
            }
            
            return result;
        }

        public int MaxHeaderSize { set; get; }

        public override bool Open(byte[] data, int len)
        {
            bool result = true;

            //this.m_SendArgs = new SocketAsyncEventArgs();
            //this.m_SendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(m_SendArgs_Completed);
            //this.m_SendBuf = new byte[this.m_Socket.SendBufferSize];
            //this.m_SendArgs.SetBuffer(this.m_SendBuf, 0, this.m_SendBuf.Length);
            base.Open(data, len);
            return result;
        }
        

        public bool ControlTransfer(out Socket socket)
        {
            bool result = true;
            this.m_IsEnd = true;
            //if (this.m_RecvArgs != null)
            //{
            //    this.m_RecvArgs.Dispose();
            //    this.m_RecvArgs = null;
            //}
            //SocketInformation inform =  this.m_Socket.DuplicateAndClose(Process.GetCurrentProcess().Id);
            //socket = new Socket(inform);
            socket = this.m_Socket;
            return result;
        }

        public override bool Close()
        {
            bool result = true;
            //if(this.m_SendArgs != null)
            //{
            //    this.m_SendArgs.Dispose();
            //    this.m_SendArgs = null;
            //}
            //this.m_SendBuf = null;
            base.Close();
            this.m_SocketLock.Dispose();
            this.m_SocketLock = null;

            return result;
        }
    }
}
