using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Threading;
using System.Collections.Concurrent;
using Valley.Net.Bindings;
using Valley.Net.Protocols.Picocell;

namespace Valley.Lora.ReverseProxy
{
    public sealed class GatewayProxy : IObserver<PacketEventArgs>, IDisposable
    {
        private readonly string _gatewayId;
        private readonly ConcurrentDictionary<Direction, GatewayChannel> _gatewayChannels = new ConcurrentDictionary<Direction, GatewayChannel>();
        private readonly IEndPointBinding _cloudBinding;
        private readonly ITelemetryLogger _logger;
        private IDisposable _gatewayStreamSubscription;
        private IDisposable _cleanChannelsSubscription;

        private enum Direction
        {
            Upstream,
            Downstream
        }

        public GatewayProxy(string gatewayId, IObservable<PacketEventArgs> gatewaysStream, IEndPointBinding cloudBinding, ITelemetryLogger logger)
        {
            _gatewayId = gatewayId;
            _cloudBinding = cloudBinding;
            _logger = logger;

            _cloudBinding.PacketReceived += OnPacketReceived;

            _gatewayStreamSubscription = gatewaysStream
                .Subscribe(this);

            //_cleanChannelsSubscription = Observable
            //   .Interval(TimeSpan.FromSeconds(60))
            //   .Subscribe(x => _gatewayChannels.Where(c => c.Value.LastAccessed < DateTime.UtcNow.AddSeconds(60)).Select(v => v.Key).ToList().ForEach(k => _gatewayChannels.Remove(k)));

            _cloudBinding.ListenAsync(null);
        }

        private void OnPacketReceived(object sender, PacketEventArgs e)
        {
            OnNext(e);
        }

        public void OnNext(PacketEventArgs value)
        {
            switch (value.Packet)
            {
                case PushDataPacket pushDataPacket: Handle(pushDataPacket, value.Binding); break;
                case PullDataPacket pullDataPacket: Handle(pullDataPacket, value.Binding); break;
                case PullRespPacket pullRespPacket: Handle(pullRespPacket, value.Binding); break;
            }
        }

        public void OnError(Exception error)
        {

        }

        public void OnCompleted()
        {

        }

        private void Handle(PushDataPacket packet, IEndPointBinding deviceBinding)
        {
            _logger.Info($"PushData packet received from {deviceBinding.ToString()}", GetType().Name);

            _logger.Info($"Sending PushAck packet to {deviceBinding.ToString()}", GetType().Name);

            _gatewayChannels
                .AddOrUpdate(Direction.Upstream, new GatewayChannel(deviceBinding, _logger), (key, channel) =>
                {
                    //Update binding with new values eg. ports.
                    channel.Binding = deviceBinding;

                    return channel;
                })
                .Binding
                .SendAsync(new PushAckPacket
                {
                    ProtocolVersion = packet.ProtocolVersion,
                    Token = packet.Token,
                });

            _logger.Info($"Sending PushData packet to {_cloudBinding.ToString()}", GetType().Name);

            _cloudBinding.SendAsync(packet);
        }

        private void Handle(PullDataPacket packet, IEndPointBinding deviceBinding)
        {
            _logger.Info($"PullData packet received from {deviceBinding.ToString()}", GetType().Name);

            _logger.Info($"Sending PullAck packet to {deviceBinding.ToString()}", GetType().Name);

            _gatewayChannels
                .AddOrUpdate(Direction.Downstream, new GatewayChannel(deviceBinding, _logger), (key, channel) =>
                {
                    //Update binding with new values eg. ports.
                    channel.Binding = deviceBinding;

                    return channel;
                })
                .Binding
                .SendAsync(new PullAckPacket
                {
                    ProtocolVersion = packet.ProtocolVersion,
                    Token = packet.Token,
                });
        }

        private void Handle(PullRespPacket packet, IEndPointBinding cloudBinding)
        {
            _logger.Info($"PullResp packet received from {cloudBinding.ToString()}", GetType().Name);

            if (!_gatewayChannels.TryGetValue(Direction.Downstream, out var channel))
                return;

            _logger.Info($"Sending PullResp packet to {_gatewayChannels[Direction.Downstream].Binding.ToString()}", GetType().Name);

            _gatewayChannels[Direction.Downstream]
                .Binding
                .SendAsync(packet);
        }

        public void Dispose()
        {
            if (_cleanChannelsSubscription != null)
            {
                _cleanChannelsSubscription.Dispose();
                _cleanChannelsSubscription = null;
            }
        }        
    }
}
