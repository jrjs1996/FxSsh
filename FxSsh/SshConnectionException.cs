using System;

namespace FxSsh {
    public class SshConnectionException : Exception {
        public SshConnectionException() {
        }

        public SshConnectionException(string message, DisconnectReason disconnectReason = DisconnectReason.None)
                : base(message) {
            this.DisconnectReason = disconnectReason;
        }

        public DisconnectReason DisconnectReason { get; }

        public override string ToString() {
            return string.Format("SSH connection disconnected bacause {0}: {1}");
        }
    }
}