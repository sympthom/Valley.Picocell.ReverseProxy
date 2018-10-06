using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valley.Net.Bindings;

namespace Valley.Lora.ReverseProxy
{
    public sealed class IoTHubDevice
    {
        private readonly string _deviceId;
        private readonly string _iotHubUri;
        private readonly RegistryManager _registryManager;
        private readonly Policy _retryPolicy;
        private readonly CircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly ITelemetryLogger _logger;
        private Device _device;
        private DeviceClient _deviceClient;

        public IoTHubDevice(string iotHubUri, string connectionString, string deviceId, ITelemetryLogger logger)
        {
            _deviceId = deviceId;
            _iotHubUri = iotHubUri;
            _registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            _logger = logger;

            _circuitBreakerPolicy = Policy
                .Handle<IotHubException>()
                .CircuitBreakerAsync(1, TimeSpan.FromSeconds(10), (exception, timespan) => { }, () => { }, () => { });

            _retryPolicy = Policy
                .Handle<IotHubException>(/*e => !(e is BrokenCircuitException)*/)
                .RetryAsync(1, (exception, timeSpan, context) =>
                {
                    Reset().GetAwaiter().GetResult();
                });

            Reset().GetAwaiter().GetResult();
        }

        private async Task Reset()
        {
            _device = await _registryManager.GetDeviceAsync(_deviceId);

            if (_device == null)
            {
                _logger.Warning($"Could not find device '{_deviceId}' registered in IoT Hub", GetType().Name);

                return;
            }

            _deviceClient = DeviceClient.Create(_iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(_deviceId, _device.Authentication.SymmetricKey.PrimaryKey), Microsoft.Azure.Devices.Client.TransportType.Amqp);
        }

        public async Task<Microsoft.Azure.Devices.Client.Message> Receive()
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    if (_deviceClient == null)
                        throw new DeviceNotInitiatedException(_deviceId);

                    var message = await _deviceClient.ReceiveAsync(TimeSpan.FromMilliseconds(15));

                    if (message == null)
                        return null;

                    await _deviceClient.CompleteAsync(message);

                    return message;
                });
            });
        }

        public async Task Send(Microsoft.Azure.Devices.Client.Message message)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    if (_deviceClient == null)
                        throw new DeviceNotInitiatedException(_deviceId);

                    await _deviceClient.SendEventAsync(message);
                });
            });
        }
    }
}
