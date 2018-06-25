using System;
using System.Collections.Generic;
using System.Text;

namespace FxSsh.Messages.Connection
{
    class EnvironmentRequestMessage : ChannelRequestMessage {
        public string VariableName { get; private set; }

        public string VariableValue { get; private set; }

        protected override void OnLoad(SshDataWorker reader) {
            base.OnLoad(reader);

            this.VariableName = reader.ReadString(Encoding.ASCII);
            this.VariableValue = reader.ReadString(Encoding.ASCII);
        }
    }
}
