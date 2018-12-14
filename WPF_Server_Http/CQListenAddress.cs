using QNetwork.Http.Server;
using QNetwork.Http.Server.Accept;
using QNetwork.Http.Server.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using QNetwork.Http.Server.Log;

namespace WPF_Server_Http.Define
{
    public class CQListenAddress : INotifyPropertyChanged
    {
        public bool IsOpen { set; get; }
        CQSocketListen_Address m_Address;
        public string ID { set; get; }
        LogStates_Accept m_ListenState;
        public CQListenAddress()
        {
            this.IsOpen = true;
        }
        public CQSocketListen_Address Address { set { this.m_Address = value; this.Update("Address"); } get { return this.m_Address; } }
        public LogStates_Accept ListenState { set { this.m_ListenState = value; this.Update("ListenState"); } get { return this.m_ListenState; } }

        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }

    public class CQCache : INotifyPropertyChanged
    {
        DateTime m_Begin;
        DateTime m_End;
        string m_ID;
        public CQCache()
        {

        }
        public DateTime Begin { set { this.m_Begin = value; this.Update("Begin"); } get { return this.m_Begin; } }
        public DateTime End { set { this.m_End = value; this.Update("End"); } get { return this.m_End; } }
        public string ID { set { this.m_ID = value; this.Update("ID"); } get { return this.m_ID; } }
        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }


    //public class CQRequest_Service: INotifyPropertyChanged
    //{
    //    CQHttpRequest m_HttpRequest;
    //    IQHttpService m_HttpService;
    //    public CQHttpRequest Request { set { this.m_HttpRequest = value; this.Update("Request"); } get { return this.m_HttpRequest; } }
    //    public IQHttpService Service { set { this.m_HttpService = value; this.Update("Service"); } get { return this.m_HttpService; } }

    //    public event PropertyChangedEventHandler PropertyChanged;
    //    void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    //}

    //public class CQHandlerData:INotifyPropertyChanged
    //{
    //    DateTime m_Begin;
    //    DateTime m_End;
    //    public CQHandlerData()
    //    {
    //        this.m_Begin = DateTime.Now;
    //    }
    //    public DateTime Begin { set { this.m_Begin = value; this.Update("Begin"); } get { return this.m_Begin; } }
    //    public DateTime End { set { this.m_End = value; this.Update("End"); } get { return this.m_End; } }
    //    public CQHttpHandler Handler { set; get; }
    //    public event PropertyChangedEventHandler PropertyChanged;
    //    void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    //}

    //public class CQSession_Process
    //{
    //    CQHttpRequest m_HttpRequest;
    //    CQHttpResponse m_HttpResponse;
    //    IQHttpService m_HttpService;
    //    public CQHttpRequest Request { set { this.m_HttpRequest = value; this.Update("Request"); } get { return this.m_HttpRequest; } }
    //    public CQHttpResponse Response { set { this.m_HttpResponse = value; this.Update("Response"); } get { return this.m_HttpResponse; } }
    //    public IQHttpService Service { set { this.m_HttpService = value; this.Update("Service"); } get { return this.m_HttpService; } }


    //    public event PropertyChangedEventHandler PropertyChanged;
    //    void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    //}
}
