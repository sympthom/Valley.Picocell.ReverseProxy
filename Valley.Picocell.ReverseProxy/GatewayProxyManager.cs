using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Valley.Net.Bindings;
using Valley.Net.Protocols.Picocell;

namespace Valley.Lora.ReverseProxy
{
    public sealed class GatewayProxyManager
    {
        private readonly GatewayProxyFactory _factory;
        private readonly IEndPointBinding _gatewayBinding;
        private readonly IPEndPoint _endpoint;
        private readonly ITelemetryLogger _logger;
        private readonly ConcurrentDictionary<string, GatewayProxy> _gatewayBridges = new ConcurrentDictionary<string, GatewayProxy>();
        private readonly Subject<PacketEventArgs> _packageStream = new Subject<PacketEventArgs>();

        public IObservable<PacketEventArgs> Stream => _packageStream;

        /// <summary>
        /// Constructor for GatewayBridgeManager.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="gatewayBinding">Binding for the incoming gateway events.</param>
        /// <param name="endpoint"></param>
        /// <param name="logger"></param>
        public GatewayProxyManager(GatewayProxyFactory factory, IEndPointBinding gatewayBinding, IPEndPoint endpoint, ITelemetryLogger logger)
        {
            _factory = factory;
            _gatewayBinding = gatewayBinding;
            _endpoint = endpoint;
            _logger = logger;

            _gatewayBinding.PacketReceived += OnPacketReceived;
        }

        private void OnPacketReceived(object sender, PacketEventArgs e)
        {
            try
            {
                switch (e.Packet)
                {
                    case PushDataPacket pushDataPacket: RegisterGatewayBridge(pushDataPacket.Eui); break;
                    case PullDataPacket pullDataPacket: RegisterGatewayBridge(pullDataPacket.Eui); break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("An error has occured while registering a gateway bridge.", GetType().Name, ex);
            }
            finally
            {
                _packageStream.OnNext(e);
            }
        }

        private void RegisterGatewayBridge(string gatewayId)
        {
            _gatewayBridges.GetOrAdd(gatewayId, (x) => _factory.CreateBridge(x, _packageStream));
        }

        public void Start()
        {
            _gatewayBinding.ListenAsync(_endpoint);
        }
    }
}
