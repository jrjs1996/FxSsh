using System;
using System.Collections.Generic;
using System.Text;

namespace FxSsh
{
    class Terminal
    {
        public string TERM;
        public UInt32 TerminalWidthCharacters;
        public UInt32 TerminalHeightRows;
        public UInt32 TerminalWidthPixels;
        public UInt32 TerminalHeightPixels;
        public string EncodedTerminalModes;

        public Terminal(string term, UInt32 terminalWidthCharacters, UInt32 terminalHeightRows,
            UInt32 terminalWidthPixels, UInt32 terminalHeightPixels, string encodedTerminalModes)
        {
            TERM = term;

            TerminalWidthCharacters = terminalWidthCharacters;
            TerminalHeightRows = terminalHeightRows;
            TerminalHeightPixels = terminalWidthPixels;
            TerminalHeightPixels = terminalHeightPixels;

            encodedTerminalModes = encodedTerminalModes;
        }
    }
}
