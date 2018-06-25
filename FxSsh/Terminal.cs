using System.Collections.Generic;

namespace FxSsh {
    class Terminal {
        public string Term;

        private uint terminalWidthCharacters;

        private uint terminalHeightRows;

        private uint terminalWidthPixels;

        private uint terminalHeightPixels;

        private string encodedTerminalModes;

        private Dictionary<string, string> environmentVariables;

        public Terminal(string term, uint terminalWidthCharacters, uint terminalHeightRows,
                        uint terminalWidthPixels, uint terminalHeightPixels, string encodedTerminalModes) {
            this.Term = term;

            this.terminalWidthCharacters = terminalWidthCharacters;
            this.terminalHeightRows = terminalHeightRows;
            this.terminalWidthPixels = terminalWidthPixels;
            this.terminalHeightPixels = terminalHeightPixels;

            this.encodedTerminalModes = encodedTerminalModes;
            this.environmentVariables = new Dictionary<string, string>();
        }

        public string GetEnvironmentVariable(string variableName) {
            this.environmentVariables.TryGetValue(variableName, out var variableValue);
            return variableValue;
        }

        public void SetEnvironmentVariable(string variableName, string variableValue) => this.environmentVariables[variableName] = variableValue;
    }
}