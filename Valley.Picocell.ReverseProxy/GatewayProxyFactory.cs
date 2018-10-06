using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Valley.Net.Bindings;
using Valley.Net.Protocols.Picocell;

namespace Valley.Lora.ReverseProxy
{
    public sealed class GatewayProxyFactory
    {
        private readonly IPacketSerializer _serializer;
        private readonly ITelemetryLogger _logger;

        public GatewayProxyFactory(IPacketSerializer serializer, ITelemetryLogger logger)
        {
            _serializer = serializer;
            _logger = logger;
        }

        public GatewayProxy CreateBridge(string gatewayId, IObservable<PacketEventArgs> gatewaysStream)
        {
            try
            {
                _logger.Info($"Creating gateway bridge for gateway '{gatewayId}'.", GetType().Name);

                return new GatewayProxy(gatewayId, gatewaysStream.Where(x => FilterByGatewayId(x, gatewayId)), new IoTHubEndpointBinding("lora-a1-we-dev-iothub.azure-devices.net", "HostName=lora-a1-we-dev-iothub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=sTLoWD18ENpQYVj+2hT8E2GcwiYMGDv0ezkQjhA3zyg=", gatewayId, _serializer, _logger), _logger);
            }
            catch(Exception ex)
            {
                throw new Exception($"Could not create gateway bridge for gateway '{gatewayId}'", ex);
            }
        }

        private static Func<PacketEventArgs, string, bool> FilterByGatewayId = (x, gatewayId) =>
        {
            switch (x.Packet)
            {
                case PushDataPacket pushDataPacket: return pushDataPacket.Eui == gatewayId;
                case PullDataPacket pullDataPacket: return pullDataPacket.Eui == gatewayId;
                default: return false;
            };
        };
    }
}
