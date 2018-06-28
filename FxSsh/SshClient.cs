using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using FxSsh.Services;

namespace FxSsh
{
    public class SshClient {
        private readonly Session session;

        private List<SshClientConnection> connections;

        internal SshClient(Session session) {
            this.connections = new List<SshClientConnection>();
            this.session = session;
        }

        public Stream Connect(IPEndPoint localEndPoint) {
            var newConnection = new SshClientConnection(localEndPoint, this);
            this.connections.Add(newConnection);
            return newConnection.stream;
        }

        public string Name => this.session.Username;

        public ImmutableArray<SshClientConnection> Connections {
            get { return this.connections.ToImmutableArray(); }
        }
    }
}
