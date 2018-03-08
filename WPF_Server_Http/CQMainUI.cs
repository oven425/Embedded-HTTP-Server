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
        public ObservableCollection<CQNetAddress> AddressList { set; get; }
        public CQMainUI()
        {
            this.AddressList = new ObservableCollection<CQNetAddress>();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name)
        {
            if(this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
