using Colorful;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valley.Net.Bindings;
using Console = Colorful.Console;

namespace Valley.Lora.ReverseProxy
{
    public sealed class Logger : ITelemetryLogger
    {
        private readonly static object LockObject = new object();
        private readonly FigletFont _font;
        private readonly Figlet _figlet;

        public Logger()
        {
            //_font = FigletFont.Load("fonts\\small.flf");
            _figlet = new Figlet();
            //_figlet = new Figlet(_font);
        }

        public void Error(string message, string source, Exception e)
        {
            lock (LockObject)
            {
                //Console.WriteLine(_figlet.ToAscii(source), Color.Red);
                Console.Write($"{source} ->\t", Color.Red);
                Console.WriteLine(message, Color.White);
            }
        }

        public void Info(string message, string source)
        {
            lock (LockObject)
            {
                //Console.WriteLine(_figlet.ToAscii(source), Color.Green);
                Console.Write($"{source} ->\t", Color.Green);
                Console.WriteLine(message, Color.White);
            }
        }

        public void Verbose(string message, string source)
        {
            lock (LockObject)
            {
                //Console.WriteLine(_figlet.ToAscii(source), Color.Green);
                Console.Write($"{source} ->\t", Color.Green);
                Console.WriteLine(message, Color.White);
            }
        }

        public void Warning(string message, string source)
        {
            lock (LockObject)
            {
                //Console.WriteLine(_figlet.ToAscii(source), Color.Yellow);
                Console.Write($"{source} ->\t", Color.Yellow);
                Console.WriteLine(message, Color.White);
            }
        }
    }
}
