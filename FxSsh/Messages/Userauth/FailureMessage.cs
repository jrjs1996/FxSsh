using System.Collections.Generic;
using System.Text;

namespace FxSsh.Messages.Userauth {
    [Message("SSH_MSG_USERAUTH_FAILURE", messageNumber)]
    public class FailureMessage : UserauthServiceMessage {
        private const byte messageNumber = 51;

        public FailureMessage() {
        }

        public FailureMessage(List<AuthenticationMethod> authenticationMethods, bool partialSuccess) {
            this.AuthenticationMethods = authenticationMethods.ToArray();
            this.PartialSuccess = partialSuccess;
        }

        public override byte MessageType => messageNumber;

        public AuthenticationMethod[] AuthenticationMethods { get; set; }

        public bool PartialSuccess { get; set; }

        protected override void OnGetPacket(SshDataWorker writer) {
            if (this.AuthenticationMethods == null) {
                writer.Write("", Encoding.ASCII);
                writer.Write(false);
                return;
            }

            var nameList = "";
            for (var i = 0; i < this.AuthenticationMethods.Length; i++) {
                var authenticationMethodName = GetAuthenticationMethodName(this.AuthenticationMethods[i]);
                nameList += authenticationMethodName;
                if (i < this.AuthenticationMethods.Length - 1)
                    nameList += ",";
            }
            writer.Write(nameList, Encoding.ASCII);
            writer.Write(this.PartialSuccess);
        }

        private static string GetAuthenticationMethodName(AuthenticationMethod authenticationMethod) {
            switch (authenticationMethod) {
                case AuthenticationMethod.PublicKey:
                    return "publickey";
                case AuthenticationMethod.Password:
                    return "password";
                case AuthenticationMethod.HostBased:
                    return "hostbased";
                default:
                    return "none";
            }
        }
    }
}