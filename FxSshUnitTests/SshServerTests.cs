using System;
using System.Net;
using NUnit.Framework;
using FxSsh;
using Moq;

namespace FxSshUnitTests
{
    [TestFixture]
    public class SshServerTests
    {
        [Test]
        public void Constructor_InitialAuthenticationMethods_IsEmpty() {
            var sshServer = this.setupSshServer();
            Assert.That(sshServer.AuthenticationMethods, Is.Empty);
        }

        [Test]
        public void Constructor_GetConnectedClients_IsEmpty() {
            var sshServer = this.setupSshServer();
            Assert.That(sshServer.AuthenticationMethods, Is.Empty);
        }

        private SshServer setupSshServer() {
            var localAddress = IPAddress.Parse("169.254.73.253");
            var localEndPoint = new IPEndPoint(localAddress, 22);
            return new SshServer(localEndPoint);
        }
    }
}
