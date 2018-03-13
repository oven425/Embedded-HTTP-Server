using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using QNetwork.Http.Server;

namespace WPF_Server_Http.UIData
{
    public class CQMainUI : INotifyPropertyChanged
    {
        public ObservableCollection<CQListenAddress> AddressList { set; get; }
        public CQMainUI()
        {
            this.AddressList = new ObservableCollection<CQListenAddress>();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }

    public class CQListenAddress : INotifyPropertyChanged
    {

        CQSocketListen_Address m_Address;
        ListenStates m_ListenState;

        public CQSocketListen_Address Address { set { this.m_Address = value; this.Update("Address"); } get { return this.m_Address; } }
        public ListenStates ListenState { set { this.m_ListenState = value; this.Update("ListenState"); } get { return this.m_ListenState; } }

        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }
}


