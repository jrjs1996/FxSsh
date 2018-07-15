using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using FxSsh.Messages;
using FxSsh.Messages.Userauth;
using JetBrains.Annotations;

namespace FxSsh.Services {
    public class UserauthService : SshService {
        public UserauthService(Session session)
                : base(session) {
        }

        public event EventHandler<UserauthArgs> Userauth;

        public event EventHandler<string> Succeed;

        internal string Username { get; private set; }

        protected internal override void CloseService() {
        }

        internal void HandleMessageCore([NotNull] UserauthServiceMessage message) {
            typeof(UserauthService)
                    .GetMethod("HandleMessage", BindingFlags.NonPublic | BindingFlags.Instance, null,
                               new[] {message.GetType()}, null)?.Invoke(this, new object[] {message});
        }

        private void HandleMessage(RequestMessage message) {
            switch (message.MethodName) {
                case "publickey":
                    var publicKeyMsg = Message.LoadFrom<PublicKeyRequestMessage>(message);
                    this.HandleMessage(publicKeyMsg);
                    break;
                case "password":
                    var passwordMsg = Message.LoadFrom<PasswordRequestMessage>(message);
                    this.HandleMessage(passwordMsg);
                    break;
                case "hostbased":
                    break;
                case "none":
                    var noneMsg = Message.LoadFrom<NoneRequestMessage>(message);
                    this.HandleMessage(noneMsg);
                    break;
                default:
                    this.Session.SendMessage(new FailureMessage());
                    break;
            }
        }

        private void HandleMessage(PublicKeyRequestMessage message) {
        
            if (this.Session.AuthenticationMethods == null ||
                !this.Session.AuthenticationMethods.ContainsKey(AuthenticationMethod.PublicKey) ||
                !Session.PublicKeyAlgorithms.ContainsKey(message.KeyAlgorithmName))
            {
                this.Session.SendMessage(new FailureMessage());
                throw new SshConnectionException("Authentication fail.",
                                                 DisconnectReason.NoMoreAuthMethodsAvailable);
            }

            if (!message.HasSignature) {
                this.Session.SendMessage(new PublicKeyOkMessage {
                    KeyAlgorithmName = message.KeyAlgorithmName,
                    PublicKey = message.PublicKey
                });
                return;
            }

            var keyAlg = Session.PublicKeyAlgorithms[message.KeyAlgorithmName](null);
            keyAlg.LoadKeyAndCertificatesData(message.PublicKey);
            var sig = keyAlg.GetSignature(message.Signature);
            bool verifed;
            using (var worker = new SshDataWorker()) {
                worker.WriteBinary(this.Session.SessionId);
                worker.Write(message.PayloadWithoutSignature);
                verifed = keyAlg.VerifyData(worker.ToByteArray(), sig);
            }

            var args = new UserauthArgs(message.KeyAlgorithmName, keyAlg.GetFingerprint(), message.PublicKey);
            if (verifed && this.Userauth != null) {
                this.Userauth(this, args);
                verifed = args.Result;
            }

            if (!verifed) {
                this.Session.SendMessage(new FailureMessage());
                throw new SshConnectionException("Authentication fail.",
                                                 DisconnectReason.NoMoreAuthMethodsAvailable);
            }

            if (this.Session.clientKeyRepository != null &&
                this.Session.clientKeyRepository.GetKeyForClient(this.Username) != null) {
                var clientKey = Encoding.ASCII.GetString(this.Session.clientKeyRepository.GetKeyForClient(this.Username));
                var messageKey = System.Convert.ToBase64String(message.PublicKey);
                if (clientKey == messageKey) {
                    this.AuthenticationSuccessful(message, args, AuthenticationMethod.PublicKey);
                    return;
                }
            } else {
                this.AuthenticationSuccessful(message, args, AuthenticationMethod.PublicKey);
                return;
            }

            throw new SshConnectionException("Authentication fail.",
                                             DisconnectReason.NoMoreAuthMethodsAvailable);
        }

        private void HandleMessage(NoneRequestMessage message) {
            var args = new UserauthArgs(null, null, null);

            this.Username = message.Username;

            if (this.Session.AuthenticationMethods == null) {
                this.AuthenticationSuccessful(message, args);
                return;
            }

            var remainingAuthenticationMethods = this.GetRemainingAuthenticationMethods();
            if (remainingAuthenticationMethods.Count == 0) {
                this.AuthenticationSuccessful(message, args);
                return;
            }

            this.Session.SendMessage(new FailureMessage(remainingAuthenticationMethods, false));
        }

        private void HandleMessage(PasswordRequestMessage message) {
            var args = new UserauthArgs(null, null, null);

            if (this.Session.AuthenticationMethods == null ||
                !this.Session.AuthenticationMethods.ContainsKey(AuthenticationMethod.Password)) {
                this.Session.SendMessage(new FailureMessage());
                return;
            }

            // Handle authentication here
            if (message.Password != "password") {
                this.Session.SendMessage(new FailureMessage());
                return;
            }

            this.AuthenticationSuccessful(message, args, AuthenticationMethod.Password);
        }

        private void AuthenticationSuccessful(RequestMessage message, UserauthArgs args,
                                              AuthenticationMethod method) {
            this.Session.AuthenticationMethods[method] = true;

            var remainingAuthenticationMethods = this.GetRemainingAuthenticationMethods();
            if (remainingAuthenticationMethods.Count > 0)
            {
                this.Session.SendMessage(new FailureMessage(remainingAuthenticationMethods, true));
                return;
            }
            this.AuthenticationSuccessful(message, args);
        }

        private void AuthenticationSuccessful(RequestMessage message, UserauthArgs args) {
            this.Session.RegisterService(message.ServiceName, args);
            this.Succeed?.Invoke(this, message.ServiceName);
            this.Session.SendMessage(new SuccessMessage());
        }

        private List<AuthenticationMethod> GetRemainingAuthenticationMethods() {
            return this.Session.AuthenticationMethods == null ? new List<AuthenticationMethod>() :
                           this.Session.AuthenticationMethods.Where(m => m.Value == false).Select(m => m.Key).ToList();
        }
    }
}