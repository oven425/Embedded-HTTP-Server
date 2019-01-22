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

    public interface IQCacheID_Range<T>
    {
        T Min { get; }
        T Max { get; }
        bool Has(T data);
        bool Enter(T data);
    }

    public class CQCacheID_Range<T>: IQCacheID_Range<T>
    {
        T m_Min;
        T m_Max;
        public T Min { get { return this.m_Min; } }
        public T Max { get { return this.m_Max; } }
        public CQCacheID_Range(T min, T max)
        {
            this.m_Min = min;
            this.m_Max = max;
        }

        virtual public bool Has(T data)
        {
            bool result = false;
            switch(data.GetType().Name)
            {
                case "Int32":
                    {
                        //int vv = data as int;
                    }
                    break;
            }
            //if(data >= m_Min && m_Max<=data)
            //{
            //    result = true;
            //}
            return result;
        }
        public bool Enter(T data)
        {
            bool result = true;

            return result;
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

        public void ResetID(string id)
        {
            byte bb = 0;
            if(byte.TryParse(id, out bb) == true)
            {

            }
        }
    }
}
