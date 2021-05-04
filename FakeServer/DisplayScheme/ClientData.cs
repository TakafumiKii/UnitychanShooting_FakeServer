using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using FakeServer.Network.Information;
using System.Runtime.CompilerServices;

namespace FakeServer.Network
{
    public class ClientData : INotifyPropertyChanged
    {
        int _Id;
        public int Id { get { return _Id; } set { _Id = value; NotifyPropertyChanged(); } }
        string _IPAddress;
        public string IPAddress { get { return _IPAddress; } set { _IPAddress = value; NotifyPropertyChanged(); } }

        int _UserId;
        public int UserId { get { return _UserId; } set { _UserId = value; NotifyPropertyChanged(); } }
        int _SeriealNo;
        public int Serieal { get { return _SeriealNo; } set { _SeriealNo = value; NotifyPropertyChanged(); ; } }
        string _Name;
        public string Name { get { return _Name; } set { _Name = value; NotifyPropertyChanged(); } }


        internal MessageManager Manager;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
