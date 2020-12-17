using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitko.Core.Storage
{
    public sealed record StorageNode
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.Now;
        public long Size { get; set; }
        public StorageNodeType Type { get; private set; }
        private readonly List<StorageNode> _children = new();
        public IEnumerable<StorageNode> Children => _children.ToArray();
        public StorageItem? StorageItem { get; private set; }

        public static StorageNode CreateDirectory(string name, string fullPath,
            IEnumerable<StorageNode>? children = null)
        {
            var node = new StorageNode {Type = StorageNodeType.Directory, Name = name, FullPath = fullPath};
            if (children?.Any() == true)
            {
                node.SetChildren(children);
            }

            return node;
        }

        public static StorageNode CreateStorageItem(StorageItem storageItem)
        {
            return new()
            {
                Type = StorageNodeType.StorageItem,
                Name = storageItem.FileName!,
                FullPath = storageItem.FilePath!,
                Size = storageItem.FileSize,
                LastModified = storageItem.LastModified,
                StorageItem = storageItem
            };
        }

        public void AddChild(StorageNode child)
        {
            if (Type == StorageNodeType.Directory)
            {
                _children.Add(child);
                Size += child.Size;
            }
        }

        public void SetChildren(IEnumerable<StorageNode> children)
        {
            if (Type == StorageNodeType.Directory)
            {
                _children.Clear();
                _children.AddRange(children);
                Size += children.Sum(s => s.Size);
            }
        }

        public string HumanSize
        {
            get
            {
                return Helpers.HumanSize(Size);
            }
        }
    }


    public enum StorageNodeType
    {
        Directory,
        StorageItem
    }
}
