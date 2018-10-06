using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Valley.Net.Bindings.Udp;
using Valley.Net.Protocols.Picocell;
using Valley.Net.Protocols.Picocell.Json;
using Xunit;

namespace Valley.Picocell.ReverseProxy.Test
{
    public sealed class UnitTest1
    {
        private const string COLLECTOR_IP_ADDRESS = "127.0.0.1";
        private const int COLLECTOR_PORT = 1690;
        private const int TIMEOUT_IN_SECONDS = 3;

        [Fact]
        public async Task Test1()
        {
            var packet = new PushDataPacket
            {
                ProtocolVersion = 1,
                Token = 1,
                Eui = "0000000000000001",
                Payload = new PushDataPacketPayload
                {
                    Rxpk = new List<Rxpk>()
                    {
                        new RxpkV2
                        {
                            Time = DateTime.UtcNow,
                            Timestamp = GpsTime.Time,
                            RadioFrequencyChain = 1,
                            Frequency = 868.500000,
                            CrcStatus = CrcStatus.OK,
                            Modulation = Modulation.LORA,
                            DataRate = DatarateIdentifier.SF7BW125,
                            CodingRate = CodeRate.CR_LORA_4_8,
                            Size = 14,
                            Data = "Wg3qoMwpJ5T372B9pxLIs0kbvUs=",
                            RadioSignals = new List<Rsig>()
                            {
                                new Rsig
                                {
                                    Antenna = 0,
                                    Channel = 7,
                                    FineTimestamp = 50000,
                                    EncryptedTimestamp = "asd",
                                    ReceivedSignalStrengthChannel = -75,
                                    LoraSignalToNoiseRatio = 30,
                                }
                            }
                        },
                        new RxpkV2
                        {
                            Time = DateTime.UtcNow,
                            Timestamp = GpsTime.Time,
                            RadioFrequencyChain = 1,
                            Frequency = 868.500000,
                            CrcStatus = CrcStatus.OK,
                            Modulation = Modulation.LORA,
                            DataRate = DatarateIdentifier.SF7BW125,
                            CodingRate = CodeRate.CR_LORA_4_8,
                            Size = 14,
                            Data = "Wg3qoMwpJ5T372B9pxLIs0kbvUs=",
                            RadioSignals = new List<Rsig>()
                            {
                                new Rsig
                                {
                                    Antenna = 1,
                                    Channel = 7,
                                    FineTimestamp = 50000,
                                    EncryptedTimestamp = "asd",
                                    ReceivedSignalStrengthChannel = -75,
                                    LoraSignalToNoiseRatio = 30,
                                }
                            }
                        }
                    },
                    Stat = new StatV2
                    {
                        Lati = 46.24,
                        Long = 3.2523,
                        Alti = 100,
                        Time = DateTime.UtcNow,
                    },
                }
            };

            var resetEvent = new AutoResetEvent(false);

            var binding = new UdpBinding(new PicocellPacketSerializer());
            binding.PacketReceived += (sender, e) => resetEvent.Set();

            await binding.ConnectAsync(new IPEndPoint(IPAddress.Parse(COLLECTOR_IP_ADDRESS), COLLECTOR_PORT));

            await binding.SendAsync(packet);

            Assert.True(resetEvent.WaitOne(TimeSpan.FromSeconds(TIMEOUT_IN_SECONDS)));
        }
    }
}
