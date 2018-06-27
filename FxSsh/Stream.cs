using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using FxSsh.Messages.Connection;
using FxSsh.Services;

namespace FxSsh
{
    public class Stream {
        private Session session;

        public Stream(Session session, string localAddress, uint localPort) {
            this.session = session;

            var connectionService = session.GetService<ConnectionService>();

            var channel = connectionService.AddChannel();

            this.session.SendMessage(new ForwardedTcpipMessage("forwarded-tcpip", channel.ServerChannelId, channel.ClientInitialWindowSize,
                                                               channel.ClientMaxPacketSize, connectionService.ForwardAddress,
                                                               connectionService.ForwardPort, localAddress, localPort));
        }

        ~Stream() {
            throw new NotImplementedException();
        }
    }
}
