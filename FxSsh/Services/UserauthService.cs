﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using FxSsh.Messages;
using FxSsh.Messages.Userauth;

namespace FxSsh.Services {
    public class UserauthService : SshService {
        public UserauthService(Session session)
                : base(session) {
        }

        public event EventHandler<UserauthArgs> Userauth;

        public event EventHandler<string> Succeed;

        protected internal override void CloseService() {
        }

        internal void HandleMessageCore(UserauthServiceMessage message) {
            Contract.Requires(message != null);

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
            if (Session.PublicKeyAlgorithms.ContainsKey(message.KeyAlgorithmName)) {
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

                if (verifed) {
                    this.AuthenticationSuccessful(message, args);
                    return;
                } else {
                    this.Session.SendMessage(new FailureMessage());
                    throw new SshConnectionException("Authentication fail.",
                                                     DisconnectReason.NoMoreAuthMethodsAvailable);
                }
            }

            this.Session.SendMessage(new FailureMessage());
        }

        private void HandleMessage(NoneRequestMessage message) {
            var args = new UserauthArgs(null, null, null);

            if (this.Session.AuthenticationMethods == null) {
                this.AuthenticationSuccessful(message, args);
                this.Session.SendMessage(new SuccessMessage());
                return;
            }

            var remainingAuthenticationMethods = this.GetRemainingAuthenticationMethods();
            if (remainingAuthenticationMethods.Count == 0) {
                this.AuthenticationSuccessful(message, args);
                this.Session.SendMessage(new SuccessMessage());
                return;
            }

            this.Session.SendMessage(new FailureMessage(remainingAuthenticationMethods, false));
        }

        private void HandleMessage(PasswordRequestMessage message) {
            var args = new UserauthArgs(null, null, null);

            if (this.Session.AuthenticationMethods == null) {
                this.Session.SendMessage(new FailureMessage());
                return;
            }

            if (!this.Session.AuthenticationMethods.ContainsKey(AuthenticationMethod.Password)) {
                this.Session.SendMessage(new FailureMessage());
                return;
            }

            // Handle authentication here
            if (message.Password != "password") {
                this.Session.SendMessage(new FailureMessage());
                return;
            }

            this.Session.AuthenticationMethods[AuthenticationMethod.Password] = true;

            var remainingAuthenticationMethods = this.GetRemainingAuthenticationMethods();
            if (remainingAuthenticationMethods.Count > 0) {
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