using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using FxSsh.Services;

namespace FxSsh
{
    public class SshClientConnection {
        private readonly Channel channel;

        public int PortNumber => this.channel.ClientPort;
        public DateTime WhenConnected => this.channel.WhenConnected;

        public SshClientConnection(Channel channel) {
            this.channel = channel;
        }
    }
}
