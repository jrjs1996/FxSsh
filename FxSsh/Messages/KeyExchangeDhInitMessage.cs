namespace FxSsh.Messages {
    [Message("SSH_MSG_KEXDH_INIT", messageNumber)]
    public class KeyExchangeDhInitMessage : Message {
        private const byte messageNumber = 30;

        public byte[] E { get; private set; }

        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader) {
            this.E = reader.ReadMpint();
        }
    }
}