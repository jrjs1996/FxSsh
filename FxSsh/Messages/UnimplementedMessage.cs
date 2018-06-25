namespace FxSsh.Messages {
    [Message("SSH_MSG_UNIMPLEMENTED", messageNumber)]
    public class UnimplementedMessage : Message {
        private const byte messageNumber = 3;

        public uint SequenceNumber { get; set; }

        public byte UnimplementedMessageType { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(messageNumber);
            writer.Write(this.SequenceNumber);
        }
    }
}