using System.Text;

namespace FxSsh.Messages.Connection {
    [Message("SSH_MSG_CHANNEL_OPEN", messageNumber)]
    public class ChannelOpenMessage : ConnectionServiceMessage {
        private const byte messageNumber = 90;

        public string ChannelType { get; protected set; }

        public uint SenderChannel { get; protected set; }

        public uint InitialWindowSize { get; protected set; }

        public uint MaximumPacketSize { get; protected set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.ChannelType = reader.ReadString(Encoding.ASCII);
            this.SenderChannel = reader.ReadUInt32();
            this.InitialWindowSize = reader.ReadUInt32();
            this.MaximumPacketSize = reader.ReadUInt32();
        }

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.ChannelType, Encoding.ASCII);
            writer.Write(this.SenderChannel);
            writer.Write(this.InitialWindowSize);
            writer.Write(this.MaximumPacketSize);
        }
    }
}