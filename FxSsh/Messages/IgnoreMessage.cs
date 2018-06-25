using System;
using System.Collections.Generic;
using System.Text;

namespace FxSsh.Messages
{
    [Message("SSH_MSG_KEXINIT", MessageNumber)]
    class IgnoreMessage : Message
    {
        private const byte MessageNumber = 2;

        public override byte MessageType { get { return MessageNumber; } }

        protected override void OnLoad(SshDataWorker reader)
        {

        }
    }
}
