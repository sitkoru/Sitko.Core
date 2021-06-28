using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Sitko.Core.App.Collections
{
    public class OrderedCollection<TItem> : IEnumerable<TItem> where TItem : class, IOrdered
    {
        private ObservableCollection<TItem> _items = new();

        private void UpdateIndex(int newIndex, int oldIndex)
        {
            _items.Move(oldIndex, newIndex);
            FillPositions();
        }

        private void FillPositions()
        {
            foreach (var item in _items)
            {
                item.Position = _items.IndexOf(item);
            }
        }

        public bool CanMoveUp(TItem item)
        {
            return item.Position > 0;
        }

        public bool CanMoveDown(TItem item)
        {
            return item.Position < _items.Count - 1;
        }

        public void MoveUp(TItem item)
        {
            if (CanMoveUp(item))
            {
                UpdateIndex(item.Position - 1, item.Position);
            }
        }


        public void MoveDown(TItem item)
        {
            if (CanMoveDown(item))
            {
                UpdateIndex(item.Position + 1, item.Position);
            }
        }

        public void AddItems(IEnumerable<TItem> items, TItem? neighbor = null, bool after = true)
        {
            foreach (var item in items)
            {
                AddItem(item, neighbor, after);
                // to add all new items in original order we need to insert them one after another   
                neighbor = item;
                if (!after)
                {
                    after = true;
                }
            }
        }

        public void AddItem(TItem item, TItem? neighbor = null, bool after = true)
        {
            neighbor ??= _items.LastOrDefault();

            var position = neighbor is not null ? after ? neighbor.Position + 1 : neighbor.Position : 0;

            _items.Insert(position, item);
            FillPositions();
        }

        public void SetItems(IEnumerable<TItem> items)
        {
            _items = new ObservableCollection<TItem>(items);
            FillPositions();
        }

        public void RemoveItem(TItem item)
        {
            _items.Remove(item);
            FillPositions();
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public interface IOrdered
    {
        int Position { get; set; }
    }
}
