using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNetwork.Http.Server.Cache
{
    public interface IQCacheIDProvider
    {
        string NickName { get; }
        string NextID();
        void ResetID(string id);
    }

    public abstract class CQCacheIDProvider<T>: IQCacheIDProvider
    {
        protected T m_TempID;
        protected T m_ID;
        protected List<T> m_Resue = new List<T>();
        protected string m_NickName;
        public string NickName { get { return this.m_NickName; } }

        public abstract void ResetID(string id);

        public abstract string NextID();
    }

    public class CQCacheID_GUID : IQCacheIDProvider
    {
        protected string m_NickName;
        public string NickName => this.m_NickName;
        public CQCacheID_GUID(string nickname)
        {
            this.m_NickName = nickname;
        }
        public string NextID()
        {
            return Guid.NewGuid().ToString();
        }

        public void ResetID(string id)
        {
        }
    }

    public class CQCacheID_Default : IQCacheIDProvider
    {
        protected string m_NickName;
        List<byte> m_Resuses = new List<byte>();

        public CQCacheID_Default(string nickname = "")
        {
            this.m_NickName = nickname;
        }

        public string NickName => this.m_NickName;
        byte m_Current = byte.MinValue;
        public string NextID()
        {
            byte bb = this.m_Current;
            bb = (byte)(bb + 1);
            return bb.ToString();
        }

        //public override int NewID()
        //{
        //    this.m_TempID = this.m_ID;
        //    if(this.m_Resue.Count > 0)
        //    {
        //        if(this.m_TempID > this.m_Resue.Max())
        //        {
        //            this.m_TempID = this.m_Resue.Min();
        //        }
        //        else
        //        {
        //            this.m_TempID = this.m_TempID + 1;
        //            this.m_ID = this.m_TempID;
        //        }
        //    }
        //    else
        //    {
        //        this.m_ID = this.m_ID + 1;
        //    }
        //    return this.m_ID;
        //}

        public void ResetID(string id)
        {
            byte bb = 0;
            if(byte.TryParse(id, out bb) == true)
            {

            }
        }
    }
}
