

namespace FxSsh.Messages
{
    [Message("SSH_MSG_REQUEST_SUCCESS", messageNumber)]
    public class RequestSuccessMessage : Message
    {
        private const byte messageNumber = 81;

        public override byte MessageType => messageNumber;

        protected override void OnGetPacket(SshDataWorker writer)
        {
        }
    }
}
