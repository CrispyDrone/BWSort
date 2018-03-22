using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.Loader
{
    public class DecodeBuffer
    {
        public byte[] Buffer { get; set; }
        public byte[] Result { get; set; }

        public int ResultOffset { get; set; }
        public int DecodedLength { get; set; }

        public int EncodedOffset { get; set; }
        public int EncodedLength { get; set; }


        public DecodeBuffer(byte[] result) 
        {
            Buffer = new byte[0x2000];
    	    Result = result;
        }


        public int BufferLength
        {
            get { return Buffer.Length; }
        }


        public void PutDecodedBytes(byte[] bytes, int offset, int length) 
        {
            if (DecodedLength + length <= BufferLength)
            {
                Array.Copy(bytes, offset, Buffer, DecodedLength, length);
	        }
            DecodedLength += length;
	    }

        public int GetEncodedBytes(byte[] dst, int length) 
        {
            length = Math.Min(EncodedLength - EncodedOffset, length);
            Array.Copy(Result, ResultOffset + EncodedOffset, dst, 0, length);
	        EncodedOffset += length;

	        return length;
	    }

        public void WriteDecodedBytes() 
        {
            Array.Copy(Buffer, 0, Result, ResultOffset, DecodedLength);
            ResultOffset += DecodedLength;
        }
    }
}
