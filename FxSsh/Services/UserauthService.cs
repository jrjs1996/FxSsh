using FxSsh.Messages;
using FxSsh.Messages.Userauth;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace FxSsh.Services
{
    public class UserauthService : SshService
    {
        public UserauthService(Session session)
            : base(session)
        {
        }

        public event EventHandler<UserauthArgs> Userauth;

        public event EventHandler<string> Succeed;

        protected internal override void CloseService()
        {
        }

        internal void HandleMessageCore(UserauthServiceMessage message)
        {
            Contract.Requires(message != null);

            typeof(UserauthService)
                .GetMethod("HandleMessage", BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new[] {message.GetType()}, null)
                .Invoke(this, new[] {message});
        }

        private void HandleMessage(RequestMessage message)
        {
            switch (message.MethodName)
            {
                case "publickey":
                    var publicKeyMsg = Message.LoadFrom<PublicKeyRequestMessage>(message);
                    HandleMessage(publicKeyMsg);
                    break;
                case "password":
                    var passwordMsg = Message.LoadFrom<PasswordRequestMessage>(message);
                    HandleMessage(passwordMsg);
                    break;
                case "hostbased":
                case "none":
                    var noneMsg = Message.LoadFrom<NoneRequestMessage>(message);
                    HandleMessage(noneMsg);
                    break;
                default:
                    _session.SendMessage(new FailureMessage());
                    break;
            }
        }

        private void HandleMessage(PublicKeyRequestMessage message)
        {
            if (Session._publicKeyAlgorithms.ContainsKey(message.KeyAlgorithmName))
            {
                if (!message.HasSignature)
                {
                    _session.SendMessage(new PublicKeyOkMessage
                    {
                        KeyAlgorithmName = message.KeyAlgorithmName,
                        PublicKey = message.PublicKey
                    });
                    return;
                }

                var keyAlg = Session._publicKeyAlgorithms[message.KeyAlgorithmName](null);
                keyAlg.LoadKeyAndCertificatesData(message.PublicKey);

                var sig = keyAlg.GetSignature(message.Signature);
                var verifed = false;

                using (var worker = new SshDataWorker())
                {
                    worker.WriteBinary(_session.SessionId);
                    worker.Write(message.PayloadWithoutSignature);

                    verifed = keyAlg.VerifyData(worker.ToByteArray(), sig);
                }

                var args = new UserauthArgs(message.KeyAlgorithmName, keyAlg.GetFingerprint(), message.PublicKey);
                if (verifed && Userauth != null)
                {
                    Userauth(this, args);
                    verifed = args.Result;
                }

                if (verifed)
                {
                    _session.RegisterService(message.ServiceName, args);
                    if (Succeed != null)
                        Succeed(this, message.ServiceName);
                    _session.SendMessage(new SuccessMessage());
                    return;
                }
                else
                {
                    _session.SendMessage(new FailureMessage());
                    throw new SshConnectionException("Authentication fail.",
                        DisconnectReason.NoMoreAuthMethodsAvailable);
                }
            }

            _session.SendMessage(new FailureMessage());
        }

        private void HandleMessage(NoneRequestMessage message)
        {
            if (_session.AuthenticationMethods == null)
            {
                _session.SendMessage(new SuccessMessage());
                return;
            }
                   
            var remainingAuthenticationMethods = GetRemainingAuthenticationMethods();
            if (remainingAuthenticationMethods.Count == 0)
            {
                _session.SendMessage(new SuccessMessage());
                return;
            }
               
            _session.SendMessage(new FailureMessage(remainingAuthenticationMethods, false));
        }

        private void HandleMessage(PasswordRequestMessage message)
        {
            if (_session.AuthenticationMethods == null)
            {
                _session.SendMessage(new FailureMessage());
                return;
            }

            if (!_session.AuthenticationMethods.ContainsKey(AuthenticationMethod.Password))
            {
                _session.SendMessage(new FailureMessage());
                return;
            }
            
            // Handle authentication here
            if (message.Password != "password")
            {
                _session.SendMessage(new FailureMessage());
                return;
            }

            _session.AuthenticationMethods[AuthenticationMethod.Password] = true;

            var remainingAuthenticationMethods = GetRemainingAuthenticationMethods();
            if (remainingAuthenticationMethods.Count > 0)
            {
                _session.SendMessage(new FailureMessage(remainingAuthenticationMethods, true));
                return;
            }

            _session.SendMessage(new SuccessMessage());
        }

        private List<AuthenticationMethod> GetRemainingAuthenticationMethods()
        {
            if (_session.AuthenticationMethods == null)
            {
                return new List<AuthenticationMethod>();
            }
            else
            {
                return _session.AuthenticationMethods.Where(m => m.Value == false).Select(m => m.Key).ToList();
            }
        }
    }
}
