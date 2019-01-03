using QNetwork.Http.Server.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

namespace QNetwork.Http.Server.Service
{
    public class CQHttpService_ServerOperate : IQHttpService
    {
        public IQHttpServer_Log Logger { set; get; }
        public IQHttpServer_Extension Extension { set; get; }
        public IQHttpServer_Operation Operate { set; get; }
        //public List<string> Methods => this.m_Methods;
        //List<string> m_Methods = new List<string>();

        public CQHttpService_ServerOperate()
        {
            //this.m_Methods.Add("/Web");
            //this.m_Methods.Add("/GetServerInfo");
        }
        public bool CloseHandler(List<string> handlers)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        [CQServiceMethod("/Web")]
        public bool Web(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
            process_result_code = ServiceProcessResults.None;
            bool result = true;

            process_result_code = ServiceProcessResults.OK;
            if (File.Exists("../ServerOperate/Index.html") == true)
            {
                resp.Set200();
                byte[] bb = File.ReadAllBytes("../ServerOperate/Index.html");
                resp.ContentLength = bb.Length;
                resp.Content = new MemoryStream();
                resp.Content.Write(bb, 0, bb.Length);
                resp.Content.Position = 0;
            }
            else
            {
                resp.Set404();
            }

            return result;
        }

        [CQServiceMethod("/GetServerInfo")]
        public bool GetServerInfo(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        {
            resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
            process_result_code = ServiceProcessResults.None;
            bool result = true;

            ManagementObjectSearcher my = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            foreach (ManagementObject share in my.Get())
            {
                string a = "主機板製造商：" + share["Manufacturer"].ToString();
                string b = "產品：" + share["Product"].ToString();
                string c = "主機板序號：" + share["SerialNumber"].ToString();
            }

            return result;
        }

        //public bool Process(CQHttpRequest req, out CQHttpResponse resp, out ServiceProcessResults process_result_code)
        //{
        //    resp = new CQHttpResponse(req.HandlerID, req.ProcessID);
        //    process_result_code = ServiceProcessResults.None;
        //    bool result = true;
        //    switch(req.URL.LocalPath)
        //    {
        //        case "/Web":
        //            {
        //                process_result_code = ServiceProcessResults.OK;
        //                if (File.Exists("../ServerOperate/Index.html") == true)
        //                {
        //                    resp.Set200();
        //                    byte[] bb = File.ReadAllBytes("../ServerOperate/Index.html");
        //                    resp.ContentLength = bb.Length;
        //                    resp.Content = new MemoryStream();
        //                    resp.Content.Write(bb, 0, bb.Length);
        //                    resp.Content.Position = 0;
        //                }
        //                else
        //                {
        //                    resp.Set404();
        //                }
        //            }
        //            break;
        //        case "/GetServerInfo":
        //            {
        //                ManagementObjectSearcher my = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
        //                foreach (ManagementObject share in my.Get())
        //                {
        //                    string a = "主機板製造商：" + share["Manufacturer"].ToString();
        //                    string b = "產品：" + share["Product"].ToString();
        //                    string c = "主機板序號：" + share["SerialNumber"].ToString();
        //                }
        //            }
        //            break;
        //    }

        //    return result;
        //}

        public bool RegisterCacheManager()
        {
            return true;
        }
    }

}
