using System.Diagnostics.Contracts;
using System.Text;

namespace FxSsh.Messages {
    [Message("SSH_MSG_DISCONNECT", messageNumber)]
    public class DisconnectMessage : Message {
        private const byte messageNumber = 1;

        public DisconnectMessage() {
        }

        public DisconnectMessage(DisconnectReason reasonCode, string description = "", string language = "en") {
            Contract.Requires(description != null);
            Contract.Requires(language != null);

            this.ReasonCode = reasonCode;
            this.Description = description;
            this.Language = language;
        }

        public DisconnectReason ReasonCode { get; private set; }

        public string Description { get; private set; }

        public string Language { get; private set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.ReasonCode = (DisconnectReason) reader.ReadUInt32();
            this.Description = reader.ReadString(Encoding.UTF8);
            if (reader.DataAvailable >= 4)
                this.Language = reader.ReadString(Encoding.UTF8);
        }

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write((uint) this.ReasonCode);
            writer.Write(this.Description, Encoding.UTF8);
            writer.Write(this.Language ?? "en", Encoding.UTF8);
        }
    }
}