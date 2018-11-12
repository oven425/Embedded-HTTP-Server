using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNetwork.Http.Server
{
    public class CQRouterData
    {
        public List<string> Urls { set; get; }
        public Type Service { set; get; }
        public CQRouterData()
        {
            this.Urls = new List<string>();
        }
    }
}
