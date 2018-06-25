namespace FxSsh.Messages.Userauth {
    [Message("SSH_MSG_USERAUTH_SUCCESS", messageNumber)]
    public class SuccessMessage : UserauthServiceMessage {
        private const byte messageNumber = 52;

        public override byte MessageType => messageNumber;

        protected override void OnGetPacket(SshDataWorker writer) {
        }
    }
}