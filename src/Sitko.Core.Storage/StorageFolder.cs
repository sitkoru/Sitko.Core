using System.Collections.Generic;

namespace Sitko.Core.Storage
{
    public class StorageFolder : IStorageNode
    {
        private readonly List<IStorageNode> _children = new List<IStorageNode>();
        public string Name { get; }
        public string FullPath { get; }
        public IEnumerable<IStorageNode> Children => _children.ToArray();

        public StorageFolder(string name, string fullPath, IEnumerable<IStorageNode>? children = null)
        {
            Name = name;
            FullPath = fullPath;
            if (children != null)
            {
                _children.AddRange(children);
            }
        }

        public void AddChild(IStorageNode child)
        {
            _children.Add(child);
        }

        public void SetChildren(IEnumerable<IStorageNode> children)
        {
            _children.Clear();
            _children.AddRange(children);
        }
    }
}
