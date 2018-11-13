using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel;
using QNetwork.Http.Server.Cache;
using static QNetwork.Http.Server.CQWebSocketResponseReader;

namespace QNetwork.Http.Server
{
    public class CQWebSocket : CQCacheBase
    {
        BackgroundWorker m_Thread;
        CQTCPHandler m_TcpHandler;
        public CQWebSocket()
        {
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


            CQHttpResponse resp = new CQHttpResponse("", "");
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
            //System.Diagnostics.Trace.WriteLine(resp);
            CQHttpResponseReader resp_reader = new CQHttpResponseReader();
            resp_reader.Set(resp);
            this.m_TcpHandler.AddSend(resp_reader);

            return result;
        }

        private bool M_TcpHandler_OnParse(System.IO.Stream data)
        {
            data.Position = 0;
            byte bb = (byte)data.ReadByte();
            int FIN = (bb & 0x80) >> 7;
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
                bb2[i] = (byte)(bb2[i] ^ bb1[j]);
            }
            string str = Encoding.UTF8.GetString(bb2);
            //System.Diagnostics.Trace.WriteLine(str);

            if (this.m_Thread == null)
            {
                this.m_Thread = new BackgroundWorker();
                this.m_Thread.DoWork += M_Thread_DoWork;
            }
            if (this.m_Thread.IsBusy == false)
            {
                this.m_Thread.RunWorkerAsync(str);
            }

            data.SetLength(0);
            return true;
        }

        private void M_Thread_DoWork(object sender, DoWorkEventArgs e)
        {
            //Stream data = e.Argument as Stream;
            //data.Position = 0;
            //CQWebSocketResponseReader cc = new CQWebSocketResponseReader();
            //byte[] nn = new byte[8192];
            //int read_len = data.Read(nn, 0, nn.Length);
            //byte[] bbb1 = new byte[read_len];
            //Array.Copy(nn, bbb1, read_len);
            string str = e.Argument as string;
            byte[] bbbbb = new byte[] { 0x81, 0x06, 0x73, 0x65, 0x6e, 0x64, 0x7e, 0x7e };
            CQWebSokcetData wsd = new CQWebSokcetData();
            MemoryStream mm = new MemoryStream();
            wsd.Build(mm, Encoding.UTF8.GetBytes(str));
            mm.Position = 0;
            this.m_TcpHandler.AddSend(mm);
        }
    }

    public class CQWebSocketResponseReader : Stream
    {
        bool m_IsEnd;
        public bool IsEnd => this.m_IsEnd;

        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        MemoryStream m_Data;
        public CQWebSocketResponseReader()
        {
            this.m_IsEnd = false;
        }

        public CQWebSocketResponseReader(byte[] data)
        {
            this.m_IsEnd = false;
            this.m_Data = new MemoryStream(data);
        }
        public void Dispose()
        {

        }

        override public int Read(byte[] buffer, int offset, int count)
        {
            int read_len = 0;
            if (this.m_IsEnd == false)
            {
                read_len = this.m_Data.Read(buffer, offset, count);
                if (this.m_Data.Position == this.m_Data.Length)
                {
                    this.m_IsEnd = true;
                }
            }

            return read_len;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        //}

        public class CQWebSokcetData
        {
            /// <summary>
            /// 1 bit
            /// </summary>
            public byte FIN { set; get; }
            /// <summary>
            /// 4 bit
            /// </summary>
            public byte Opcode { set; get; }
            /// <summary>
            /// 1 bit
            /// </summary>

            public bool EnableMask { set; get; }
            /// <summary>
            /// 0 or 4 bytes
            /// </summary>
            public int MaskKey { set; get; }
            public CQWebSokcetData()
            {
                this.FIN = 1;
                this.Opcode = 0x01;
                this.EnableMask = false;
            }
            public bool Parse(System.IO.Stream data)
            {
                bool result = true;
                data.Position = 0;
                byte bb = (byte)data.ReadByte();
                this.FIN = (byte)((bb & 0x80) >> 7);
                this.Opcode = (byte)(bb & 0x0F);
                bb = (byte)data.ReadByte();

                int mask = (bb & 0x80) >> 7;
                this.EnableMask = mask == 1;
                int payload_len = (bb & 0x7F);


                byte[] bb1 = new byte[4];

                data.Read(bb1, 0, bb1.Length);
                this.MaskKey = BitConverter.ToInt32(bb1, 0);

                byte[] bb2 = new byte[payload_len];
                data.Read(bb2, 0, bb2.Length);
                for (var i = 0; i < payload_len; i++)
                {
                    var j = i % 4;
                    bb2[i] = (byte)(bb2[i] ^ bb1[j]);
                }
                string str = Encoding.UTF8.GetString(bb2);
                System.Diagnostics.Trace.WriteLine(str);
                return result;
            }

            public bool Build(System.IO.Stream data, byte[] payload)
            {
                bool result = true;
                BinaryWriter bw = new BinaryWriter(data);
                byte b1 = (byte)(this.FIN << 7);
                b1 = (byte)(b1 | this.Opcode);
                bw.Write(b1);

                b1 = (byte)(this.EnableMask == true ? 0x80 : 0x00);
                b1 = (byte)(b1 | payload.Length);
                bw.Write(b1);
                bw.Write(payload);
                return result;
            }
        }

    }
}
