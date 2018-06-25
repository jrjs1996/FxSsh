namespace FxSsh.Messages.Connection {
    [Message("SSH_MSG_CHANNEL_FAILURE", messageNumber)]
    public class ChannelFailureMessage : ConnectionServiceMessage {
        private const byte messageNumber = 100;

        public uint RecipientChannel { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.RecipientChannel);
        }
    }
}