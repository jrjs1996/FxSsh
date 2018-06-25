using System.Text;

namespace FxSsh.Messages.Connection {
    [Message("SSH_MSG_CHANNEL_OPEN", messageNumber)]
    public class ChannelOpenMessage : ConnectionServiceMessage {
        private const byte messageNumber = 90;

        public string ChannelType { get; private set; }

        public uint SenderChannel { get; private set; }

        public uint InitialWindowSize { get; private set; }

        public uint MaximumPacketSize { get; private set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.ChannelType = reader.ReadString(Encoding.ASCII);
            this.SenderChannel = reader.ReadUInt32();
            this.InitialWindowSize = reader.ReadUInt32();
            this.MaximumPacketSize = reader.ReadUInt32();
        }
    }
}