using System;
using NUnit.Framework;
using FxSsh;
using Moq;

namespace FxSshUnitTests
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        public void TestMethod1() {
            var mockClient = new Mock<SshClient>();
            mockClient.Name = "root";

            var sshConnection = new SshClientConnection(8000, mockClient);

            Assert.That(mockClient.Name, Is.EqualTo("root"));

            var sshServer = new SshServer();
        }
    }
}
