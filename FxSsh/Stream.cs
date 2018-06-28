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
        private readonly Session session;

        private readonly Renci.SshNet.SshClient client;

        public Stream(Session session) {
            this.session = session;

            var connectionService = session.GetService<ConnectionService>();

            var channel = connectionService.AddChannel();

            string clientAddress = ((IPEndPoint)this.session.RemoteEndPoint).Address.MapToIPv4().ToString();
            uint clientPort = (uint)((IPEndPoint)this.session.RemoteEndPoint).Port;

            this.session.SendMessage(new ForwardedTcpipMessage("forwarded-tcpip", channel.ServerChannelId, channel.ClientInitialWindowSize,
                                                               channel.ClientMaxPacketSize, connectionService.ForwardAddress,
                                                               connectionService.ForwardPort, clientAddress, clientPort));
        
            this.client = new Renci.SshNet.SshClient(clientAddress, 22, this.session.Username, "password");
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
