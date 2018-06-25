using System;
using System.Diagnostics.Contracts;

namespace FxSsh.Services {
    public class SessionChannel : Channel {
        public SessionChannel(ConnectionService connectionService,
                              uint clientChannelId, uint clientInitialWindowSize, uint clientMaxPacketSize,
                              uint serverChannelId)
                : base(connectionService, clientChannelId, clientInitialWindowSize, clientMaxPacketSize, serverChannelId) {
        }

        public event EventHandler<MessageReceivedArgs> DataReceived;

        internal void OnData(byte[] data) {
            Contract.Requires(data != null);

            this.ServerAttemptAdjustWindow((uint) data.Length);

            var args = new MessageReceivedArgs(this, data);

            this.DataReceived?.Invoke(this, args);
        }
    }
}