namespace FxSsh.Messages {
    [Message("SSH_MSG_KEXINIT", messageNumber)]
    class IgnoreMessage : Message {
        private const byte messageNumber = 2;

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
        }
    }
}