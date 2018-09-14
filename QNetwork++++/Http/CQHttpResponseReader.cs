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


    //public interface Stream:IDisposable
    //{
    //    int Read(byte[] buffer, int offset, int count);
    //    bool IsEnd { get; }

    //}

    //public abstract class CQResponseReader:Stream
    //{
    //    bool IsEnd { get; }
    //}

    public class CQHttpResponseReader : Stream
    {
        CQHttpResponse m_Resp;
        MemoryStream m_HeaderBuf = new MemoryStream();

        IQResponseReader_ReadStates m_ReadState;
        long m_Length = 0;
        long m_Position = 0;
        public bool Set(CQHttpResponse resp)
        {
            bool result = true;
            this.m_Resp = resp;
            this.m_IsEnd = false;
            this.m_ReadState = IQResponseReader_ReadStates.None;
            string str_header = this.m_Resp.ToString();
            this.m_Length = this.m_Length + Encoding.ASCII.GetByteCount(str_header);
            if ((this.m_Resp != null) && (this.m_Resp.Content != null))
            {
                this.m_Length = this.m_Length + (this.m_Resp.Content.Length - this.m_Resp.Content.Position);
            }
            return result;
        }
        bool m_IsEnd = true;

        public bool IsEnd => this.m_IsEnd;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => this.m_Length;

        public override long Position { get => this.m_Position; set { } }

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
                    this.m_Position = this.m_Position + read_len;
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
                        this.m_Position = this.m_Position + rdlen;
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this.m_IsEnd = true;
            if (this.m_Resp.Content != null)
            {
                this.m_Resp.Content.Close();
                this.m_Resp.Content.Dispose();
                this.m_Resp.Content = null;
            }
            if (this.m_HeaderBuf != null)
            {
                this.m_HeaderBuf.Close();
                this.m_HeaderBuf.Dispose();
                this.m_HeaderBuf = null;
            }
            this.m_Resp = null;
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
    }
}
