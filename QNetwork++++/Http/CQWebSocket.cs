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
        SocketAsyncEventArgs m_RecvArgs;
        byte[] m_RecvBuf;
        public CQWebSocket()
        {
            m_IsEnd = false;
        }
        public string ID => throw new NotImplementedException();

        public object Data { set; get; }
        bool m_IsEnd;


        Socket m_Socket;
        public bool IsTimeOut(TimeSpan timeout)
        {
            return this.m_IsEnd;
        }

        public bool Open(Socket socket, byte[] data, int len)
        {
            
            bool result = true;
            this.m_Socket = socket;
            this.m_RecvArgs = new SocketAsyncEventArgs();
            this.m_RecvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(M_RecvArgs_Completed);
            this.m_RecvBuf = new byte[this.m_Socket.ReceiveBufferSize];
            this.m_RecvArgs.SetBuffer(this.m_RecvBuf, 0, this.m_RecvBuf.Length);
            
            CQHttpRequest req = new CQHttpRequest("", "");
            req.ParseHeader(data, 0, len);
            string key = req.Headers["Sec-WebSocket-Key"];

            SHA1 sha1 = new SHA1CryptoServiceProvider();//建立一個SHA1
            byte[] source = Encoding.Default.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");//將字串轉為Byte[]
            byte[] crypto = sha1.ComputeHash(source);//進行SHA1加密
            string base64_str = Convert.ToBase64String(crypto);//把加密後的字串從Byte[]轉為字串

            CQHttpResponse resp = new CQHttpResponse("");
            resp.Code = "101";
            resp.Message = "Switching Protocols";
            resp.Protocol = "HTTP/1.1";
            resp.Connection = Connections.Upgrade;
            resp.Headers.Add("Upgrade", "websocket");
            resp.Headers.Add("Sec-WebSocket-Accept", base64_str);
            string resp_str = resp.ToString();
            this.m_Socket.Send(Encoding.ASCII.GetBytes(resp_str));

            bool hr = this.m_Socket.ReceiveAsync(this.m_RecvArgs);

            return result;
        }

        private void M_RecvArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                this.m_IsEnd = true;
            }
            else
            {

            }
        }
    }
}
