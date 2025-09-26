using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageGIF
{
    internal class Program
    {
        static void Main()
        {
            string rootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "root");

            Directory.CreateDirectory(rootDir);

            Console.WriteLine("Server adresa: http://localhost:5050/");
            Console.WriteLine("Folder slika: " + rootDir);

            var server = new WebServer("http://localhost:5050/", rootDir);
            server.Start();
        }
    }
}
