using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNetwork.Http.Server.Cache
{
    public interface IQCacheIDProvider
    {
        string NickName {get; }
        string NewID();
    }

    public class CQCacheID_Default<T> : IQCacheIDProvider
    {
        string m_NickName;
        public CQCacheID_Default(string nickname = "default")
        {
            this.m_NickName = nickname;
        }
       
        public string NickName => this.m_NickName;

        public string NewID()
        {
            string id = "";

            return id;
        }
    }
}
