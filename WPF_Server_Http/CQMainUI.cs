using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using QNetwork.Http.Server;
using QNetwork.Http.Server.Accept;
using WPF_Server_Http.Define;

namespace WPF_Server_Http.UIData
{
    public class CQMainUI : INotifyPropertyChanged
    {
        public ObservableCollection<CQCache> Caches { set; get; }
        public ObservableCollection<CQListenAddress> AddressList { set; get; }
        //public ObservableCollection<CQRequest_Service> Request_Services { set; get; }
        string m_Listen_IP;
        int m_Listen_Port;
        public CQMainUI()
        {
            this.Caches = new ObservableCollection<CQCache>();
            //this.Request_Services = new ObservableCollection<CQRequest_Service>();
            this.AddressList = new ObservableCollection<CQListenAddress>();
            this.m_Listen_IP = "127.0.01";
            this.m_Listen_Port = 3333;
        }
        public string Listen_IP { set { this.m_Listen_IP = value; this.Update("Listen_IP"); } get { return this.m_Listen_IP; } }
        public int Listen_Port { set { this.m_Listen_Port = value; this.Update("Listen_Port"); } get { return this.m_Listen_Port; } }
        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }

    
}


