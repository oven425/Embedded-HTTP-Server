using QNetwork.Http.Server.Accept;
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
}
