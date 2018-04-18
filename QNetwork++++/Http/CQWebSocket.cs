using System;
using System.Collections.Generic;
using System.IO;
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
        public string ID => this.m_TcpHandler == null ? "" : this.m_TcpHandler.ID;

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
            string key = req.Headers["SEC-WEBSOCKET-KEY"];

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
            resp.Headers.Add("Sec-WebSocket-Version", "13");

            //resp.Headers.Add("Access-Control-Allow-Credentials", "true");
            //resp.AccessControlAllowHeaders.Add("content-type");
            //resp.AccessControlAllowHeaders.Add("authorization");
            //resp.AccessControlAllowHeaders.Add("x-websocket-extensions");
            //resp.AccessControlAllowHeaders.Add("x-websocket-version");
            //resp.AccessControlAllowHeaders.Add("x-websocket-protocol");
            //resp.Headers.Add("Access-Control-Allow-Headers", "content-type");
            //resp.Headers.Add("Access-Control-Allow-Headers", "authorization");
            //resp.Headers.Add("Access-Control-Allow-Headers", "x-websocket-extensions");
            //resp.Headers.Add("Access-Control-Allow-Headers", "x-websocket-version");
            //resp.Headers.Add("Access-Control-Allow-Headers", "x-websocket-protocol");
            //resp.Headers.Add("Access-Control-Allow-Origin", "*");
            System.Diagnostics.Trace.WriteLine(resp);
            CQHttpResponseReader resp_reader = new CQHttpResponseReader();
            resp_reader.Set(resp);
            this.m_TcpHandler.AddSend(resp_reader);
            //string resp_str = resp.ToString();

            return result;
        }

        private bool M_TcpHandler_OnParse(System.IO.Stream data)
        {
            data.Position = 0;
            byte bb = (byte)data.ReadByte();
            int FIN = (bb & 0x80)>>7;
            int Opcode = (bb & 0x0F);
            bb = (byte)data.ReadByte();
            
            int mask = (bb & 0x80) >> 7;
            int payload_len = (bb & 0x7F);

            byte[] bb1 = new byte[4];
            data.Read(bb1, 0, bb1.Length);
            int mask_key = BitConverter.ToInt32(bb1, 0);

            byte[] bb2 = new byte[payload_len];
            data.Read(bb2, 0, bb2.Length);
            for (var i = 0; i < payload_len; i++)
            {
                var j = i % 4;
                bb2[i] = (byte)(bb2[i]^ bb1[j]);
            }
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(bb2));


            
            return true;
        }
    }

    public class CQWebSokcetData
    {
        public Stream Data { set; get; }

    }
}
