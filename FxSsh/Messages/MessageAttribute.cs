using System;
using System.Diagnostics.Contracts;

namespace FxSsh.Messages {
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MessageAttribute : Attribute {
        public MessageAttribute(string name, byte number) {
            Contract.Requires(name != null);

            this.Name = name;
            this.Number = number;
        }

        public string Name { get; }

        public byte Number { get; }
    }
}