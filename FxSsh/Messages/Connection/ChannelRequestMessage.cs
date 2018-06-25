using System.Text;

namespace FxSsh.Messages.Connection {
    [Message("SSH_MSG_CHANNEL_REQUEST", messageNumber)]
    public class ChannelRequestMessage : ConnectionServiceMessage {
        private const byte messageNumber = 98;

        public uint RecipientChannel { get; set; }

        public string RequestType { get; set; }

        public bool WantReply { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.RecipientChannel = reader.ReadUInt32();
            this.RequestType = reader.ReadString(Encoding.ASCII);
            this.WantReply = reader.ReadBoolean();
        }

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.RecipientChannel);
            writer.Write(this.RequestType, Encoding.ASCII);
            writer.Write(this.WantReply);
        }
    }
}