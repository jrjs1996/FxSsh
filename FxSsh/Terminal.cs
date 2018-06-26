using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using FxSsh.Services;

namespace FxSsh {
    public class Terminal {
        private string term;

        private uint terminalWidthCharacters;

        private uint terminalHeightRows;

        private uint terminalWidthPixels;

        private uint terminalHeightPixels;

        private string encodedTerminalModes;

        private readonly Channel channel;

        private readonly Dictionary<string, string> environmentVariables;

        private readonly byte[] backspace = { 0x04, 0x08, 0x1b, 0x5b, 0x4b };

        public Terminal(string term, uint terminalWidthCharacters, uint terminalHeightRows,
                        uint terminalWidthPixels, uint terminalHeightPixels, string encodedTerminalModes,
                        Channel channel) {
            this.term = term;

            this.terminalWidthCharacters = terminalWidthCharacters;
            this.terminalHeightRows = terminalHeightRows;
            this.terminalWidthPixels = terminalWidthPixels;
            this.terminalHeightPixels = terminalHeightPixels;

            this.encodedTerminalModes = encodedTerminalModes;
            this.channel = channel;
            this.environmentVariables = new Dictionary<string, string>();
        }

        public string GetEnvironmentVariable(string variableName) {
            this.environmentVariables.TryGetValue(variableName, out var variableValue);
            return variableValue;
        }

        public void SetEnvironmentVariable(string variableName, string variableValue) => this.environmentVariables[variableName] = variableValue;

        public void HandleInput(byte[] data) {
            if (data[0] == 127) {
                this.channel.SendData(this.backspace);
                return;
            }
            this.channel.SendData(data);
        }
    }
}