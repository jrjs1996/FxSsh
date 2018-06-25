using System.Net;

namespace FxSsh {
    public class StartingInfo {
        public const int DefaultPort = 22;

        public StartingInfo()
                : this(IPAddress.IPv6Any, DefaultPort) {
        }

        public StartingInfo(IPAddress localAddress, int port) {
            this.LocalAddress = localAddress;
            this.Port = port;
        }

        public IPAddress LocalAddress { get; }

        public int Port { get; }
    }
}