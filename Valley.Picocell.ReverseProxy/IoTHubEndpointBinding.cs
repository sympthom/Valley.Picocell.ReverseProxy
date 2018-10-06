using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;
using Valley.Net.Bindings;
using Valley.Net.Protocols.Picocell;
using Valley.Net.Protocols.Picocell.Json;

namespace Valley.Lora.ReverseProxy
{
    public sealed class IoTHubEndpointBinding : IEndPointBinding
    {
        private readonly string _deviceId;
        private readonly ConcurrentQueue<INetworkPacket> _queue = new ConcurrentQueue<INetworkPacket>();
        private readonly JsonPacketPayloadSerializer _payloadSerializer = new JsonPacketPayloadSerializer();
        private readonly ITelemetryLogger _logger;
        private CancellationToken _cancellationToken = new CancellationToken();
        private Task _sendThread;
        private Task _receiveThread;
        private readonly IoTHubDevice _device;

        public event EventHandler<PacketEventArgs> PacketReceived;

        public IoTHubEndpointBinding(string iotHubUri, string connectionString, string deviceId, IPacketSerializer serializer, ITelemetryLogger logger) //: base(serializer, logger)
        {
            _device = new IoTHubDevice(iotHubUri, connectionString, deviceId, _logger);
            _deviceId = deviceId;
            _logger = logger;
            _sendThread = Task.Factory.StartNew(async () => await SendWorker(_cancellationToken), _cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            _receiveThread = Task.Factory.StartNew(async () => await ReceiveWorker(_cancellationToken), _cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        public Task ConnectAsync(IPEndPoint endpoint)
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public Task SendAsync(INetworkPacket packet)
        {
            _queue.Enqueue(packet);

            return Task.CompletedTask;
        }

        public bool ListenAsync(IPEndPoint endpoint)
        {
            return true;
        }

        private async Task ReceiveWorker(CancellationToken cancellationToken)
        {
            Microsoft.Azure.Devices.Client.Message message = null;
            INetworkPacket packet = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    message = await _device.Receive();

                    if (message == null)
                        continue;

                    var messageType = MessageType.PULL_RESP;// Enum.Parse(typeof(MessageType), message.Properties["MessageType"]);

                    var data = message.GetBytes();

                    _logger.Info($"Receiving '{messageType}' packet from IoT Hub by deviceid '{_deviceId}'", GetType().Name);

                    switch (messageType)
                    {
                        case MessageType.PULL_RESP:
                            {
                                packet = new PullRespPacket
                                {
                                    ProtocolVersion = int.Parse(message.Properties["ProtocolVersion"]),
                                    //Token = message.Properties["Token"],
                                    Payload = _payloadSerializer.Deserialize<PullRespPacketPayload>(data),
                                };
                            }
                            break;
                        default:
                            {
                                continue;
                            }
                    }

                    PacketReceived(this, new PacketEventArgs(packet, this));
                }
                catch (BrokenCircuitException ex)
                {
                    _logger.Error($"Circuit breaker open -> An error occured while receiving a message from IoT Hub by deviceid '{_deviceId}'", GetType().Name, ex);

                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
                catch (Exception ex)
                {
                    _logger.Error($"An error occured while receiving a message from IoT Hub by deviceid '{_deviceId}'", GetType().Name, ex);
                }
            }
        }

        private async Task SendWorker(CancellationToken cancellationToken)
        {
            INetworkPacket networkPacket = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!_queue.TryDequeue(out networkPacket))
                    {
                        //await Task.Delay(100);

                        continue;
                    }

                    var correlationId = Guid.NewGuid();

                    switch (networkPacket)
                    {
                        case PushDataPacket pushDataPacket:
                            {
                                var data = _payloadSerializer.Serialize(pushDataPacket.Payload);

                                var message = new Microsoft.Azure.Devices.Client.Message(data);
                                message.CorrelationId = correlationId.ToString();
                                message.Properties["EventId"] = correlationId.ToString();
                                message.Properties["CorrelationId"] = correlationId.ToString();
                                message.Properties["DeviceId"] = pushDataPacket.Eui;
                                message.Properties["ProtocolName"] = "Lora Packet Forwarder";
                                message.Properties["ProtocolVersion"] = pushDataPacket.ProtocolVersion.ToString();
                                message.Properties["MessageType"] = pushDataPacket.MessageType.ToString();
                                message.CreationTimeUtc = DateTime.UtcNow;

                                _logger.Info($"Sending PushDataPacket to IoT Hub by deviceid '{_deviceId}'", GetType().Name);

                                await _device.Send(message);
                            }
                            break;
                    }
                }
                catch (BrokenCircuitException ex)
                {
                    _logger.Error($"Circuit breaker open -> An error occured while sending a message to IoT Hub by deviceid '{_deviceId}'. Discarding message", GetType().Name, ex);
                }
                catch (Exception ex)
                {
                    _logger.Error($"An error occured while sending a message to IoT Hub by deviceid '{_deviceId}'", GetType().Name, ex);
                }
            }
        }

        public override string ToString()
        {
            return $"IoT Hub Gateway '{_deviceId}'";
        }
    }
}
