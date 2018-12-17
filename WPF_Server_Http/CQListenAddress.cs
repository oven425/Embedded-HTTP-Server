﻿using QNetwork.Http.Server;
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

    public class CQProcess : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }

    public class CQCache : INotifyPropertyChanged
    {
        LogStates_Cache m_State { set; get; }
        DateTime m_Begin;
        DateTime m_End;
        string m_ManagerID;
        string m_CacheID;
        string m_Name;
        public CQCache()
        {

        }
        public LogStates_Cache State { set { this.m_State = value; this.Update("State"); } get { return this.m_State; } }
        public DateTime Begin { set { this.m_Begin = value; this.Update("Begin"); } get { return this.m_Begin; } }
        public DateTime End { set { this.m_End = value; this.Update("End"); } get { return this.m_End; } }
        public string ManagerID { set { this.m_ManagerID = value; this.Update("ManagerID"); } get { return this.m_ManagerID; } }
        public string CacheID { set { this.m_CacheID = value; this.Update("CacheID"); } get { return this.m_CacheID; } }
        public string Name { set { this.m_Name = value; this.Update("Name"); } get { return this.m_Name; } }
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
