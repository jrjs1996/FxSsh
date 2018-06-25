using System.Diagnostics.Contracts;

namespace FxSsh.Services {
    public class MessageReceivedArgs {
        public MessageReceivedArgs(SessionChannel channel, byte[] data) {
            Contract.Requires(channel != null);
            Contract.Requires(data != null);

            this.Channel = channel;
            this.Data = data;
        }

        public SessionChannel Channel { get; }

        public byte[] Data { get; }
    }
}