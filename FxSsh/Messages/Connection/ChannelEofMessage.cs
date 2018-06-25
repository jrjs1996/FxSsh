namespace FxSsh.Messages.Connection {
    [Message("SSH_MSG_CHANNEL_EOF", messageNumber)]
    public class ChannelEofMessage : ConnectionServiceMessage {
        private const byte messageNumber = 96;

        public uint RecipientChannel { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.RecipientChannel = reader.ReadUInt32();
        }

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.RecipientChannel);
        }
    }
}