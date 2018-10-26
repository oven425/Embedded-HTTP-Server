using QNetwork.Http.Server;
using QNetwork.Http.Server.Accept;
using QNetwork.Http.Server.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace WPF_Server_Http.Define
{
    public class CQListenAddress : INotifyPropertyChanged
    {
        public bool IsOpen { set; get; }
        CQSocketListen_Address m_Address;
        ListenStates m_ListenState;
        public CQListenAddress()
        {
            this.IsOpen = true;
        }
        public CQSocketListen_Address Address { set { this.m_Address = value; this.Update("Address"); } get { return this.m_Address; } }
        public ListenStates ListenState { set { this.m_ListenState = value; this.Update("ListenState"); } get { return this.m_ListenState; } }

        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }

    public class CQRequest_Service: INotifyPropertyChanged
    {
        CQHttpRequest m_HttpRequest;
        IQHttpService m_HttpService;
        public CQHttpRequest Request { set { this.m_HttpRequest = value; this.Update("Request"); } get { return this.m_HttpRequest; } }
        public IQHttpService Service { set { this.m_HttpService = value; this.Update("Service"); } get { return this.m_HttpService; } }


        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }

    public class CQSession_Process
    {
        CQHttpRequest m_HttpRequest;
        CQHttpResponse m_HttpResponse;
        IQHttpService m_HttpService;
        public CQHttpRequest Request { set { this.m_HttpRequest = value; this.Update("Request"); } get { return this.m_HttpRequest; } }
        public CQHttpResponse Response { set { this.m_HttpResponse = value; this.Update("Response"); } get { return this.m_HttpResponse; } }
        public IQHttpService Service { set { this.m_HttpService = value; this.Update("Service"); } get { return this.m_HttpService; } }


        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }
}
