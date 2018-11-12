using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QNetwork
{
    public class CQCircleStream<T>where T :Stream,new()
    {
        int m_BufIndex_Write = 0;
        int m_BufIndex_Read = 0;
        List<T> m_Bufs = new List<T>();
        
        public CQCircleStream()
        {
            this.m_Bufs.Add(new T());
            this.m_Bufs.Add(new T());
        }

        public bool WriteByte(byte data)
        {
            bool result = true;
            this.m_Bufs[this.m_BufIndex_Write].WriteByte(data);

            return result;
        }

        public bool Write(byte[] data)
        {
            bool result = true;
            this.m_Bufs[this.m_BufIndex_Write].Write(data, 0, data.Length);

            return result;
        }

        public int Read(byte[] data)
        {
            int len = 0;
            while(data.Length != len)
            {
                if (this.m_BufIndex_Write == this.m_BufIndex_Read)
                {
                    this.m_Bufs[this.m_BufIndex_Read].Position = 0;
                    this.m_BufIndex_Write = this.m_BufIndex_Write == 1 ? 0 : 1;
                    this.m_Bufs[this.m_BufIndex_Write].SetLength(0);
                }
                else if (this.m_Bufs[this.m_BufIndex_Read].Position == this.m_Bufs[this.m_BufIndex_Read].Length)
                {
                    if(this.m_Bufs[this.m_BufIndex_Write].Length == 0)
                    {
                        break;
                    }
                    this.m_BufIndex_Read = this.m_BufIndex_Write;
                    this.m_BufIndex_Write = this.m_BufIndex_Write == 1 ? 0 : 1;
                    this.m_Bufs[this.m_BufIndex_Write].SetLength(0);
                    this.m_Bufs[this.m_BufIndex_Read].Position = 0;
                }

                int read_len = this.m_Bufs[this.m_BufIndex_Read].Read(data, len, data.Length-len);
                len = len + read_len;
            }

            return len;
        }
    }

    
}
