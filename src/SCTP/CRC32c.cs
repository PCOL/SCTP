namespace SCTP
{
    /// <summary>
    /// Computes a CRC32 checksum.
    /// </summary>
    public class CRC32c
    {
        private readonly static uint[] crc32_table = new uint[256];

        private readonly static uint ulPolynomial = 0x04c11db7;

        static CRC32c()
        {
            InitCrcTable();
        }

        /// <summary>
        /// Initialises the CRC table.
        /// </summary>
        private static void InitCrcTable()
        {
            // 256 values representing ASCII character codes.
            for (uint i = 0; i <= 0xFF; i++)
            {
                crc32_table[i] = Reflect(i, 8) << 24;

                for (uint j = 0; j < 8; j++)
                {
                    long val = crc32_table[i] & (1 << 31);

                    if (val != 0)
                    {
                        val = ulPolynomial;
                    }
                    else
                    {
                        val = 0;
                    }

                    crc32_table[i] = (crc32_table[i] << 1) ^ (uint)val;
                }

                crc32_table[i] = Reflect(crc32_table[i], 32);
            }
        }

        private static uint Reflect(uint re, byte ch)
        {
            // Used only by Init_CRC32_Table()

            uint value = 0;

            // Swap bit 0 for bit 7
            // bit 1 for bit 6, etc.
            for (int i = 1; i < (ch + 1); i++)
            {
                long tmp = re & 1;
                int v = ch - i;

                if (tmp != 0)
                {
                    value |= (uint)1 << v; //(uint)(ch - i));
                }

                re >>= 1;
            }

            return value;
        }

        public static uint GetCRC(byte[] buffer, int offset, int count)
        {
            uint crc = 0xffffffff;

            // Perform the algorithm on each character
            // in the string, using the lookup table values.

            for (int i = offset; i < count; i++)
            {
                crc = (crc >> 8) ^ crc32_table[(crc & 0xFF) ^ buffer[i]];
            }

            // Exclusive OR the result with the beginning value.
            return crc ^ 0xffffffff;
        }
    }
}
