using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace FxSsh {
    public class SshDataWorker : IDisposable {
        private readonly MemoryStream ms;

        public SshDataWorker() {
            this.ms = new MemoryStream(512);
        }

        public SshDataWorker(byte[] buffer) {
            Contract.Requires(buffer != null);

            this.ms = new MemoryStream(buffer);
        }

        public long DataAvailable => this.ms.Length - this.ms.Position;

        public void Write(bool value) {
            this.ms.WriteByte(value ? (byte) 1 : (byte) 0);
        }

        public void Write(byte value) {
            this.ms.WriteByte(value);
        }

        public void Write(uint value) {
            var bytes = new[] {(byte) (value >> 24), (byte) (value >> 16), (byte) (value >> 8), (byte) (value & 0xFF)};
            this.ms.Write(bytes, 0, 4);
        }

        public void Write(ulong value) {
            var bytes = new[] {
                (byte) (value >> 56), (byte) (value >> 48), (byte) (value >> 40), (byte) (value >> 32),
                (byte) (value >> 24), (byte) (value >> 16), (byte) (value >> 8), (byte) (value & 0xFF)
            };
            this.ms.Write(bytes, 0, 8);
        }

        public void Write(string str, Encoding encoding) {
            Contract.Requires(str != null);
            Contract.Requires(encoding != null);

            var bytes = encoding.GetBytes(str);
            this.WriteBinary(bytes);
        }

        public void WriteMpint(byte[] data) {
            Contract.Requires(data != null);

            if (data.Length == 1 && data[0] == 0) {
                this.Write(new byte[4]);
            } else {
                var length = (uint) data.Length;
                var high = ((data[0] & 0x80) != 0);
                if (high) {
                    this.Write(length + 1);
                    this.Write(0);
                    this.Write(data);
                } else {
                    this.Write(length);
                    this.Write(data);
                }
            }
        }

        public void Write(byte[] data) {
            Contract.Requires(data != null);

            this.ms.Write(data, 0, data.Length);
        }

        public void WriteBinary(byte[] buffer) {
            Contract.Requires(buffer != null);

            this.Write((uint) buffer.Length);
            this.ms.Write(buffer, 0, buffer.Length);
        }

        public void WriteBinary(byte[] buffer, int offset, int count) {
            Contract.Requires(buffer != null);

            this.Write((uint) count);
            this.ms.Write(buffer, offset, count);
        }

        public bool ReadBoolean() {
            var num = this.ms.ReadByte();

            if (num == -1)
                throw new EndOfStreamException();
            return num != 0;
        }

        public byte ReadByte() {
            var data = this.ReadBinary(1);
            return data[0];
        }

        public uint ReadUInt32() {
            var data = this.ReadBinary(4);
            return (uint) (data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
        }

        public ulong ReadUInt64() {
            var data = this.ReadBinary(8);
            return ((ulong) data[0] << 56 | (ulong) data[1] << 48 | (ulong) data[2] << 40 | (ulong) data[3] << 32 |
                    (ulong) data[4] << 24 | (ulong) data[5] << 16 | (ulong) data[6] << 8 | data[7]);
        }

        public string ReadString(Encoding encoding) {
            Contract.Requires(encoding != null);

            var bytes = this.ReadBinary();
            return encoding.GetString(bytes);
        }

        public byte[] ReadMpint() {
            var data = this.ReadBinary();

            if (data.Length == 0)
                return new byte[1];

            if (data[0] == 0) {
                var output = new byte[data.Length - 1];
                Array.Copy(data, 1, output, 0, output.Length);
                return output;
            }

            return data;
        }

        public byte[] ReadBinary(int length) {
            var data = new byte[length];
            var bytesRead = this.ms.Read(data, 0, length);

            if (bytesRead < length)
                throw new ArgumentOutOfRangeException(nameof(length));

            return data;
        }

        public byte[] ReadBinary() {
            var length = this.ReadUInt32();

            return this.ReadBinary((int) length);
        }

        public byte[] ToByteArray() {
            return this.ms.ToArray();
        }

        public void Dispose() {
            this.ms.Dispose();
        }
    }
}