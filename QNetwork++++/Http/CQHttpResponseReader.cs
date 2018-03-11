using QNetwork.Http.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        //public bool IsEmpty { get { return this.m_Resp == null; } }
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
            switch (this.m_ReadState)
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
}
