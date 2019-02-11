using QNetwork.Http.Server.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QNetwork
{
    public class CQAccepter
    {

    }

    public class CQSocketHandler
    {
        
    }

    public class CQHttpParse
    {
        public static bool Parse(Stream stream, int maxlen, ref CQHttpRequest req)
        {
            bool result = true;
            byte[] data = new byte[maxlen];
            int len = stream.Read(data, 0, data.Length);
            for(int i=0; i<len; i++)
            {
                if ((data[i] == '\r') && (data[i + 1] == '\n') && (data[i + 2] == '\r') && (data[i + 3] == '\n'))
                {
                    //findindex = i + 4;
                    //req_ = new byte[findindex];
                    //Array.Copy(this.m_HeaderBuf, req_, findindex);
                    break;
                }
            }

            return result;
        }
    }
}
