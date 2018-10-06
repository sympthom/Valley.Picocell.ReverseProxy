using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valley.Net.Bindings;

namespace Valley.Lora.ReverseProxy
{
    public sealed class GatewayChannel
    {        
        private readonly ITelemetryLogger _logger;
        private IEndPointBinding _binding;
        private DateTime _lastAccessed = DateTime.UtcNow;

        public DateTime LastAccessed => _lastAccessed;

        public IEndPointBinding Binding
        {
            get
            {
                _lastAccessed = DateTime.UtcNow;

                return _binding;
            }
            set
            {
                _binding = value;
            }
        }

        public GatewayChannel(IEndPointBinding binding, ITelemetryLogger logger)
        {
            _binding = binding;
            _logger = logger;
        }
    }
}
