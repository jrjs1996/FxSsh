using System;
using System.Collections.Generic;
using System.Text;

namespace FxSsh.Messages.Connection
{
    class TcpipForwardMessage : GlobalRequestMessage
    {
        public string Address { get; protected set; }

        public UInt32 Port { get; protected set; }

        protected override void OnLoad(SshDataWorker reader) {
            base.OnLoad(reader);
            this.Address = reader.ReadString(Encoding.UTF8);
            this.Port = reader.ReadUInt32();
        }
    }
}
