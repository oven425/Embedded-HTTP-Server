using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNetwork.Http.Server.Service
{
    //[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    //public class CQServiceSetting : Attribute
    //{
    //    public CQServiceSetting()
    //    {
    //    }
    //    public string[] Methods { set; get; }
    //    //public string Method { set; get; }
    //}

    public enum LifeTypes
    {
        Transient,
        Singleton
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CQServiceRoot : Attribute
    {
        public CQServiceRoot()
        {
            this.LifeType = LifeTypes.Singleton;
        }
        public CQServiceRoot(string root)
            : this()
        {
            this.Root = root;
        }
        public override string ToString()
        {
            return this.Root;
        }
        public string Root { set; get; }
        
        public LifeTypes LifeType { set; get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CQServiceMethod : Attribute
    {
        public CQServiceMethod()
        {
            this.LocalPath = "/";
            this.UseLimit = 1;
        }
        public CQServiceMethod(string local_path)
        {
            this.LocalPath = local_path;
        }
        public override string ToString()
        {
            return this.LocalPath;
        }
        public string LocalPath { set; get; }
        public int UseLimit { set; get; }
    }
}
