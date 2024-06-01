using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class ImageCache : IDisposable
    {
        private readonly Dictionary<string, LinkedListNode<ImageData>> _map;
        private readonly LinkedList<ImageData> _list;
        private readonly int _capacity;
        private readonly TimeSpan _ttl;
        private readonly object _lock;
        private readonly int _cleanUpInterval;
        private bool _shouldCleanUp = true;
        private readonly Thread _cleanUpThread;

        public ImageCache(int capacity, TimeSpan ttl)
        {
            _map = new Dictionary<string, LinkedListNode<ImageData>>();
            _list = new LinkedList<ImageData>();
            _lock = new object();

            _capacity = capacity;
            _ttl = ttl;
            _cleanUpInterval = 5000;

            _cleanUpThread = new Thread(CleanUp);
            _cleanUpThread.Start();
        }

        public void AddImage(string imageName, byte[] imageData)
        {
            var newData = new ImageData
            {
                ImageName = imageName,
                ActualData = imageData,
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

        public bool TryGetImage(string imageName, out byte[] imageData)
        {
            LinkedListNode<ImageData> node;
            lock (_lock)
            {
                if (!_map.TryGetValue(imageName, out node))
                {
                    imageData = null;
                    return false;
                }

            }

            imageData = node.Value.ActualData;
            return true;
        }

        public void Print(string prefix)
        { 
            StringBuilder s = new StringBuilder();
            s.Append(prefix);
            lock (_lock)
            {
                s.Append("[");
                foreach (var image in _list)
                {
                    s.Append(image.ImageName);
                    s.Append(" ");
                }
                s.Append("]");
                Console.WriteLine(s);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _map.Clear();
                _list.Clear();
            }
            Console.WriteLine("Cache cleared");
        }

        private void CleanUp()
        {
            while (_shouldCleanUp)
            {
                Thread.Sleep(_cleanUpInterval);
                lock (_lock)
                {
                    Print("Before clean up: ");
                    foreach (var node in _list)
                    {
                        if ((DateTime.Now - node.CreationTime) > _ttl)
                        {
                            _map.Remove(node.ImageName);
                            _list.Remove(node);
                        }
                    }
                    Print("After clean up: ");
                }
            }
        }

        public void Dispose()
        {
            if (_shouldCleanUp != true) return;

            Console.WriteLine("Trying to join clean-up thread...");
            _shouldCleanUp = false;
            _cleanUpThread.Join();
            Console.WriteLine("Successfully joined clean-up thread!");
        }
    }

    internal struct ImageData: IComparable<ImageData>
    {
        public string ImageName;
        public byte[] ActualData;
        public DateTime CreationTime;
        public DateTime LastTimeUsed;
        public int CompareTo(ImageData other)
        {
            return LastTimeUsed.CompareTo(other);
        }
    }
}
