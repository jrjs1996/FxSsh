namespace FxSsh.Messages.Connection {
    [Message("SSH_MSG_CHANNEL_SUCCESS", messageNumber)]
    public class ChannelSuccessMessage : ConnectionServiceMessage {
        private const byte messageNumber = 99;

        public ChannelSuccessMessage() {
        }

        public ChannelSuccessMessage(uint recipientChannel) {
            this.RecipientChannel = recipientChannel;
        }

        public uint RecipientChannel { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.RecipientChannel);
        }
    }
}