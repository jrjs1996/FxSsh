namespace FxSsh.Messages {
    [Message("SSH_MSG_KEXDH_REPLY", messageNumber)]
    public class KeyExchangeDhReplyMessage : Message {
        private const byte messageNumber = 31;

        public byte[] HostKey { get; set; }

        public byte[] F { get; set; }

        public byte[] Signature { get; set; }

        public override byte MessageType => messageNumber;

        protected override void OnGetPacket(SshDataWorker writer) {
            writer.WriteBinary(this.HostKey);
            writer.WriteMpint(this.F);
            writer.WriteBinary(this.Signature);
        }
    }
}