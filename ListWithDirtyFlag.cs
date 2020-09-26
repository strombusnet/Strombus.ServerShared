using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Strombus.ServerShared
{
    public class ListWithDirtyFlag<T> : ObservableCollection<T>
    {
        private bool _isDirty = false;

        public ListWithDirtyFlag()
        {
        }

        public ListWithDirtyFlag(IEnumerable<T> collection)
        {
            AddRange(collection);
            _isDirty = false;
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                this.Add(item);
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            _isDirty = true;
        }

        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }
            set
            {
                // let the caller reset the isDirty flag
                if (value == false) _isDirty = false;
            }
        }

        static public implicit operator ListWithDirtyFlag<T>(List<T> rhs)
        {
            return new ListWithDirtyFlag<T>(rhs);
        }

        static public implicit operator List<T>(ListWithDirtyFlag<T> rhs)
        {
            return new List<T>(rhs);
        }
    }
}
