using Microsoft.Azure.Devices.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Valley.Lora.ReverseProxy
{
    public sealed class DeviceNotInitiatedException : IotHubException
    {
        public DeviceNotInitiatedException(string deviceId) : base($"Device '{deviceId}' is not initiated.")
        {

        }
    }
}
