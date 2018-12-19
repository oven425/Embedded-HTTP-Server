using System;
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
}
