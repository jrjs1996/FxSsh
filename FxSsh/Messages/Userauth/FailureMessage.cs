using System;
using System.Collections.Generic;
using System.Text;

namespace FxSsh.Messages.Userauth
{
    [Message("SSH_MSG_USERAUTH_FAILURE", MessageNumber)]
    public class FailureMessage : UserauthServiceMessage
    {
        private const byte MessageNumber = 51;

        public override byte MessageType { get { return MessageNumber; } }

        public AuthenticationMethod[] AuthenticationMethods { get; set; }

        public bool PartialSuccess { get; set; }

        public FailureMessage() { }

        public FailureMessage(List<AuthenticationMethod> authenticationMethods, bool partialSuccess)
        {
            AuthenticationMethods = authenticationMethods.ToArray();
            PartialSuccess = partialSuccess;
        }

        protected override void OnGetPacket(SshDataWorker writer)
        {
            if (AuthenticationMethods == null)
            {
                writer.Write("", Encoding.ASCII);
                writer.Write(false);
                return;
            }

            string nameList = "";
            for (var i = 0; i < AuthenticationMethods.Length; i++)
            {
                var authenticationMethodName = GetAuthenticationMethodName(AuthenticationMethods[i]);
                nameList += authenticationMethodName;
                if (i < AuthenticationMethods.Length - 1)
                    nameList += ",";
            }
            writer.Write(nameList, Encoding.ASCII);
            writer.Write(PartialSuccess);
        }

        private string GetAuthenticationMethodName(AuthenticationMethod authenticationMethod)
        {
            switch (authenticationMethod)
            {
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
