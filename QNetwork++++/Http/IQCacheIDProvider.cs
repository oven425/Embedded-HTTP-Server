using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNetwork.Http.Server.Cache
{
    public interface IQCacheIDProvider
    {
        string NickName { get; }
        string GetID();
        void ResetID(string id);
    }

    public abstract class CQCacheIDProvider<T>: IQCacheIDProvider
    {
        protected T m_TempID;
        protected T m_ID;
        protected List<T> m_Resue = new List<T>();
        protected string m_NickName;
        public string NickName { get { return this.m_NickName; } }
        public abstract T NewID();
        public abstract void Reset(T data);

        public abstract void ResetID(string id);

        public abstract string GetID();
    }

    public class CQCacheID_Default : CQCacheIDProvider<int>
    {
        public CQCacheID_Default(string nickname = "default")
        {
            this.m_NickName = nickname;
        }

        public override string GetID()
        {
            throw new NotImplementedException();
        }

        public override int NewID()
        {
            this.m_TempID = this.m_ID;
            if(this.m_Resue.Count > 0)
            {
                if(this.m_TempID > this.m_Resue.Max())
                {
                    this.m_TempID = this.m_Resue.Min();
                }
                else
                {
                    this.m_TempID = this.m_TempID + 1;
                    this.m_ID = this.m_TempID;
                }
            }
            else
            {
                this.m_ID = this.m_ID + 1;
            }
            return this.m_ID;
        }

        public override void Reset(int data)
        {
            this.m_Resue.Add(data);
            this.m_Resue.Sort();
        }

        public override void ResetID(string id)
        {
            throw new NotImplementedException();
        }
    }
}
