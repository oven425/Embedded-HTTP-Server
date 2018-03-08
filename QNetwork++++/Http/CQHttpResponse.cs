using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace QNetwork.Http.Server
{
    public class CQHttpResponseReader
    {
        CQHttpResponse m_Resp;
        MemoryStream m_HeaderBuf = new MemoryStream();
        enum ReadStates
        {
            None,
            Header,
            Content
        }
        ReadStates m_ReadState;
        public bool IsEmpty { get { return this.m_Resp == null; } }
        public bool Set(CQHttpResponse resp)
        {
            bool result = true;
            this.m_Resp = resp;
            this.m_IsEnd = false;
            this.m_ReadState = ReadStates.None;
            return result;
        }
        bool m_IsEnd = true;
       
        public bool IsEnd { get { return this.m_IsEnd; } }

        public int Read(byte[] buffer, int offset, int count)
        {
            switch(this.m_ReadState)
            {
                case ReadStates.None:
                    {
                        string str_header = this.m_Resp.ToString();
                        byte[] buf = Encoding.UTF8.GetBytes(str_header);
                        this.m_HeaderBuf.SetLength(0);
                        this.m_HeaderBuf.Write(buf, 0, buf.Length);
                        this.m_HeaderBuf.Position = 0;
                        this.m_ReadState = ReadStates.Header;
                    }
                    break;
            }
            int read_len = 0;
            int maxread_len = count - offset;
            if (maxread_len > buffer.Length)
            {
                maxread_len = buffer.Length;
            }
            if (maxread_len > 0)
            {
                if (this.m_ReadState == ReadStates.Header)
                {
                    read_len = this.m_HeaderBuf.Read(buffer, offset, count);
                    if (this.m_HeaderBuf.Position == this.m_HeaderBuf.Length)
                    {
                        this.m_ReadState = ReadStates.Content;
                    }
                }
                if (this.m_ReadState == ReadStates.Content)
                {
                    int read_size = maxread_len - read_len;
                    int read_offset = offset + read_len;
                    if ((this.m_Resp.Content != null) && (read_size > 0))
                    {
                        int rdlen = this.m_Resp.Content.Read(buffer, read_offset, read_size);
                        read_len = read_len + rdlen;
                    }
                }
            }
            else
            {
                read_len = 0;
            }
            if (read_len < maxread_len)
            {
                this.m_IsEnd = true;
            }
            else if (read_len <= 0)
            {
                this.m_IsEnd = true;
            }
            else if ((this.m_Resp.Content != null) && (this.m_Resp.Content.Position >= this.m_Resp.Content.Length))
            {
                this.m_IsEnd = true;
            }
            if (this.m_IsEnd == true)
            {
                if (this.m_Resp.Content != null)
                {
                    this.m_Resp.Content.Close();
                    this.m_Resp.Content.Dispose();
                    this.m_Resp.Content = null;
                }
            }
            return read_len;
        }
    }

    public enum Connections
    {
        Close,
        KeepAlive,
        Upgrade,
        None
    }
    public class CQHttpResponse:IDisposable
    {
        public enum BuildTypes
        {
            Basic,
            MultiPart
        }
        BuildTypes m_BuildType;
        public string Protocol { set; get; }
        public string Code { set; get; }
        public string Message { set; get; }
        public Stream Content { set; get; }
        public Dictionary<string, string> Headers { set; get; }
        string m_HandlerID;
        public string HandlerID { get { return this.m_HandlerID; } }
        public Connections Connection { set; get; }
        public long ContentLength { set; get; }
        public string ContentType { set; get; }
        public BuildTypes BuildType { get { return this.m_BuildType; } }
        public CQHttpResponse(string handlerid, BuildTypes builetype= BuildTypes.Basic)
        {
            this.m_BuildType = builetype;
            this.m_HandlerID = handlerid;
            this.Headers = new Dictionary<string, string>();
            this.Message = "OK";
            this.Code = "200";
            this.Protocol = "HTTP/1.1";
        }

        public override string ToString()
        {
            StringBuilder strb = new StringBuilder();
            switch (this.m_BuildType)
            {
                case BuildTypes.Basic:
                    {
                        strb.AppendLine(string.Format("{0} {1} {2}", this.Protocol, this.Code, this.Message));

                        for (int i = 0; i < this.Headers.Count; i++)
                        {
                            strb.AppendLine(string.Format("{0}: {1}", this.Headers.ElementAt(i).Key, this.Headers.ElementAt(i).Value));
                        }
                        if (this.ContentLength >= 0)
                        {
                            strb.AppendLine(string.Format("{0}:{1}", "Content-Length", this.ContentLength));
                        }
                        if (string.IsNullOrEmpty(this.ContentType) == false)
                        {
                            strb.AppendLine(string.Format("{0}:{1}", "Content-Type", this.ContentType));
                        }
                        switch(this.Connection)
                        {
                            case Connections.KeepAlive:
                                {
                                    strb.AppendLine(string.Format("{0}:{1}", "Connection", "KeepAlive"));
                                }
                                break;
                            case Connections.Upgrade:
                                {
                                    strb.AppendLine(string.Format("{0}:{1}", "Connection", "Upgrade"));
                                }
                                break;
                            case Connections.None:
                                {
                                }
                                break;
                            case Connections.Close:
                            default:
                                {
                                    strb.AppendLine(string.Format("{0}:{1}", "Connection", "Close"));
                                }
                                break;
                        }
                        strb.AppendLine();
                    }
                    break;
                case BuildTypes.MultiPart:
                    {
                        strb.AppendLine("--QQQ");
                        if (string.IsNullOrEmpty(this.ContentType) == false)
                        {
                            strb.AppendLine(string.Format("{0}:{1}", "Content-Type", this.ContentType));
                        }
                        if(this.ContentLength >= 0)
                        {
                            strb.AppendLine(string.Format("{0}:{1}", "Content-Length", this.ContentLength));
                        }
                        
                        strb.AppendLine();
                    }
                    break;
            }

            return strb.ToString();
        }

        public void Set200(string message = "OK")
        {
            this.Code = "200";
            this.Message = message;
        }

        public void Set401(string message = "Unauthorized", string nonce = "", string realm = "admin")
        {
            this.Code = "401";
            this.Message = message;
            if (string.IsNullOrEmpty(nonce) == true)
            {
                this.Headers["WWW-Authenticate"] = string.Format("Basic realm=\"{0}\"", realm);
            }
            else
            {
                this.Headers["WWW-Authenticate"] = string.Format("Digest realm=\"{0}\",nonce=\"{0}\"", realm, nonce);
            }
        }
        public void Set403(string message = "Forbidden")
        {
            this.Code = "403";
            this.Message = message;
        }
        public void Set404(string message = "Not Found")
        {
            this.Code = "404";
            this.Message = message;
        }


        public void Dispose()
        {
            if (this.Content != null)
            {
                this.Content.Close();
                this.Content.Dispose();
                this.Content = null;
            }
        }
    }
}
