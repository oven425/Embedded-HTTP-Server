using QNetwork.Http.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QNetwork.Http.Server
{
    enum IQResponseReader_ReadStates
    {
        None,
        Header,
        Content
    }
    public interface IQResponseReader
    {
        int Read(byte[] buffer, int offset, int count);
        bool IsEnd { get; }
    }
    public class CQHttpResponseReader : Stream
    {
        CQHttpResponse m_Resp;
        MemoryStream m_HeaderBuf = new MemoryStream();

        IQResponseReader_ReadStates m_ReadState;
        long m_Length;
        public bool Set(CQHttpResponse resp)
        {
            bool result = true;
            this.m_Resp = resp;
            this.m_IsEnd = false;
            this.m_ReadState = IQResponseReader_ReadStates.None;

            return result;
        }
        bool m_IsEnd = true;

        public bool IsEnd { get { return this.m_IsEnd; } }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => this.m_Length;

        public override long Position { set; get; }

        public override void Flush()
        {
            if(this.m_HeaderBuf != null)
            {
                this.m_HeaderBuf.Flush();
            }
            if((this.m_Resp != null) && (this.m_Resp.Content != null))
            {
                this.m_Resp.Content.Flush();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            switch (this.m_ReadState)
            {
                case IQResponseReader_ReadStates.None:
                    {
                        string str_header = this.m_Resp.ToString();
                        byte[] buf = Encoding.UTF8.GetBytes(str_header);
                        this.m_HeaderBuf.SetLength(0);
                        this.m_HeaderBuf.Write(buf, 0, buf.Length);
                        this.m_HeaderBuf.Position = 0;
                        this.m_ReadState = IQResponseReader_ReadStates.Header;
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
                if (this.m_ReadState == IQResponseReader_ReadStates.Header)
                {
                    read_len = this.m_HeaderBuf.Read(buffer, offset, count);
                    if (this.m_HeaderBuf.Position == this.m_HeaderBuf.Length)
                    {
                        this.m_ReadState = IQResponseReader_ReadStates.Content;
                    }
                }
                if (this.m_ReadState == IQResponseReader_ReadStates.Content)
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
