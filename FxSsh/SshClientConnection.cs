using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FxSsh.Services;
using JetBrains.Annotations;

namespace FxSsh
{
    public class SshClientConnection {

        internal readonly SshServerStream stream;

        public int PortNumber { get; private set; }
        // Shouldn't be a reference to channel. B
        public DateTime WhenConnected { get; private set; }

        public SshClientConnection(IPEndPoint localEndPoint, SshClient client) {
            this.WhenConnected = DateTime.Now;

            Socket s = new Socket(AddressFamily.InterNetwork,
                                  SocketType.Stream,
                                  ProtocolType.Tcp);

            this.PortNumber = localEndPoint.Port;
            s.Connect(localEndPoint);
            this.stream = new SshServerStream(s, client);
        }
    }

    public class SshServerStream : NetworkStream {
        private readonly SshClient client;

        public SshServerStream([NotNull] Socket socket, SshClient client) : base(socket) {
            this.client = client;
        }

        public SshServerStream([NotNull] Socket socket, bool ownsSocket, SshClient client) : base(socket, ownsSocket) {
            this.client = client;
        }

        public SshServerStream([NotNull] Socket socket, FileAccess access, SshClient client) : base(socket, access) {
            this.client = client;
        }

        public SshServerStream([NotNull] Socket socket, FileAccess access, bool ownsSocket, SshClient client) : base(socket, access, ownsSocket) {
            this.client = client;
        }

        protected override void Dispose(bool disposing) {
            this.client.Connections.Remove(this.client.Connections.FirstOrDefault(c => c.stream == this));
            base.Dispose(disposing);
        }
    }
}
