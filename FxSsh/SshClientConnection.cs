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
        // Shouldn't be a reference to channel. B
        public DateTime WhenConnected { get; private set; }

        public SshClientConnection(int port, SshClient client) {
            this.WhenConnected = DateTime.Now;
            
            Socket s = new Socket(AddressFamily.InterNetwork,
                                  SocketType.Stream,
                                  ProtocolType.Tcp);
            
            s.Connect(client.Session.remoteAddress, port);
            this.stream = new SshServerStream(s, client);
        }
    }
}
