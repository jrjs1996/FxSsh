using System.IO;
using System.Linq;
using System.Net.Sockets;
using JetBrains.Annotations;

namespace FxSsh {
    public class SshServerStream : NetworkStream {
        private readonly SshClient client;

        public SshServerStream([NotNull] Socket socket, [NotNull] SshClient client) : base(socket) {
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
            this.client.connections.Remove(this.client.Connections.FirstOrDefault(c => c.stream == this));
            base.Dispose(disposing);
        }
    }
}