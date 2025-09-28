using ImageGIFConverter;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Net;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Concurrent;

namespace ImageGIF
{
    public class WebServer
    {
        private readonly string _prefix;
        private readonly string _rootDir;
        private HttpListener? _listener;
        private readonly GIFService _gifService;
        private readonly Cache _cache;
        private readonly ConcurrentDictionary<string, object> _fileLocks = new();

        public WebServer(string prefix, string rootDir)
        {
            _prefix = prefix;
            _rootDir = rootDir;
            Directory.CreateDirectory(_rootDir);
            _gifService = new GIFService();
            _cache = new Cache();
        }

        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_prefix);
            _listener.Start();

            Console.WriteLine($"Server pokrenut na {_prefix}");

            while (_listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(state => HandleRequest((HttpListenerContext)state!), context);
                }
                catch (HttpListenerException)
                {
                    Console.WriteLine("Server je zaustavljen");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Greska: " + ex);
                }
            }
        }

        public void Stop()
        {
            _listener?.Stop();
            _listener?.Close();
        }

        private void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod != "GET")
            {
                Respond(response, 405, "Dozvoljen je samo GET");
                return;
            }

            string relativePath = request.Url!.AbsolutePath.TrimStart('/');
            if (string.IsNullOrEmpty(relativePath))
            {
                Respond(response, 400, "Morate navesti ime fajla");
                return;
            }


            string filePath = Path.Combine(_rootDir, relativePath);
            var fileLock = _fileLocks.GetOrAdd(filePath, _ => new object());

            lock (fileLock)
            {
                if (!File.Exists(filePath))
                {
                    Respond(response, 404, "Fajl nije pronadjen");
                    return;
                }

                if (_cache.TryGet(filePath, out var cached))
                {
                    Console.WriteLine($"[CACHE] {relativePath}");
                    WriteResponse(response, cached.StatusCode, cached.ContentType, cached.Body);
                    return;

                }

                try
                {
                    using var ms = new MemoryStream();
                    _gifService.CreateAnimatedGIF(filePath, ms, 10, 50);
                    byte[] gifBytes = ms.ToArray();

                    _cache.Set(filePath, 200, "image/gif", gifBytes);

                    WriteResponse(response, 200, "image/gif", gifBytes);
                    Console.WriteLine($"[OK] Procesuirano {relativePath} kao GIF");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greska: {ex.Message}");
                    Respond(response, 500, "Greska prilikom proceusiranja");
                }
            }
        }

        private void Respond(HttpListenerResponse response, int code, string message)
        {
            byte[] buf = Encoding.UTF8.GetBytes(message ?? "");
            WriteResponse(response, code, "text/plain; charset=utf-8", buf);
        }

        private void WriteResponse(HttpListenerResponse response, int code, string contentType, byte[] body)
        {
            response.StatusCode = code;
            response.ContentType = contentType ?? "text/plain; charset=utf-8";

            if(body != null && body.Length > 0)
            {
                response.ContentLength64 = body.Length;
                response.OutputStream.Write(body, 0, body.Length);
            }
            response.OutputStream.Close();

        }


    }
}
