using QNetwork.Http.Server.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace QNetwork
{
    public class CQProtocolStream
    {
        protected int m_WriteIndex;
        protected byte[] m_Buffer;
        public CQProtocolStream(int maxlen)
        {
            this.m_Buffer = new byte[maxlen];
        }

        public virtual bool Parse(byte[] data, int len)
        {
            bool result = true;
            Array.Copy(this.m_Buffer, this.m_WriteIndex, data, 0, len);
            this.m_WriteIndex = this.m_WriteIndex + len;
            return result;
        }
    }

    public class CQProtocolStream_Http: CQProtocolStream
    {
        public CQProtocolStream_Http(int maxlen)
            : base(maxlen)
        {

        }

        enum ParseStates
        {
            Header,
            RecvContent
        }
        ParseStates m_ParseState;

        public bool Parse(byte[] data, int len, List<CQHttpRequest> requests)
        {
            bool result = true;
            base.Parse(data, len);
            int last_index = 0;
            int i = 0;
            for (; i < this.m_WriteIndex - 3; i++)
            {
                if ((this.m_Buffer[i] == '\r') && (this.m_Buffer[i + 1] == '\n') && (this.m_Buffer[i + 2] == '\r') && (this.m_Buffer[i + 3] == '\n'))
                {
                    int findindex = i + 4;
                    CQHttpRequest req = new CQHttpRequest("");
                    req.HeaderRaw = new byte[findindex - last_index];

                    Array.Copy(this.m_Buffer, last_index, req.HeaderRaw, 0, findindex);
                    req.ParseHeader(req.HeaderRaw, 0, req.HeaderRaw.Length);
                    last_index = findindex;

                    
                }
            }


            return result;
        }
    }



    //public class CQCircleStream<T>:Stream where T : Stream, new()
    //{
    //    int m_BufIndex_Write = 0;
    //    int m_BufIndex_Read = 0;
    //    List<T> m_Bufs = new List<T>();

    //    public override bool CanRead => true;

    //    public override bool CanSeek => false;

    //    public override bool CanWrite => true;

    //    public override long Length => this.m_Bufs.Sum(x => x.Length - x.Position);

    //    public override long Position { set; get; }

    //    public CQCircleStream()
    //    {
    //        this.m_Bufs.Add(new T());
    //        this.m_Bufs.Add(new T());
    //    }

    //    public override void WriteByte(byte data)
    //    {
    //        this.m_Bufs[this.m_BufIndex_Write].WriteByte(data);
    //    }

    //    public bool Write(byte[] data)
    //    {
    //        bool result = true;
    //        this.m_Bufs[this.m_BufIndex_Write].Write(data, 0, data.Length);

    //        return result;
    //    }

    //    //public int Read(byte[] data)
    //    //{
    //    //    int len = 0;
    //    //    while (data.Length != len)
    //    //    {
    //    //        if (this.m_BufIndex_Write == this.m_BufIndex_Read)
    //    //        {
    //    //            this.m_Bufs[this.m_BufIndex_Read].Position = 0;
    //    //            this.m_BufIndex_Write = this.m_BufIndex_Write == 1 ? 0 : 1;
    //    //            this.m_Bufs[this.m_BufIndex_Write].SetLength(0);
    //    //        }
    //    //        else if (this.m_Bufs[this.m_BufIndex_Read].Position == this.m_Bufs[this.m_BufIndex_Read].Length)
    //    //        {
    //    //            if (this.m_Bufs[this.m_BufIndex_Write].Length == 0)
    //    //            {
    //    //                break;
    //    //            }
    //    //            this.m_BufIndex_Read = this.m_BufIndex_Write;
    //    //            this.m_BufIndex_Write = this.m_BufIndex_Write == 1 ? 0 : 1;
    //    //            this.m_Bufs[this.m_BufIndex_Write].SetLength(0);
    //    //            this.m_Bufs[this.m_BufIndex_Read].Position = 0;
    //    //        }

    //    //        int read_len = this.m_Bufs[this.m_BufIndex_Read].Read(data, len, data.Length - len);
    //    //        len = len + read_len;
    //    //    }

    //    //    return len;
    //    //}

    //    public override void Flush()
    //    {
    //        foreach(var oo in this.m_Bufs)
    //        {
    //            oo.Flush();
    //        }
    //    }

    //    public override long Seek(long offset, SeekOrigin origin)
    //    {
    //        return 0;
    //    }

    //    public override void SetLength(long value)
    //    {
    //    }

    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        int len = 0;
    //        while ((count-offset) != len)
    //        {
    //            if (this.m_BufIndex_Write == this.m_BufIndex_Read)
    //            {
    //                this.m_Bufs[this.m_BufIndex_Read].Position = 0;
    //                this.m_BufIndex_Write = this.m_BufIndex_Write == 1 ? 0 : 1;
    //                this.m_Bufs[this.m_BufIndex_Write].SetLength(0);
    //            }
    //            else if (this.m_Bufs[this.m_BufIndex_Read].Position == this.m_Bufs[this.m_BufIndex_Read].Length)
    //            {
    //                if (this.m_Bufs[this.m_BufIndex_Write].Length == 0)
    //                {
    //                    break;
    //                }
    //                this.m_BufIndex_Read = this.m_BufIndex_Write;
    //                this.m_BufIndex_Write = this.m_BufIndex_Write == 1 ? 0 : 1;
    //                this.m_Bufs[this.m_BufIndex_Write].SetLength(0);
    //                this.m_Bufs[this.m_BufIndex_Read].Position = 0;
    //            }

    //            int read_len = this.m_Bufs[this.m_BufIndex_Read].Read(buffer, len, buffer.Length - len);
    //            len = len + read_len;
    //        }

    //        return len;
    //    }

    //    public override void Write(byte[] buffer, int offset, int count)
    //    {

    //    }
    //}

    //public class CQCircleStream<T>where T :Stream,new()
    //{
    //    int m_BufIndex_Write = 0;
    //    int m_BufIndex_Read = 0;
    //    List<T> m_Bufs = new List<T>();

    //    public CQCircleStream()
    //    {
    //        this.m_Bufs.Add(new T());
    //        this.m_Bufs.Add(new T());
    //    }

    //    public bool WriteByte(byte data)
    //    {
    //        bool result = true;
    //        this.m_Bufs[this.m_BufIndex_Write].WriteByte(data);

    //        return result;
    //    }

    //    public bool Write(byte[] data)
    //    {
    //        bool result = true;
    //        this.m_Bufs[this.m_BufIndex_Write].Write(data, 0, data.Length);

    //        return result;
    //    }

    //    public int Read(byte[] data)
    //    {
    //        int len = 0;
    //        while(data.Length != len)
    //        {
    //            if (this.m_BufIndex_Write == this.m_BufIndex_Read)
    //            {
    //                this.m_Bufs[this.m_BufIndex_Read].Position = 0;
    //                this.m_BufIndex_Write = this.m_BufIndex_Write == 1 ? 0 : 1;
    //                this.m_Bufs[this.m_BufIndex_Write].SetLength(0);
    //            }
    //            else if (this.m_Bufs[this.m_BufIndex_Read].Position == this.m_Bufs[this.m_BufIndex_Read].Length)
    //            {
    //                if(this.m_Bufs[this.m_BufIndex_Write].Length == 0)
    //                {
    //                    break;
    //                }
    //                this.m_BufIndex_Read = this.m_BufIndex_Write;
    //                this.m_BufIndex_Write = this.m_BufIndex_Write == 1 ? 0 : 1;
    //                this.m_Bufs[this.m_BufIndex_Write].SetLength(0);
    //                this.m_Bufs[this.m_BufIndex_Read].Position = 0;
    //            }

    //            int read_len = this.m_Bufs[this.m_BufIndex_Read].Read(data, len, data.Length-len);
    //            len = len + read_len;
    //        }

    //        return len;
    //    }
    //}


}
