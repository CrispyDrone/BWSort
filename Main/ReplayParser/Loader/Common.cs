using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace ReplayParser.Loader
{

    public class Common
    {
        private Common()
        {

        }

        public static short ReverseBytes(short value)
        {
            return IPAddress.NetworkToHostOrder(value);
        }
        public static int ReverseBytes(int value)
        {
            return IPAddress.NetworkToHostOrder(value);
        }
        public static long ReverseBytes(long value)
        {
            return IPAddress.NetworkToHostOrder(value);
        }

        public static int ToInteger(byte[] bytes)
        {
            int value = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                value <<= 8;
                value = value | (bytes[i] & 0xFF);
            }

            return ReverseBytes(value);
        }

        // I added this, can this be written better? Most likely...
        public static byte[] IntToByteArray(uint number)
        {
            uint originalnumber = number;
            int counter = 1;
            while ((number >>= 8) > 0)
            {
                counter++;
            }
            byte[] numberAsByteArray = new byte[counter];
            byte selector = byte.MaxValue;
            for (int i = 0; i < counter; i++)
            {
                var byteSelector = selector << (8 * i);
                numberAsByteArray[i] = (byte)((originalnumber & byteSelector) >> (8 * i));
            }
            return numberAsByteArray;
        }

        // I added this, can this be written better? Most likely...
        public static int[] FindAllIndexOfByte(byte[] byteArray, byte abyte, int start)
        {
            return byteArray.Select((b, i) => b == abyte ? i : -1).Where(i => i != -1 && i >= start).ToArray();

        }

        // I added this, can this be written better? most likely....
        // It finds the offset in a binary file of a particular number
        public static int FindOffset(BinaryReader _reader, uint number, int distance)
        {
            var numberAsByteArray = IntToByteArray(number).Reverse().ToArray();
            var replayStream = _reader.BaseStream;
            replayStream.Position = 0;
            int offset = 0;

            byte[] data = new byte[distance];
            _reader.Read(data, 0, distance);
            int[] indexes_firstbyte = FindAllIndexOfByte(data, numberAsByteArray[0], 0);

            foreach (var index in indexes_firstbyte)
            {
                for (int i = 1; i < numberAsByteArray.Length; i++)
                {
                    if (data[index + i] != numberAsByteArray[i])
                        break;
                    offset = index;
                }
            }
            if (offset == 0)
            {
                throw new Exception("Unsupported compression algorithm.");
            }
            return offset;
        }


        public static short ToUnsignedByte(byte value)
        {
            return (short)(value & 0xff);
        }

        public static short ToUnsignedByte(int value)
        {
            return (short)(((byte)value) & 0xff);
        }
    }
}
