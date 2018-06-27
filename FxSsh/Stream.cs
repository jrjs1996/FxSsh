using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using FxSsh.Messages.Connection;
using FxSsh.Messages.Userauth;
using FxSsh.Services;
using Renci.SshNet;

namespace FxSsh
{
    public class Stream {
        private Session session;

        private SshClient client;

        public Stream(Session session, string localAddress, uint localPort, Dictionary<string, string> hostKey,
                      List<AuthenticationMethod> authenticationMethods) {
            this.session = session;

            var connectionService = session.GetService<ConnectionService>();

            var channel = connectionService.AddChannel();

            this.session.SendMessage(new ForwardedTcpipMessage("forwarded-tcpip", channel.ServerChannelId, channel.ClientInitialWindowSize,
                                                               channel.ClientMaxPacketSize, connectionService.ForwardAddress,
                                                               connectionService.ForwardPort, localAddress, localPort));
            
            this.client = new SshClient("169.254.73.20", 22, "james", "password");
            this.client.Connect();
            

        }

        public void SendCommand(string command) {
            SshCommand sc = this.client.CreateCommand(command);
            sc.Execute();
            Console.WriteLine(sc.Result);
        }

        ~Stream() {
            throw new NotImplementedException();
        }
    }
}
