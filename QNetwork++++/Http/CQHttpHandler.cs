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
    public class CQHttpHandler
    {
        CQTCPHandler m_SocketHandler;
        public delegate bool NewRequestDelegate(CQHttpHandler hadler, List<CQHttpRequest> requests);
        public event NewRequestDelegate OnNewRequest;
        public string ID { get { return this.m_ID; } }
        public CQHttpHandler(CQTCPHandler data)
        {
            this.m_SocketHandler = data;
            this.m_SocketHandler.OnParse += M_SocketHandler_OnParse;
            this.MaxHeaderSize = 8192;
            this.m_ID = this.m_SocketHandler.ID;
        }

        private bool M_SocketHandler_OnParse(System.IO.Stream data)
        {
            this.ParseRequest(data);
            return true;
        }



        //Queue<CQHttpResponse> m_SendResps = new Queue<CQHttpResponse>();
        public bool SendResp(CQHttpResponse resp)
        {
            bool result = true;
            CQHttpResponseReader resp_reader = new CQHttpResponseReader();
            resp_reader.Set(resp);
            this.m_SocketHandler.AddSend(resp_reader);
            //Monitor.Enter(this.m_SendRespsLock);
            //this.m_SendResps.Enqueue(resp);
            //Monitor.Exit(this.m_SendRespsLock);
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
            //if (this.m_Thread_Send.IsBusy == false)
            //{
            //    this.m_Thread_Send.RunWorkerAsync();
            //}
#endif

            return result;
        }



        string m_ID;
        //MemoryStream m_Temp = new MemoryStream();
        enum ParseStates
        {
            Header,
            RecvContent
        }
        ParseStates m_ParseState;
        CQHttpRequest m_RecvRequest;
        long m_ContentLength = 0;
        byte[] m_ContentBuf = new byte[8192];
        byte[] m_HeaderBuf = new byte[8192];
        protected bool ParseRequest(System.IO.Stream data)
        {
            bool result = true;
            List<CQHttpRequest> requests = new List<CQHttpRequest>();
            int findindex = 0;
            //if (this.m_Temp.Length != 0)
            {
                //this.m_Temp.Seek(0, SeekOrigin.End);
                //this.m_Temp.Write(data, 0, size);
            }
            data.Position = 0;
            bool isparse_end = false;
            while (isparse_end == false)
            {
                if(this.m_ParseState == ParseStates.Header)
                {
                    byte[] req_ = null;
                    int lastindex = 0;
                    int read_len = data.Read(this.m_HeaderBuf, 0, this.m_HeaderBuf.Length);
                    for (int i = 0; i < read_len - 3; i++)
                    {
                        if ((this.m_HeaderBuf[i] == '\r') && (this.m_HeaderBuf[i + 1] == '\n') && (this.m_HeaderBuf[i + 2] == '\r') && (this.m_HeaderBuf[i + 3] == '\n'))
                        {
                            findindex = i + 4;
                            req_ = new byte[findindex];
                            Array.Copy(this.m_HeaderBuf, req_, findindex);
                            break;
                        }
                    }
                    if (req_ != null)
                    {
                        string address = "127.0.0.1";
                        if (this.m_SocketHandler == null)
                        {
                            return true;
                        }
                        if (this.m_SocketHandler.RemoteEndPoint is IPEndPoint)
                        {
                            IPEndPoint ppoint = (IPEndPoint)this.m_SocketHandler.RemoteEndPoint;
                            address = string.Format("{0}:{1}", ppoint.Address.ToString(), ppoint.Port);
                        }

                        CQHttpRequest req = new CQHttpRequest(this.m_SocketHandler.ID, address);
                        
                        req.ParseHeader(req_, lastindex, findindex);

                        if ((req.Method == "POST") && (req.ContentLength > 0))
                        {
                            this.m_ParseState = ParseStates.RecvContent;
                            this.m_RecvRequest = req;
                            this.m_ContentLength = this.m_RecvRequest.ContentLength;
                            this.m_RecvRequest.Content = new MemoryStream();
                            int rrsize = read_len - findindex;
                            this.m_RecvRequest.Content.Write(this.m_HeaderBuf, findindex, rrsize);
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
                    long rrsize = this.m_RecvRequest.ContentLength - this.m_RecvRequest.Content.Length;
                    if(rrsize > data.Length)
                    {
                        while(true)
                        {
                            int read_len = data.Read(this.m_ContentBuf, 0, this.m_ContentBuf.Length);
                            this.m_RecvRequest.Content.Write(this.m_ContentBuf, 0, read_len);
                            if(read_len != this.m_ContentBuf.Length)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {

                        if (rrsize > this.m_ContentBuf.Length)
                        {
                            System.Diagnostics.Trace.WriteLine("");
                        }
                        else
                        {
                            int read_len = data.Read(this.m_ContentBuf, 0, this.m_ContentBuf.Length);
                            this.m_RecvRequest.Content.Write(this.m_ContentBuf, 0, read_len);
                        }
                    }

                }
                if ((this.m_ParseState == ParseStates.RecvContent) && (this.m_RecvRequest.Content.Length == this.m_RecvRequest.ContentLength))
                {
                    this.m_ParseState = ParseStates.Header;
                    this.m_RecvRequest.Content.Position = 0;
                    requests.Add(this.m_RecvRequest);
                }

                if ((data.Length - data.Position) <= 0)
                {
                    
                    data.SetLength(0);
                    isparse_end = true;
                }
               


            }
            if(data.Length>this.MaxHeaderSize)
            {
                this.m_SocketHandler.Close();
                //this.m_IsEnd = true;
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

        public bool Open(byte[] data, int len)
        {
            bool result = true;

            //this.m_SendArgs = new SocketAsyncEventArgs();
            //this.m_SendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(m_SendArgs_Completed);
            //this.m_SendBuf = new byte[this.m_Socket.SendBufferSize];
            //this.m_SendArgs.SetBuffer(this.m_SendBuf, 0, this.m_SendBuf.Length);
            //base.Open(data, len);
            this.m_SocketHandler.Open(data, len);
            return result;
        }
        
        public bool IsEnd
        {
            get
            {
                bool isend = true;
                if(this.m_SocketHandler != null)
                {
                    isend = this.m_SocketHandler.IsEnd;
                }
                return isend;
            }
        }
        public bool ControlTransfer(out CQTCPHandler handler)
        {
            bool result = true;
            this.m_SocketHandler.OnParse -= M_SocketHandler_OnParse;
            handler = this.m_SocketHandler;
            return result;
        }

        public bool Close()
        {
            bool result = true;
            if(this.m_SocketHandler != null)
            {
                this.m_SocketHandler.Close();
                this.m_SocketHandler = null;
            }
            return result;
        }
    }
}
