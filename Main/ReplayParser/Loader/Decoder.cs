using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.Loader
{
    public class Decoder
    {

        private int m04;
        private int m08;
        private int m0C;
        private int m10;
        private int m14;
        private int m18;
        private int m1C;
        private int m20;

        public DecodeBuffer DecodeBuffer { get; set; }

        private byte[] m0030 = new byte[0x2204];
        private byte[] m2234 = new byte[0x0800];
        private byte[] m2A34 = new byte[0x0100];
        private byte[] m2B34 = new byte[0x0100];
        private byte[] m2C34 = new byte[0x0100];
        private byte[] m2D34 = new byte[0x0180];
        private byte[] m2EB4 = new byte[0x0100];
        private byte[] m2FB4 = new byte[0x0100];
        private byte[] m30B4 = new byte[0x0040];
        private byte[] m30F4 = new byte[0x0010];
        private byte[] m3104 = new byte[0x0010];
        private byte[] m3114 = new byte[0x0020];

        public int DecodeBlock()
        {
            m1C = 0x800;
            m20 = DecodeBuffer.GetEncodedBytes(m2234, m1C);

            if (m20 <= 4)
                return 3;

            m04 = Common.ToUnsignedByte(DecodeBuffer.Result[DecodeBuffer.ResultOffset + 0]);
            m0C = Common.ToUnsignedByte(DecodeBuffer.Result[DecodeBuffer.ResultOffset + 1]);
            m14 = Common.ToUnsignedByte(DecodeBuffer.Result[DecodeBuffer.ResultOffset + 2]);
            m18 = 0;
            m1C = 3;

            if (m0C < 4 || m0C > 6)
            {
                return 1;
            }

            m10 = (1 << m0C) - 1;

            if (m04 != 0)
            {
                return 2;
            }


            Array.Copy(ByteConstants.OFF_5071D0, 0, m30F4, 0, ByteConstants.OFF_5071D0.Length);
            Com1(ByteConstants.OFF_5071E0.Length, m30F4, ByteConstants.OFF_5071E0, m2B34);

            Array.Copy(ByteConstants.OFF_5071A0, 0, m3104, 0, ByteConstants.OFF_5071A0.Length);
            Array.Copy(ByteConstants.OFF_5071B0, 0, m3114, 0, ByteConstants.OFF_5071B0.Length);
            Array.Copy(ByteConstants.OFF_507120, 0, m30B4, 0, ByteConstants.OFF_507120.Length);

            Com1(ByteConstants.OFF_507160.Length, m30B4, ByteConstants.OFF_507160, m2A34);
            UnpackChunk();

            return 0;
        }

        private static void Com1(int strlen, byte[] src, byte[] str, byte[] dst)
        {

            int n, x, y;

            for (n = strlen - 1; n >= 0; n--)
            {
                for (x = Common.ToUnsignedByte(str[n]), y = 1 << Common.ToUnsignedByte(src[n]); x < 0x100; x += y)
                {
                    dst[x] = (byte)n;
                }
            }
        }

        private int UnpackChunk()
        {

            int tmp, len;

            m08 = 0x1000;
            do
            {
                len = Function1();
                if (len >= 0x305)
                {
                    break;
                }

                if (len >= 0x100)
                {
                    /* decode region of length len -0xFE */
                    len -= 0xFE;
                    tmp = Function2(len);

                    if (tmp == 0)
                    {
                        len = 0x306;
                        break;
                    }

                    for (; len > 0; m08++, len--)
                    {
                        m0030[m08] = m0030[m08 - tmp];
                    }

                }
                else
                {

                    /* just copy the character */
                    m0030[m08] = (byte)len;
                    m08++;
                }

                if (m08 < 0x2000)
                {
                    continue;
                }

                DecodeBuffer.PutDecodedBytes(m0030, 0x1000, 0x1000);

                for (int i = 0; i < (m08 - 0x1000); i++)
                {
                    m0030[i] = m0030[i + 0x1000];
                }

                m08 -= 0x1000;

            } while (true);

            DecodeBuffer.PutDecodedBytes(m0030, 0x1000, m08 - 0x1000);

            return len;
        }

        private int Something(int count)
        {

            int tmp;

            if (m18 < count)
            {
                m14 >>= Common.ToUnsignedByte(m18);

                if (m1C == m20)
                {
                    m20 = DecodeBuffer.GetEncodedBytes(m2234, 0x0800);
                    if (m20 == 0)
                    {
                        return 1;
                    }
                    else
                    {
                        m1C = 0;
                    }
                }

                tmp = Common.ToUnsignedByte(m2234[m1C]);
                tmp <<= 8;
                m1C++;
                tmp |= m14;
                m14 = tmp;
                tmp >>= (count - Common.ToUnsignedByte(m18));
                m14 = tmp;
                m18 += (8 - count);
            }
            else
            {
                m18 -= count;
                m14 >>= Common.ToUnsignedByte(count);
            }

            return 0;
        }

        private int Function1()
        {

            int x, result;

            /* myesi->m14 is odd */
            if ((1 & m14) != 0)
            {
                if (Something(1) != 0)
                {
                    return 0x306;
                }

                result = m2B34[Common.ToUnsignedByte(m14)];

                if (Something(m30F4[result]) != 0)
                {
                    return 0x306;
                }

                if (m3104[result] != 0)
                {
                    x = ((1 << m3104[result]) - 1) & m14;

                    if (Something(m3104[result]) != 0 && (result + x) != 0x10E)
                    {
                        return 0x306;
                    }

                    byte[] bytes = new byte[4];
                    bytes[0] = m3114[2 * result];
                    bytes[1] = m3114[2 * result + 1];
                    bytes[2] = (byte)((result >> 16) & 0xFF);
                    bytes[3] = (byte)((result >> 24) & 0xFF);

                    result = Common.ToInteger(bytes);
                    result += x;
                }

                return result + 0x100;
            }

            /* myesi->m14 is even */
            if (Something(1) != 0)
            {
                return 0x306;
            }

            if (m04 == 0)
            {
                result = Common.ToUnsignedByte(m14);

                if (Something(8) != 0)
                {
                    return 0x306;
                }

                return result;
            }

            if ((byte)m14 == 0)
            {

                if (Something(8) != 0)
                {
                    return 0x306;
                }

                result = m2EB4[Common.ToUnsignedByte(m14)];

            }
            else
            {

                result = m2C34[Common.ToUnsignedByte(m14)];

                if (result == 0xFF)
                {
                    if ((m14 & 0x3F) == 0)
                    {
                        if (Something(6) != 0)
                        {
                            return 0x306;
                        }
                        result = m2C34[m14 & 0x7F];
                    }
                    else
                    {
                        if (Something(4) != 0)
                        {
                            return 0x306;
                        }
                        result = m2D34[m14 & 0xFF];
                    }
                }
            }

            if (Something(m2FB4[result]) != 0)
            {
                return 0x306;
            }

            return result;
        }
        
        private int Function2(int length)
        {

            int tmp;

            tmp = m2A34[Common.ToUnsignedByte(m14)];
            if (Something(m30B4[tmp]) != 0)
            {
                return 0;
            }

            if (length != 2)
            {
                tmp <<= Common.ToUnsignedByte(m0C);
                tmp |= m14 & m10;
                if (Something(m0C) != 0)
                {
                    return 0;
                }
            }
            else
            {
                tmp <<= 2;
                tmp |= m14 & 3;
                if (Something(2) != 0)
                {
                    return 0;
                }
            }

            return tmp + 1;
        }
    }
}
