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
        internal readonly Session Session;

        internal List<SshClientConnection> connections;

        internal SshClient(Session session) {
            this.connections = new List<SshClientConnection>();
            this.Session = session;
        }

        public Stream Connect(int port) {
            var newConnection = new SshClientConnection(port, this);
            this.connections.Add(newConnection);
            return newConnection.stream;
        }

        internal void DisconnectSession() {
            this.Session.Disconnect();
        }

        public string Name => this.Session.Username;

        public ImmutableArray<SshClientConnection> Connections => this.connections.ToImmutableArray();
    }
}
