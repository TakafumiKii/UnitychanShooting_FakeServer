using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Data;
using System.Collections.ObjectModel;

namespace FakeServer.Utility
{
    public class UIDataCollection<T> : ObservableCollection<T>
    {
        public ReadOnlyObservableCollection<T> ReadOnlyCollection { get; private set; }
        object LockObject = new object();
        object ReadOnlyLockObject = new object();

        public UIDataCollection()
        {
            ReadOnlyCollection = new ReadOnlyObservableCollection<T>(this);
            BindingOperations.EnableCollectionSynchronization(this, LockObject);
            BindingOperations.EnableCollectionSynchronization(ReadOnlyCollection, ReadOnlyLockObject);
        }
        ~UIDataCollection()
        {
            BindingOperations.DisableCollectionSynchronization(this);
            BindingOperations.DisableCollectionSynchronization(ReadOnlyCollection);
        }
    }
}
