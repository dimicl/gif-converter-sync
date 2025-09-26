using ImageGIFConverter;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Net;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace ImageGIF
{
    public class WebServer
    {
        private readonly string _prefix;
        private readonly string _rootDir;
        private HttpListener? _listener;
        private readonly GIFService _gifService = new GIFService();
        private readonly Cache _cache = new Cache();

        public WebServer(string prefix, string rootDir)
        {
            _prefix = prefix;
            _rootDir = rootDir;
            Directory.CreateDirectory(_rootDir);
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
                SendError(response, 405, "Dozvoljen je samo GET");
                return;
            }

            string relativePath = request.Url!.AbsolutePath.TrimStart('/');
            if (string.IsNullOrEmpty(relativePath))
            {
                SendError(response, 400, "Morate navesti ime fajla");
                return;
            }


            string filePath = Path.Combine(_rootDir, relativePath);

            if (!File.Exists(filePath))
            {
                SendError(response, 404, "Fajl nije pronadjen");
                return;
            }

            if(_cache.TryGet(filePath, out var cached))
            {
                Console.WriteLine($"[CACHE] {relativePath}");
                response.StatusCode = cached.StatusCode;
                response.ContentType = cached.ContentType;
                response.OutputStream.Write(cached.Body, 0, cached.Body.Length);
                response.OutputStream.Close();
                return;

            }

            try
            {
                using var ms = new MemoryStream();
                _gifService.CreateAnimatedGIF(filePath, ms, 10, 50);
                byte[] gifBytes = ms.ToArray();

                _cache.Set(filePath, 200, "image/gif", gifBytes);

                response.StatusCode = 200;
                response.ContentType = "image/gif";
                response.OutputStream.Write(gifBytes, 0, gifBytes.Length);
                Console.WriteLine($"[OK] Procesuirano {relativePath} kao GIF");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska: {ex.Message}");

                SendError(response, 500, "Greska prilikom proceusiranja");
            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        private void SendError(HttpListenerResponse response, int code, string message)
        {
            response.StatusCode = code;
            response.ContentType = "text/plain; charset=utf-8";
            using var writer = new StreamWriter(response.OutputStream, Encoding.UTF8);
            writer.Write(message);
            writer.Flush();
        }


    }
}
