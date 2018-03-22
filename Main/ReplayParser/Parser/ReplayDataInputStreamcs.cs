using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ReplayParser.Loader;

namespace ReplayParser.Parser
{
    public class ReplayDataInputStream
    {

	    private BinaryReader _input;

        public int Position { get; private set; }
	
	    public ReplayDataInputStream(Stream stream) {

            _input = new BinaryReader(stream);
		    Position = 0;
	    }
	
	    public byte ReadByte()
        {
            byte value = 0xFF;
            if (_input.BaseStream.Position != _input.BaseStream.Length)
            {
                value = _input.ReadByte();
                Position += sizeof(byte);
            }
		    return value;
	    }

	    public void ReadBytes(ref byte[] bytes)
        {
		
		    bytes = _input.ReadBytes(bytes.Length);
		    Position += bytes.Length;
	    }

	    public int ReadInt()
        {
		
		    int value = _input.ReadInt32();
		    Position += sizeof(int);
		    //return Common.ReverseBytes(value);
            return value;
	    }

	    public long ReadLong()
        {
		
		    long value = _input.ReadInt64();
            Position += sizeof(long);
            //return Common.ReverseBytes(value);
            return value;
	    }

	    public short ReadShort(){
		
		    short value = _input.ReadInt16();
            Position += sizeof(short);
            //return Common.ReverseBytes(value);
            return value;
	    }

	    public short ReadUnsignedByte()
        {
		    byte value = ReadByte();
		    return (short) (value & 0xFF);
	    }

	    public int ReadUnsignedShort()
        {
		
		    short value = ReadShort();
		    return value & 0xFFFF;
	    }

	    public long ReadUnsignedInt()
        {
		
		    int value = _input.ReadInt32();
		    return value & 0xFFFFFFFFL;
	    }

	    public void Close()
        {
		    _input.Close();
	    }

    }
}
