using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using QNetwork.Http.Server.Log;

namespace QNetwork.Http.Server.Protocol
{
    public enum Connections
    {
        Close,
        KeepAlive,
        Upgrade,
        None
    }
    public class CQHttpResponse:IDisposable
    {

        public IQHttpServer_Log Logger { set; get; }
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
        public List<string> AccessControlAllowHeaders { set; get; }
        string m_HandlerID;
        public string HandlerID { get { return this.m_HandlerID; } }
        string m_ProcessID;
        public string ProcessID { get { return this.m_ProcessID; } }
        public Connections Connection { set; get; }
        public long ContentLength { set; get; }
        public string ContentType { set; get; }
        public BuildTypes BuildType { get { return this.m_BuildType; } }
        
        public CQHttpResponse(BuildTypes builetype = BuildTypes.Basic)
        {
            this.AccessControlAllowHeaders = new List<string>();
            this.m_BuildType = builetype;
            //this.m_HandlerID = handlerid;
            //this.m_ProcessID = processid;
            this.Headers = new Dictionary<string, string>();
            this.Message = "OK";
            this.Code = "200";
            this.Protocol = "HTTP/1.1";
        }

        public CQHttpResponse(string handlerid, string processid, BuildTypes builetype= BuildTypes.Basic)
        {
            this.AccessControlAllowHeaders = new List<string>();
            this.m_BuildType = builetype;
            this.m_HandlerID = handlerid;
            this.m_ProcessID = processid;
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
                            strb.AppendLine(string.Format("{0}: {1}", "Content-Length", this.ContentLength));
                        }
                        if (string.IsNullOrEmpty(this.ContentType) == false)
                        {
                            strb.AppendLine(string.Format("{0}:{1}", "Content-Type", this.ContentType));
                        }
                        foreach(string header in this.AccessControlAllowHeaders)
                        {
                            strb.AppendLine(string.Format("{0}: {1}", "Access-Control-Allow-Headers", header));
                        }
                        switch(this.Connection)
                        {
                            case Connections.KeepAlive:
                                {
                                    strb.AppendLine(string.Format("{0}: {1}", "Connection", "KeepAlive"));
                                }
                                break;
                            case Connections.Upgrade:
                                {
                                    strb.AppendLine(string.Format("{0}: {1}", "Connection", "Upgrade"));
                                }
                                break;
                            case Connections.None:
                                {
                                }
                                break;
                            case Connections.Close:
                            default:
                                {
                                    strb.AppendLine(string.Format("{0}: {1}", "Connection", "Close"));
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
                            strb.AppendLine(string.Format("{0}: {1}", "Content-Type", this.ContentType));
                        }
                        if(this.ContentLength >= 0)
                        {
                            strb.AppendLine(string.Format("{0}: {1}", "Content-Length", this.ContentLength));
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

        public bool BuildContentFromString(string data)
        {
            bool result = true;
            byte[] str_buf = Encoding.ASCII.GetBytes(data);
            if(this.Content != null)
            {
                if(this.Content is MemoryStream)
                {
                    this.Content.SetLength(0);
                }
                else
                {
                    this.Content.Close();
                    this.Content.Dispose();
                    this.Content = null;
                }
            }
            if(this.Content == null)
            {
                this.Content = new MemoryStream();
            }
            this.Content.Write(str_buf, 0, str_buf.Length);
            this.Content.Position = 0;
            this.ContentLength = this.Content.Length;

            return result;
        }

        public void SetContent(string data, string type="text/plain")
        {
            if(this.Content == null)
            {
                this.Content = new MemoryStream();
            }
            else if(!(this.Content is MemoryStream))
            {
                this.Content.Close();
                this.Content.Dispose();
                this.Content = null;
                this.Content = new MemoryStream();
            }
            else
            {
                this.Content.SetLength(0);
            }
            byte[] bb = Encoding.UTF8.GetBytes(data);
            this.Content.Write(bb, 0, bb.Length);
            this.Content.Position = 0;
            this.ContentLength = this.Content.Length;
            this.ContentType = type;
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
