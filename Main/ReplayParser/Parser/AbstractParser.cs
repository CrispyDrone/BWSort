using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ReplayParser.Parser
{
    public abstract class AbstractParser
    {
        
	    protected byte[] _data;
	    protected ReplayDataInputStream _input;

        public AbstractParser(byte[] data)
        {
            _data = data;
            _input = new ReplayDataInputStream(new MemoryStream(data));
        }

        protected String parseString(int length)
        {
		    byte[] bytes = new byte[length];
		    _input.ReadBytes(ref bytes);
		
		    int size = bytes.Length;
		    for (int i = 0; i < bytes.Length; i++) {
			    if (bytes[i] == 0) {
				    size = i;
				    break;
			    }
		    }

            

            return UTF8Encoding.UTF8.GetString(bytes, 0, size);
	    }
    }
}
