namespace FxSsh.Messages.Connection {
    [Message("SSH_MSG_CHANNEL_OPEN_CONFIRMATION", messageNumber)]
    public class ChannelOpenConfirmationMessage : ConnectionServiceMessage {
        private const byte messageNumber = 91;

        public uint RecipientChannel { get; set; }

        public uint SenderChannel { get; set; }

        public uint InitialWindowSize { get; set; }

        public uint MaximumPacketSize { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.RecipientChannel = reader.ReadUInt32();
            this.SenderChannel = reader.ReadUInt32();
            this.InitialWindowSize = reader.ReadUInt32();
            this.MaximumPacketSize = reader.ReadUInt32();
        }

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.RecipientChannel);
            writer.Write(this.SenderChannel);
            writer.Write(this.InitialWindowSize);
            writer.Write(this.MaximumPacketSize);
        }
    }
}