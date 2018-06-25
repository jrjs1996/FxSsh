using System.Text;

namespace FxSsh.Messages.Connection {
    [Message("SSH_MSG_CHANNEL_OPEN_FAILURE", messageNumber)]
    public class ChannelOpenFailureMessage : ConnectionServiceMessage {
        private const byte messageNumber = 92;

        public uint RecipientChannel { get; set; }

        public ChannelOpenFailureReason ReasonCode { get; set; }

        public string Description { get; set; }

        public string Language { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.RecipientChannel);
            writer.Write((uint) this.ReasonCode);
            writer.Write(this.Description, Encoding.ASCII);
            writer.Write(this.Language ?? "en", Encoding.ASCII);
        }
    }
}