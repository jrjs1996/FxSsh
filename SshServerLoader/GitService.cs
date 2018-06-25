using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SshServerLoader {
    public class GitService {
        private Process process;

        private readonly ProcessStartInfo startInfo;

        public GitService(string command, string project) {
            var args = Path.Combine(@"F:\Dev\GitTest\", project + ".git");

            this.startInfo = new ProcessStartInfo(Path.Combine(@"D:\PortableGit\mingw64\libexec\git-core", command + ".exe"), args) {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
        }

        public event EventHandler<byte[]> DataReceived;

        public event EventHandler EofReceived;

        public event EventHandler<uint> CloseReceived;

        public void Start() {
            this.process = Process.Start(this.startInfo);
            Task.Run(() => this.MessageLoop());
        }

        public void OnData(byte[] data) {
            this.process.StandardInput.BaseStream.Write(data, 0, data.Length);
            this.process.StandardInput.BaseStream.Flush();
        }

        public void OnClose() {
            this.process.StandardInput.BaseStream.Close();
        }

        private void MessageLoop() {
            var bytes = new byte[1024 * 64];
            while (true) {
                var len = this.process.StandardOutput.BaseStream.Read(bytes, 0, bytes.Length);
                if (len <= 0)
                    break;

                var data = bytes.Length != len
                                   ? bytes.Take(len).ToArray()
                                   : bytes;
                this.DataReceived?.Invoke(this, data);
            }
            this.EofReceived?.Invoke(this, EventArgs.Empty);
            this.CloseReceived?.Invoke(this, (uint) this.process.ExitCode);
        }
    }
}