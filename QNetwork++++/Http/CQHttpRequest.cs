using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using QNetwork.Http.Server.Log;

namespace QNetwork.Http.Server
{
    public enum POST_States
    {
        Auth,
        Prcocessing,
        Complete
    }
    public class CQHttpRequest : IDisposable
    {
        public IQHttpServer_Log Logger { set; get; }
        public DateTime CreateTime { set; get; }
        public Dictionary<string, string> Headers { set; get; }
        public string Method { set; get; }
        public string ResourcePath { set; get; }
        public string Protocol { set; get; }
        public Stream Content { set; get; }
        string m_HandlerID;
        public string HandlerID { get { return this.m_HandlerID; } }
        public string ProcessID { get { return this.m_ProcessID; } }
        public Uri URL { set; get; }
        string m_Address;
        public byte[] HeaderRaw { set; get; }
        string m_ProcessID;
        public CQHttpRequest(string handlerid, string address)
        {
            this.m_Address = address;
            this.CreateTime = DateTime.Now;
            this.m_HandlerID = handlerid;
            this.m_ProcessID = string.Format("{0}_{1}", this.m_HandlerID, this.CreateTime.Ticks);
            this.Headers = new Dictionary<string, string>();
        }

        static public bool Parse(string src, out Dictionary<string,string> dst)
        {
            bool result = true;
            dst = new Dictionary<string, string>();
            try
            {
                if(string.IsNullOrEmpty(src) == false)
                {
                    if (src[0] == '?')
                    {
                        src = src.Remove(0, 1);
                    }
                    string[] sl_1 = src.Split(new string[] { " & ", " &", "& ", "&" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < sl_1.Length; i++)
                    {
                        string[] sl_2 = sl_1[i].Split(new string[] { " = ", " =", "= ", "=" }, StringSplitOptions.RemoveEmptyEntries);
                        if (sl_2.Length == 2)
                        {
                            if (dst.ContainsKey(sl_2[0]) == true)
                            {
                                dst[sl_2[0]] = sl_2[1];
                            }
                            else
                            {
                                dst.Add(sl_2[0], sl_2[1]);
                            }
                        }
                    }
                }
            }
            catch(Exception ee)
            {
                System.Diagnostics.Trace.WriteLine(ee.Message);
                System.Diagnostics.Trace.WriteLine(ee.StackTrace);
                result = false;
            }

            return result;
        }

        public bool ParseHeader(byte[] data, int data_offset, int len)
        {
            this.HeaderRaw = new byte[len];
            Array.Copy(data, data_offset, this.HeaderRaw, 0, len);
            string str = Encoding.ASCII.GetString(this.HeaderRaw, 0, len);
            return this.ParseHeader(str);
        }

        public bool ParseHeader(string data)
        {
            this.m_ContentLength = 0;
            //System.Diagnostics.Trace.WriteLine(data);
            bool result = true;
            string[] sl = data.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < sl.Length; i++)
            {
                if (i == 0)
                {
                    string[] sl_1 = sl[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (sl_1.Length == 3)
                    {
                        this.Method = sl_1[0];
                        this.ResourcePath = sl_1[1];
                        this.Protocol = sl_1[2];
                    }
                    if(string.IsNullOrEmpty(this.m_Address) == true)
                    {
                        this.m_Address = "127.0.0.1";
                    }
                    this.URL = new Uri(string.Format("http://{0}{1}", this.m_Address, this.ResourcePath));
                }
                else
                {
                    if (string.IsNullOrEmpty(sl[i]) == false)
                    {
                        int index = sl[i].IndexOf(":");
                        string key_str = sl[i].Substring(0, index);
                        while (key_str.Length > 0)
                        {
                            if (key_str.Last() == ' ')
                            {
                                key_str = key_str.Remove(key_str.Length - 1);
                            }
                            else
                            {
                                break;
                            }
                        }
                        string value_str = sl[i].Substring(index + 1, sl[i].Length - (index + 1));
                        while (value_str.Length > 0)
                        {
                            if (value_str.First() == ' ')
                            {
                                value_str = value_str.Remove(0, 1);
                            }
                            else
                            {
                                break;
                            }
                        }
                        key_str = key_str.ToUpperInvariant();
                        if (this.Headers.ContainsKey(key_str) == false)
                        {
                            this.Headers.Add(key_str, value_str);
                        }
                        else
                        {
                            this.Headers[key_str] = value_str;
                        }
                    }
                }
            }
            if(this.Headers.ContainsKey("CONTENT-LENGTH") == true)
            {
                long.TryParse(this.Headers["CONTENT-LENGTH"], out this.m_ContentLength);
            }
            return result;
        }
        long m_ContentLength;
        public long ContentLength { get { return this.m_ContentLength; } }

        public void Dispose()
        {
            if (this.Content != null)
            {
                this.Content.Close();
                this.Content.Dispose();
                this.Content = null;
            }
        }
    }
}
