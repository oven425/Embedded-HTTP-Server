﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNetwork.Http.Server.Service
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CQServiceSetting : Attribute
    {
        public CQServiceSetting()
        {
        }
        public string[] Methods { set; get; }
        //public string Method { set; get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CQServiceRoot : Attribute
    {
        public CQServiceRoot(string root)
        {
            this.Root = root;
        }
        public override string ToString()
        {
            return this.Root;
        }
        public string Root { set; get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CQServiceMethod : Attribute
    {
        public CQServiceMethod(string local_path)
        {
            this.LocalPath = local_path;
        }
        public override string ToString()
        {
            return this.LocalPath;
        }
        public string LocalPath { set; get; }
    }
}
