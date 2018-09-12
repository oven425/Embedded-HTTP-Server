using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using QNetwork.Http.Server;
using QNetwork.Http.Server.Accept;

namespace WPF_Server_Http.UIData
{
    public class CQMainUI : INotifyPropertyChanged
    {
        public ObservableCollection<CQListenAddress> AddressList { set; get; }
        string m_Listen_IP;
        int m_Listen_Port;
        public CQMainUI()
        {
            this.AddressList = new ObservableCollection<CQListenAddress>();
            this.m_Listen_IP = "127.0.01";
            this.m_Listen_Port = 3333;
        }
        public string Listen_IP { set { this.m_Listen_IP = value; this.Update("Listen_IP"); } get { return this.m_Listen_IP; } }
        public int Listen_Port { set { this.m_Listen_Port = value; this.Update("Listen_Port"); } get { return this.m_Listen_Port; } }
        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }

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
}


