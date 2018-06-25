namespace FxSsh.Messages {
    [Message("SSH_MSG_NEWKEYS", messageNumber)]
    public class NewKeysMessage : Message {
        private const byte messageNumber = 21;

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
        }

        protected override void OnGetPacket(SshDataWorker writer) {
        }
    }
}