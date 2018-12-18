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

    public class CQProcess : INotifyPropertyChanged
    {
        string m_HandlerID;
        string m_ProcessID;
        string m_Request;
        DateTime m_CreateHandler;
        DateTime m_CreateRequest;
        DateTime m_ProcessRequest;
        DateTime m_CreateResponse;
        DateTime m_ProcessResponse;
        DateTime m_SendResponse;
        DateTime m_SendResponse_Compelete;
        DateTime m_DestoryHandler;
        public string HandlerID { set { this.m_HandlerID = value; this.Update("HandlerID"); } get { return this.m_HandlerID; } }
        public string ProcessID { set { this.m_ProcessID = value; this.Update("ProcessID"); } get { return this.m_ProcessID; } }
        public string Request { set { this.m_Request = value; this.Update("Request"); } get { return this.m_Request; } }
        public DateTime CreateHandler { set { this.m_CreateHandler = value; this.Update("CreateHandler"); } get { return this.m_CreateHandler; } }
        public DateTime CreateRequest { set { this.m_CreateRequest = value; this.Update("CreateRequest"); } get { return this.m_CreateRequest; } }
        public DateTime ProcessRequest { set { this.m_ProcessRequest = value; this.Update("ProcessRequest"); } get { return this.m_ProcessRequest; } }
        public DateTime CreateResponse { set { this.m_CreateResponse = value; this.Update("CreateResponse"); } get { return this.m_CreateResponse; } }
        public DateTime ProcessResponse { set { this.m_ProcessResponse = value; this.Update("ProcessResponse"); } get { return this.m_ProcessResponse; } }
        public DateTime SendResponse { set { this.m_SendResponse = value; this.Update("SendResponse"); } get { return this.m_SendResponse; } }
        public DateTime SendResponse_Compelete { set { this.m_SendResponse_Compelete = value; this.Update("SendResponse_Compelete"); } get { return this.m_SendResponse_Compelete; } }
        public DateTime DestoryHandler { set { this.m_DestoryHandler = value; this.Update("DestoryHandler"); } get { return this.m_DestoryHandler; } }
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

}
