using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace ConsoleApp1
{
    internal class ImageCache
    {
        private readonly ConcurrentDictionary<string, LinkedListNode<ImageData>> _map;
        private readonly LinkedList<ImageData> _list;
        private readonly int _capacity;
        private readonly TimeSpan _ttl;
        private readonly object _lock;

        public ImageCache(int capacity, TimeSpan ttl)
        {
            _map = new ConcurrentDictionary<string, LinkedListNode<ImageData>>();
            _list = new LinkedList<ImageData>();
            _lock = new object();

            _capacity = capacity;
            _ttl = ttl;
        }

        public void AddImage(string imageName, MemoryStream data)
        {
            var newData = new ImageData
            {
                ImageName = imageName,
                ActualData = data,
                CreationTime = DateTime.Now
            };

            lock (_lock)
            {
                if (_list.Count > _capacity)
                {
                    _map.TryRemove(_list.Last.Value.ImageName, out _);
                    _list.RemoveLast();
                }

                var newNode = _list.AddFirst(newData);
                _map.AddOrUpdate(imageName, newNode,
                    (id, node) =>
                    {
                        _list.Remove(node);
                        return newNode;
                    }
                );
            }
        }

        public bool TryGetImage(string imageName, out MemoryStream data)
        {
            if (!_map.TryGetValue(imageName, out var node))
            {
                data = null;
                return false;
            }
            ImageData imageData = node.Value;

            if ((DateTime.Now - imageData.CreationTime) > _ttl)
            {
                lock (_lock)
                {
                    _map.TryRemove(imageName, out _);
                    _list.Remove(imageData);
                }
                data = null;
                return false;
            }

            data = imageData.ActualData;
            return true;
        }
    }

    internal struct ImageData
    {
        public string ImageName;
        public MemoryStream ActualData;
        public DateTime CreationTime;
    }
}
