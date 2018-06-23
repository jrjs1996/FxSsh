using System;
using System.Collections.Generic;
using System.Text;

namespace FxSsh.Messages.Userauth
{
    class PasswordRequestMessage: RequestMessage
    {
        public string Password;

        protected override void OnLoad(SshDataWorker reader)
        {
            base.OnLoad(reader);
            reader.ReadBoolean();
            Password = reader.ReadString(Encoding.UTF8);
        }
    }
}
