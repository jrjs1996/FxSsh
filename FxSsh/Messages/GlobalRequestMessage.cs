using System;
using System.Collections.Generic;
using System.Text;

namespace FxSsh.Messages
{
    [Message("SSH_MSG_GLOBAL_REQUEST", messageNumber)]
    class GlobalRequestMessage : ConnectionServiceMessage {
        private const byte messageNumber = 80;

        public string RequestType { get; protected set; }

        public bool WantReply { get; protected set; }
      
        public override byte MessageType => messageNumber;

        protected override void OnLoad(SshDataWorker reader)
        {
            this.RequestType = reader.ReadString(Encoding.UTF8);
            this.WantReply = reader.ReadBoolean();
        }
    }
}
