using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;


namespace FxSsh
{
    public class SshClient {
        internal readonly Session Session;

        internal List<SshClientConnection> connections;

        public string Name => this.Session.Username;

        public ImmutableArray<SshClientConnection> Connections => this.connections.ToImmutableArray();

        internal SshClient(Session session) {
            this.connections = new List<SshClientConnection>();
            this.Session = session;
        }

        public Stream Connect(int port) {
            if (!this.Session.ReverseConnectionOpen)
                this.Session.StartReverseConnection();
            var newConnection = new SshClientConnection(port, this);
            this.connections.Add(newConnection);
            return newConnection.stream;
        }

        internal void DisconnectSession() {
            this.Session.Disconnect();
        }

        
    }
}
