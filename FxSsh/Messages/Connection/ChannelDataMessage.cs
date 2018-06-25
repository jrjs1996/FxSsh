namespace FxSsh.Messages.Connection {
    [Message("SSH_MSG_CHANNEL_DATA", messageNumber)]
    public class ChannelDataMessage : ConnectionServiceMessage {
        private const byte messageNumber = 94;

        public uint RecipientChannel { get; set; }

        public byte[] Data { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.RecipientChannel = reader.ReadUInt32();
            this.Data = reader.ReadBinary();
        }

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.RecipientChannel);
            writer.WriteBinary(this.Data);
        }
    }
}