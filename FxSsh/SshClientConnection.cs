using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FxSsh.Services;

namespace FxSsh
{
    public class SshClientConnection {

        internal readonly SshServerStream stream;

        public int PortNumber { get; private set; }

        public DateTime WhenConnected { get; private set; }

        public DateTime LastSeen { get; private set; }

        public SshClientConnection(int port, SshClient client) {
            this.WhenConnected = DateTime.Now;
            
            Socket s = new Socket(AddressFamily.InterNetwork,
                                  SocketType.Stream,
                                  ProtocolType.Tcp);
         
            s.Connect(client.Session.remoteAddress, port);
            s.BeginReceive(new byte[0], 0, 0, SocketFlags.None, this.OnSocketRecieve, new Object());
            this.stream = new SshServerStream(s, client);
        }

        private void OnSocketRecieve(IAsyncResult result) {
            this.LastSeen = DateTime.Now;
        }
    }
}
