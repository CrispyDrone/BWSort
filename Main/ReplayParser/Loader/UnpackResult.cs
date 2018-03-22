using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.Loader
{
    public class UnpackResult
    {

	    public byte[] Identifier { get; private set;}
        public byte[] Header { get; private set; }
        public byte[] Actions { get; private set; }
        public byte[] Map { get; private set; }

	    public UnpackResult(byte[] identifier, byte[] header, byte[] actions, byte[] map) {

		    this.Identifier = identifier;
		    this.Header     = header;
		    this.Actions    = actions;
		    this.Map        = map;
	    }
    }
}
