using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageGIFConverter
{
    public sealed class Cache
    {
        public sealed class CachedResponse
        {
            public int StatusCode;
            public string ContentType;
            public byte[] Body = [];
        }

        private readonly Dictionary<string, CachedResponse> _map = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public bool TryGet(string key, out CachedResponse cr)
        {
            lock (_lock)
            {
                return _map.TryGetValue(key, out cr!);
            }
        }

        public void Set(string key, int status, string contentType, byte[] body)
        {
            lock (_lock)
            {
                _map[key] = new CachedResponse
                {
                    StatusCode = status,
                    ContentType = contentType,
                    Body = body
                };
            }
        }
    }
}
