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
    public class UserScoreRankData : INotifyPropertyChanged
    {
        int _Rank;
        public int Rank { get { return _Rank + 1; } set { _Rank = value; NotifyPropertyChanged(); } }

        string _Name;
        public string Name { get { return _Name; } set { _Name = value; NotifyPropertyChanged(); } }

        int _UserId;
        public int UserId { get { return _UserId; } set { _UserId = value; NotifyPropertyChanged(); } }

        int _Point;
        public int Point { get { return _Point; } set { _Point = value; NotifyPropertyChanged(); } }

        public UserScoreRankData()
        {

        }

        public UserScoreRankData(UserRankInfo info)
        {
            Rank = info.Rank;
            Name = info.Name;
            UserId = info.UserId;
            Point = info.Point;
        }

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
