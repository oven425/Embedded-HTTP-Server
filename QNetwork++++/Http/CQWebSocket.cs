using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace QNetwork.Http.Server
{
    public class CQWebSocket: IQCacheData
    {
        CQTCPHandler m_TcpHandler;
        public CQWebSocket()
        {
            m_IsEnd = false;
        }
        public string ID => throw new NotImplementedException();

        public object Data { set; get; }
        bool m_IsEnd;

        public bool IsTimeOut(TimeSpan timeout)
        {
            return this.m_IsEnd;
        }

        public bool Open(CQTCPHandler handler, byte[] data, int len)
        {
            
            bool result = true;
            this.m_TcpHandler = handler;
            this.m_TcpHandler.OnParse += M_TcpHandler_OnParse;
            CQHttpRequest req = new CQHttpRequest("", "");
            req.ParseHeader(data, 0, len);
            string key = req.Headers["SEC-WEBSOCKET-VERSION"];

            SHA1 sha1 = new SHA1CryptoServiceProvider();//建立一個SHA1
            byte[] source = Encoding.Default.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");//將字串轉為Byte[]
            byte[] crypto = sha1.ComputeHash(source);//進行SHA1加密
            string base64_str = Convert.ToBase64String(crypto);//把加密後的字串從Byte[]轉為字串
 

            CQHttpResponse resp = new CQHttpResponse("");
            resp.Code = "101";
            resp.Message = "Socket Protocol Handshake";
            resp.Protocol = "HTTP/1.1";
            resp.Connection = Connections.Upgrade;
            resp.ContentLength = -1;
            resp.Headers.Add("Upgrade", "websocket");
            resp.Headers.Add("Sec-WebSocket-Accept", base64_str);

            resp.Headers.Add("Access-Control-Allow-Credentials", "true");
            resp.AccessControlAllowHeaders.Add("content-type");
            resp.AccessControlAllowHeaders.Add("authorization");
            resp.AccessControlAllowHeaders.Add("x-websocket-extensions");
            resp.AccessControlAllowHeaders.Add("x-websocket-version");
            resp.AccessControlAllowHeaders.Add("x-websocket-protocol");
            //resp.Headers.Add("Access-Control-Allow-Headers", "content-type");
            //resp.Headers.Add("Access-Control-Allow-Headers", "authorization");
            //resp.Headers.Add("Access-Control-Allow-Headers", "x-websocket-extensions");
            //resp.Headers.Add("Access-Control-Allow-Headers", "x-websocket-version");
            //resp.Headers.Add("Access-Control-Allow-Headers", "x-websocket-protocol");
            resp.Headers.Add("Access-Control-Allow-Origin", "http://10.1.1.246:3333");

            CQHttpResponseReader resp_reader = new CQHttpResponseReader();
            resp_reader.Set(resp);
            this.m_TcpHandler.AddSend(resp_reader);
            //string resp_str = resp.ToString();

            return result;
        }

        private bool M_TcpHandler_OnParse(System.IO.Stream data)
        {
            throw new NotImplementedException();
        }
    }
}
