using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using FxSsh.Services;

namespace FxSsh
{
    public class SshClient {
        private readonly Session session;

        public SshClient(Session session) {
            this.session = session;
        }

        public string Name => this.session.Username;

        public ImmutableArray<SshClientConnection> Connections {
            get { return this.session.GetService<ConnectionService>()
                    .Channels.Select(c => new SshClientConnection(c)).ToImmutableArray(); }
        }
    }
}
