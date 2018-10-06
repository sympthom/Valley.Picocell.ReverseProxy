using System;
using System.Net;
using Valley.Net.Bindings.Udp;
using Valley.Net.Protocols.Picocell;

namespace Valley.Lora.ReverseProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            var serializer = new PicocellPacketSerializer();
            var logger = new Logger();

            var manager = new GatewayProxyManager(new GatewayProxyFactory(serializer, logger), new UdpBinding(serializer), new IPEndPoint(IPAddress.Any, 1690), logger);

            manager.Start();

            Console.ReadKey();
        }
    }
}
