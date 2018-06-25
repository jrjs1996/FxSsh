namespace FxSsh.Messages.Connection {
    [Message("SSH_MSG_CHANNEL_WINDOW_ADJUST", messageNumber)]
    public class ChannelWindowAdjustMessage : ConnectionServiceMessage {
        private const byte messageNumber = 93;

        public ChannelWindowAdjustMessage() {
        }

        public ChannelWindowAdjustMessage(uint recipientChannel, uint bytesToAdd) {
            this.RecipientChannel = recipientChannel;
            this.BytesToAdd = bytesToAdd;
        }

        public uint RecipientChannel { get; set; }

        public uint BytesToAdd { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.RecipientChannel = reader.ReadUInt32();
            this.BytesToAdd = reader.ReadUInt32();
        }

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.Write(this.RecipientChannel);
            writer.Write(this.BytesToAdd);
        }
    }
}