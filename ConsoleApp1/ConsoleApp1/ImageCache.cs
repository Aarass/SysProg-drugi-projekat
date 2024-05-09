using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace ConsoleApp1
{
    internal class ImageCache
    {
        private readonly Dictionary<string, LinkedListNode<ImageData>> _map;
        private readonly LinkedList<ImageData> _list;
        private readonly int _capacity;
        private readonly TimeSpan _ttl;
        private readonly object _lock;

        public ImageCache(int capacity, TimeSpan ttl)
        {
            _map = new Dictionary<string, LinkedListNode<ImageData>>();
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
                if (_list.Count >= _capacity)
                {
                    _map.Remove(_list.Last.Value.ImageName);
                    _list.RemoveLast();
                }

                if (_map.TryGetValue(imageName, out var node))
                {
                    _map.Remove(imageName);
                    _list.Remove(node);
                }

                var newNode = _list.AddFirst(newData);
                _map.Add(imageName, newNode);
            }
        }

        public bool TryGetImage(string imageName, out MemoryStream data)
        {
            LinkedListNode<ImageData> node;
            lock (_lock)
            {
                if (!_map.TryGetValue(imageName, out node))
                {
                    data = null;
                    return false;
                }

                if ((DateTime.Now - node.Value.CreationTime) > _ttl)
                {
                    _map.Remove(imageName);
                    _list.Remove(node);
                    data = null;
                    return false;
                }
            }

            data = node.Value.ActualData;
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
