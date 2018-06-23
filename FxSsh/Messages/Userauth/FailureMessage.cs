using System;
using System.Text;

namespace FxSsh.Messages.Userauth
{
    [Message("SSH_MSG_USERAUTH_FAILURE", MessageNumber)]
    public class FailureMessage : UserauthServiceMessage
    {
        private const byte MessageNumber = 51;

        public override byte MessageType { get { return MessageNumber; } }

        public string[] NameList { get; set; }

        public bool PartialSuccess { get; set; }

        public FailureMessage() { }

        public FailureMessage(string[] nameList, bool partialSuccess)
        {
            NameList = nameList;
            PartialSuccess = partialSuccess;
        }

        protected override void OnGetPacket(SshDataWorker writer)
        {
            for (var i = 0; i < NameList.Length; i++)
            {
                writer.Write(NameList[i] + ",", Encoding.ASCII);
                if (i < NameList.Length -1)
                    writer.Write(NameList[i], Encoding.ASCII);
            }

            writer.Write(PartialSuccess);
        }
    }
}
