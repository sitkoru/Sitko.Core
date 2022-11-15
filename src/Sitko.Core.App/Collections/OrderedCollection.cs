using System.Collections;
using System.Collections.ObjectModel;

namespace Sitko.Core.App.Collections;

public class OrderedCollection<TItem> : IEnumerable<TItem> where TItem : class, IOrdered
{
    private ObservableCollection<TItem> items = new();

    public IEnumerator<TItem> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void UpdateIndex(int newIndex, int oldIndex)
    {
        items.Move(oldIndex, newIndex);
        FillPositions();
    }

    private void FillPositions()
    {
        foreach (var item in items)
        {
            item.Position = items.IndexOf(item);
        }
    }

    public bool CanMoveUp(TItem item) => item.Position > 0;

    public bool CanMoveDown(TItem item) => item.Position < items.Count - 1;

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

    public void AddItems(IEnumerable<TItem> newItems, TItem? neighbor = null, bool after = true)
    {
        foreach (var item in newItems)
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
        neighbor ??= items.LastOrDefault();

        var position = neighbor is not null ? after ? neighbor.Position + 1 : neighbor.Position : 0;

        items.Insert(position, item);
        FillPositions();
    }

    public void SetItems(IEnumerable<TItem> newItems)
    {
        items = new ObservableCollection<TItem>(newItems);
        FillPositions();
    }

    public void RemoveItem(TItem item)
    {
        items.Remove(item);
        FillPositions();
    }

    public void Clear() => items.Clear();
}

public interface IOrdered
{
    int Position { get; set; }
}

