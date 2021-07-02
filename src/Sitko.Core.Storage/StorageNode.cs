using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sitko.Core.App.Helpers;

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
            if (children?.Any() == true) node.SetChildren(children);

            return node;
        }

        public static StorageNode CreateStorageItem(StorageItem storageItem)
        {
            return new()
            {
                Type = StorageNodeType.StorageItem,
                Name = storageItem.FileName!,
                FullPath = storageItem.FilePath,
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

        public void AddItem(StorageItem item)
        {
            var parts = item.FilePath.Split("/");
            var current = this;
            foreach (var part in parts)
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

        public void RemoveItem(StorageItem item)
        {
            RemoveItem(item.FilePath);
        }

        public void RemoveItem(string filePath)
        {
            var parts = filePath.Split("/");
            var current = this;
            foreach (var part in parts)
                if (part == parts.Last())
                {
                    var children = current._children.FirstOrDefault(c =>
                        c.Type == StorageNodeType.StorageItem && c.StorageItem!.FilePath == filePath);
                    if (children != null) current._children.Remove(children);
                }
                else
                {
                    var child = current.Children.Where(n => n.Type == StorageNodeType.Directory)
                        .FirstOrDefault(f => f.Name == part);
                    if (child == null) return;
                }
        }

        private string PreparePath(string path)
        {
            return path.Replace("\\", "/").Replace("//", "/");
        }

        public string HumanSize => FilesHelper.HumanSize(Size);
    }


    public enum StorageNodeType
    {
        Directory,
        StorageItem
    }
}
