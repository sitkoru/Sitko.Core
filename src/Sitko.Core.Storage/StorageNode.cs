using JetBrains.Annotations;
using Sitko.Core.App.Helpers;

namespace Sitko.Core.Storage;

public sealed record StorageNode
{
    private readonly List<StorageNode> children = new();
    [PublicAPI] public string Name { get; set; } = string.Empty;
    [PublicAPI] public string FullPath { get; set; } = string.Empty;
    [PublicAPI] public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
    [PublicAPI] public long Size { get; set; }
    [PublicAPI] public StorageNodeType Type { get; private set; }
    public IEnumerable<StorageNode> Children => children.ToArray();
    [PublicAPI] public StorageItem? StorageItem { get; private set; }

    [PublicAPI] public string HumanSize => FilesHelper.HumanSize(Size);

    [PublicAPI]
    public static StorageNode CreateDirectory(string name, string fullPath,
        IEnumerable<StorageNode>? directoryChildren = null)
    {
        var node = new StorageNode { Type = StorageNodeType.Directory, Name = name, FullPath = fullPath };
        if (directoryChildren is not null)
        {
            node.SetChildren(directoryChildren);
        }

        return node;
    }

    [PublicAPI]
    public static StorageNode CreateStorageItem(StorageItem storageItem) =>
        new()
        {
            Type = StorageNodeType.StorageItem,
            Name = storageItem.FileName,
            FullPath = storageItem.FilePath,
            Size = storageItem.FileSize,
            LastModified = storageItem.LastModified,
            StorageItem = storageItem
        };

    private void AddChild(StorageNode child)
    {
        if (Type == StorageNodeType.Directory)
        {
            children.Add(child);
            Size += child.Size;
        }
    }

    [PublicAPI]
    public void SetChildren(IEnumerable<StorageNode> directoryChildren)
    {
        var directoryChildrenArr = directoryChildren.ToArray();
        if (Type == StorageNodeType.Directory && directoryChildrenArr.Length > 0)
        {
            children.Clear();
            children.AddRange(directoryChildrenArr);
            Size += directoryChildrenArr.Sum(s => s.Size);
        }
    }

    [PublicAPI]
    public void AddItem(StorageItem item)
    {
        var parts = item.FilePath.Split("/");
        var current = this;
        foreach (var part in parts)
        {
            if (part == parts.Last())
            {
                current.AddChild(CreateStorageItem(item));
            }
            else
            {
                var child = current.Children.Where(n => n.Type == StorageNodeType.Directory)
                    .FirstOrDefault(f => f.Name == part);
                if (child == null)
                {
                    child = CreateDirectory(part, PreparePath(Path.Combine(current.FullPath, part)));
                    current.AddChild(child);
                }

                current = child;
            }
        }
    }

    [PublicAPI]
    public void RemoveItem(StorageItem item) => RemoveItem(item.FilePath);

    [PublicAPI]
    public void RemoveItem(string filePath)
    {
        var parts = filePath.Split("/");
        var current = this;
        foreach (var part in parts)
        {
            if (part == parts.Last())
            {
                var child = current.children.FirstOrDefault(c =>
                    c.Type == StorageNodeType.StorageItem && c.StorageItem!.FilePath == filePath);
                if (child != null)
                {
                    current.children.Remove(child);
                }
            }
            else
            {
                var child = current.Children.Where(n => n.Type == StorageNodeType.Directory)
                    .FirstOrDefault(f => f.Name == part);
                if (child == null)
                {
                    return;
                }
            }
        }
    }

    private static string PreparePath(string path) => path.Replace("\\", "/").Replace("//", "/");
}

public enum StorageNodeType
{
    Directory,
    StorageItem
}

