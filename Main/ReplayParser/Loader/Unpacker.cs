using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Mime;
using Ionic.Zip;
using ReplayParser.Entities;
using ReplayParser.Parser;
using Ionic.Zlib;

/*
    TODO:
    Find a clean way to parse both replay types new and old without weird exception catching
    Document better
    Try and understand the actual parsing system better, and maybe add more to it
*/

namespace ReplayParser.Loader
{
    public class Unpacker
    {
        
        protected const int IDENTIFIER_LENGTH   = 4;
        protected const int HEADER_LENGTH       = 633;
        protected const int SECTION_SIZE_LENGTH = 4;

        protected BinaryReader _reader;

        public UnpackResult Unpack(BinaryReader reader)
        {
            _reader = reader;

            UnpackResult result = null;
            try
            {
                byte[] identifier;
                byte[] header;
                byte[] actions;
                byte[] map;

                // unpack the identifier and header
                identifier = ZlibUnpack(IDENTIFIER_LENGTH);
                int identity = Common.ToInteger(identifier);
                if (identity != 1397908850 /*new patch replay */ && identity != 1397908851)
                    throw
                        new Exception(
                            "Not a valid replay file!"); //if a file somehow makes it here before any of the other exceptions (typically would throw insufficient space in decode buffer first)

                /*
                header = UnpackNextSection(HEADER_LENGTH);
                Console.WriteLine(_reader.BaseStream.Position.ToString("X"));

                int actionsSize = Common.ToInteger(UnpackNextSection(SECTION_SIZE_LENGTH));
                actions = UnpackNextSection(actionsSize);

                int mapSize = Common.ToInteger(UnpackNextSection(SECTION_SIZE_LENGTH));
                map = UnpackNextSection(mapSize);
                */

                header = ZlibUnpack(HEADER_LENGTH, true);
                Header headerUnpacked = new HeaderParser(header).ParseHeader();
                //Console.WriteLine(headerUnpacked.GameCreator);


                // unpack the actions section
                int actionsSize = Common.ToInteger(ZlibUnpack(SECTION_SIZE_LENGTH));
                actions = ZlibUnpack(actionsSize);

                // unpack the map section
                int mapSize = Common.ToInteger(ZlibUnpack(SECTION_SIZE_LENGTH));
                map = ZlibUnpack(mapSize);


                result = new UnpackResult(identifier, header, actions, map);

            }
            finally
            {
                reader.Close();
            }

            return result;
        }

        /*
            Okay so this method is probably (who am I kidding) hacky
            BUT IT FUCKING WORKS
            it may have taken me way longer than it should to understand it, BUT IT WORKS!
            doesn't really throw exceptions like it probably should but whatevs.
        */
        //protected byte[] ZlibUnpack(int length)
        //{
        //    byte[] result = new byte[length];                    //result will be the final uncompressed bytes
        //    byte[] temp = new byte[length];                      //temp will hold the compressed data
        //    int checksum = _reader.ReadInt32();                  //read in the checksum we don't actually do anything with it
        //    int blocks = _reader.ReadInt32();                    //read in the amount of blocks
        //    for (int block = 0; block < blocks; block++)         //iterate for that amount of blocks
        //    {
        //        int encodedLength = _reader.ReadInt32();         //read the length of the encoded data for the current block
        //        int offset = block * 8192;                       //adjust the offset it's always a multiple of 8192
        //        _reader.Read(temp, 0, encodedLength);            //read the encoded data into temp

        //        if (encodedLength == temp.Length - offset)       //if the encoded data filled all the space allocated then we don't need decompression
        //        {
        //            Buffer.BlockCopy(temp, 0, result, offset, temp.Length);//copy the data to result
        //            continue;
        //        }

        //        temp = ZlibStream.UncompressBuffer(temp);        //decompress temp
        //        Buffer.BlockCopy(temp, 0, result, offset, temp.Length); //copy the data to result, minding the offset of course
        //    }

        //    return result;
        //}

        protected byte[] ZlibUnpack(int length, bool isHeader = false)
        {
            byte[] result = new byte[length];                    //result will be the final uncompressed bytes
            byte[] temp = new byte[length];                      //temp will hold the compressed data
            int checksum = _reader.ReadInt32();                  //read in the checksum we don't actually do anything with it
            if (isHeader)
            {
                uint magicHeader = 0x789cU;
                var offset = Common.FindOffset(_reader, magicHeader, 50);
                _reader.BaseStream.Position = offset - 8;
            }
            int blocks = _reader.ReadInt32();                    //read in the amount of blocks
            for (int block = 0; block < blocks; block++)         //iterate for that amount of blocks
            {
                int encodedLength = _reader.ReadInt32();         //read the length of the encoded data for the current block
                int offset = block * 8192;                       //adjust the offset it's always a multiple of 8192
                _reader.Read(temp, 0, encodedLength);            //read the encoded data into temp

                if (encodedLength == temp.Length - offset)       //if the encoded data filled all the space allocated then we don't need decompression
                {
                    Buffer.BlockCopy(temp, 0, result, offset, temp.Length);//copy the data to result
                    continue;
                }

                temp = ZlibStream.UncompressBuffer(temp);        //decompress temp
                Buffer.BlockCopy(temp, 0, result, offset, temp.Length); //copy the data to result, minding the offset of course
            }

            return result;
        }      

        /// <summary>
        /// Unpacks next section of the compressed replay
        /// </summary>
        /// <param name="length">The length of the section</param>
        /// <returns>The decoded data</returns>
        /*
            Now that I understand this section I have to say it's documented pretty poorly
        */
        protected byte[] UnpackNextSection(int length)
        {
            byte[] result = new byte[length];

            Decoder decoder = new Decoder();
            DecodeBuffer buffer = new DecodeBuffer(result);

            decoder.DecodeBuffer = buffer;


            int checksum = _reader.ReadInt32();
            int blocks = _reader.ReadInt32();
            //Console.WriteLine("amount of blocks: " + blocks);
            for (int block = 0; block < blocks; block++)
            {

                // read the length of the next block of encoded data
                int encodedLength = _reader.ReadInt32();
                //Console.WriteLine("Length of the next block: " + encodedLength);
                // we error if there is no space for the next block of encoded data
                if (encodedLength > result.Length - buffer.ResultOffset)
                {
                    throw new Exception("Insufficient space in decode buffer");
                }

                // read the block of encoded data into the result (decoded data will overwrite).
                _reader.Read(result, buffer.ResultOffset, encodedLength);


                // skip decoding if the encoded data filled the remaining space
                if (encodedLength == Math.Min(result.Length - buffer.ResultOffset, buffer.BufferLength))
                {
                    Console.WriteLine("Skipped decoding");
                    continue;
                }

                // set the decode buffer parameters
                buffer.EncodedOffset = 0;
                buffer.DecodedLength = 0;
                buffer.EncodedLength = encodedLength;

                // decode the block
                if (decoder.DecodeBlock() != 0)
                {
                    throw new Exception("Error decoding block offset " + block);
                }

                // sanity check the decoded length
                if (buffer.DecodedLength == 0 ||
                    buffer.DecodedLength > buffer.BufferLength ||
                    buffer.DecodedLength > result.Length)
                {
                    throw new Exception("Decode data length mismatch");
                }

                // flush the decoded bytes into the result
                buffer.WriteDecodedBytes();
            }

            return result;
        }
    }
}