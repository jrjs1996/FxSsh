
using System;
using System.Diagnostics.Contracts;

namespace FxSsh.Services
{
    public class SessionChannel : Channel
    {
        public event EventHandler<MessageReceivedArgs> DataReceived;

        public SessionChannel(ConnectionService connectionService,
            uint clientChannelId, uint clientInitialWindowSize, uint clientMaxPacketSize,
            uint serverChannelId)
            : base(connectionService, clientChannelId, clientInitialWindowSize, clientMaxPacketSize, serverChannelId)
        {

        }

        internal void OnData(byte[] data)
        {
            Contract.Requires(data != null);

            ServerAttemptAdjustWindow((uint)data.Length);

            var args = new MessageReceivedArgs(this, data);

            if (DataReceived != null)
                DataReceived(this, args);
        }
    }
}
