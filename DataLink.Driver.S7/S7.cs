using System;

namespace DataLink.Driver.S7
{
    public static class S7
    {
        // S7 ID Area (Area that we want to read/write)
        public const int S7AreaPE = 0x81;
        public const int S7AreaPA = 0x82;
        public const int S7AreaMK = 0x83;
        public const int S7AreaDB = 0x84;
        public const int S7AreaCT = 0x1C;
        public const int S7AreaTM = 0x1D;
        // Connection types
        public const byte PG = 0x01;
        public const byte OP = 0x02;
        public const byte S7_BASIC = 0x03;
        // Block type
        public const int Block_OB = 0x38;
        public const int Block_DB = 0x41;
        public const int Block_SDB = 0x42;
        public const int Block_FC = 0x43;
        public const int Block_SFC = 0x44;
        public const int Block_FB = 0x45;
        public const int Block_SFB = 0x46;
        // Sub Block Type
        public const int SubBlk_OB = 0x08;
        public const int SubBlk_DB = 0x0A;
        public const int SubBlk_SDB = 0x0B;
        public const int SubBlk_FC = 0x0C;
        public const int SubBlk_SFC = 0x0D;
        public const int SubBlk_FB = 0x0E;
        public const int SubBlk_SFB = 0x0F;
        // Block languages
        public const int BlockLangAWL = 0x01;
        public const int BlockLangKOP = 0x02;
        public const int BlockLangFUP = 0x03;
        public const int BlockLangSCL = 0x04;
        public const int BlockLangDB = 0x05;
        public const int BlockLangGRAPH = 0x06;
        // PLC Status
        public const int S7CpuStatusUnknown = 0x00;
        public const int S7CpuStatusRun = 0x08;
        public const int S7CpuStatusStop = 0x04;
        // Type Var
        public const int S7TypeBool = 1;
        public const int S7TypeInt = 1;

        /// <summary>
        /// Returns the bit at Pos.Bit
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Pos"></param>
        /// <param name="Bit"></param>
        /// <returns></returns>
        public static bool GetBitAt(byte[] Buffer, int Pos, int Bit)
        {
            int Value = Buffer[Pos] & 0x0FF;
            byte[] Mask = {
            (byte)0x01,(byte)0x02,(byte)0x04,(byte)0x08,
            (byte)0x10,(byte)0x10,(byte)0x40,(byte)0x80
        };
            if (Bit < 0) Bit = 0;
            if (Bit > 7) Bit = 7;

            return (Value & Mask[Bit]) != 0;
        }
        /// <summary>
        ///  Returns a 16 bit unsigned value : from 0 to 65535 (2^16-1)
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Pos"></param>
        /// <returns></returns>
        public static int GetWordAt(byte[] Buffer, int Pos)
        {
            int hi = (Buffer[Pos] & 0x00FF);
            int lo = (Buffer[Pos + 1] & 0x00FF);
            return (hi << 8) + lo;
        }

        /// <summary>
        /// Returns a 16 bit signed value : from -32768 to 32767
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Pos"></param>
        /// <returns></returns>
        public static int GetShortAt(byte[] Buffer, int Pos)
        {
            int hi = (Buffer[Pos]);
            int lo = (Buffer[Pos + 1] & 0x00FF);
            return ((hi << 8) + lo);
        }

        /// <summary>
        /// Returns a 32 bit unsigned value : from 0 to 4294967295 (2^32-1)
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Pos"></param>
        /// <returns></returns>
        public static long GetDWordAt(byte[] Buffer, int Pos)
        {
            long Result;
            Result = (long)(Buffer[Pos] & 0x0FF);
            Result <<= 8;
            Result += (long)(Buffer[Pos + 1] & 0x0FF);
            Result <<= 8;
            Result += (long)(Buffer[Pos + 2] & 0x0FF);
            Result <<= 8;
            Result += (long)(Buffer[Pos + 3] & 0x0FF);
            return Result;
        }

        /// <summary>
        /// Returns a 32 bit signed value : from 0 to 4294967295 (2^32-1)
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Pos"></param>
        /// <returns></returns>
        public static int GetDIntAt(byte[] Buffer, int Pos)
        {
            int Result;
            Result = Buffer[Pos];
            Result <<= 8;
            Result += (Buffer[Pos + 1] & 0x0FF);
            Result <<= 8;
            Result += (Buffer[Pos + 2] & 0x0FF);
            Result <<= 8;
            Result += (Buffer[Pos + 3] & 0x0FF);
            return Result;
        }
        /// <summary>
        /// Returns a 32 bit floating point
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Pos"></param>
        /// <returns></returns>
        public static float GetFloatAt(byte[] Buffer, int Pos)
        {
            int IntFloat = GetDIntAt(Buffer, Pos);
            byte[] bytes = BitConverter.GetBytes(IntFloat);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Returns an ASCII string
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Pos"></param>
        /// <param name="MaxLen"></param>
        /// <returns></returns>
        public static String GetStringAt(byte[] Buffer, int Pos, int MaxLen)
        {
            byte[] StrBuffer = new byte[MaxLen];
            Array.Copy(Buffer, Pos, StrBuffer, 0, MaxLen);
            string S;
            try
            {
                S = StrBuffer.GetString(); // the charset is UTF-8
            }
            catch (Exception ex)//should be UnsupportedEncodingException
            {
                S = "";
            }
            return S;
        }
        /// <summary>
        /// Returns an printable string
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Pos"></param>
        /// <param name="MaxLen"></param>
        /// <returns></returns>
        public static String GetPrintableStringAt(byte[] Buffer, int Pos, int MaxLen)
        {
            byte[] StrBuffer = new byte[MaxLen];
            Array.Copy(Buffer, Pos, StrBuffer, 0, MaxLen);
            for (int c = 0; c < MaxLen; c++)
            {
                if ((StrBuffer[c] < 31) || (StrBuffer[c] > 126))
                    StrBuffer[c] = 46; // '.'
            }
            String S;
            try
            {
                S = StrBuffer.GetString(); // the charset is UTF-8
            }
            catch (Exception ex)//should be UnsupportedEncodingException
            {
                S = "";
            }
            return S;
        }
        /// <summary>
        /// Returns a date
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Pos"></param>
        /// <returns></returns>
        public static DateTime GetDateAt(byte[] Buffer, int Pos)
        {
            int Year = S7.BCDtoByte(Buffer[Pos + 1]);
            if (Year < 90)
                Year += 2000;
            else
                Year += 1900;

            int Month = S7.BCDtoByte(Buffer[Pos + 2]);
            int Day = S7.BCDtoByte(Buffer[Pos + 3]);
            int Hour = S7.BCDtoByte(Buffer[Pos + 4]);
            int Min = S7.BCDtoByte(Buffer[Pos + 5]);
            int Sec = S7.BCDtoByte(Buffer[Pos + 6]);
            //lth: should be get by this position, it is bit moved now.
            //Year = S7.BCDtoByte(Buffer[Pos]);
            //if (Year < 90)
            //    Year += 2000;
            //else
            //    Year += 1900;

            //Month = S7.BCDtoByte(Buffer[Pos + 1]) - 1;
            //Day = S7.BCDtoByte(Buffer[Pos + 2]);
            //Hour = S7.BCDtoByte(Buffer[Pos + 3]);
            //Min = S7.BCDtoByte(Buffer[Pos + 4]);
            //Sec = S7.BCDtoByte(Buffer[Pos + 5]);
            var S7Date = new DateTime(Year, Month, Day, Hour, Min, Sec);
            return S7Date;
        }

        public static void SetBitAt(byte[] Buffer, int Pos, int Bit, bool Value)
        {
            byte[] Mask = {
            (byte)0x01,(byte)0x02,(byte)0x04,(byte)0x08,
            (byte)0x10,(byte)0x20,(byte)0x40,(byte)0x80
        };
            if (Bit < 0) Bit = 0;
            if (Bit > 7) Bit = 7;

            if (Value)
                Buffer[Pos] = (byte)(Buffer[Pos] | Mask[Bit]);
            else
                Buffer[Pos] = (byte)(Buffer[Pos] & ~Mask[Bit]);
        }

        public static void SetWordAt(byte[] Buffer, int Pos, int Value)
        {
            int Word = Value & 0x0FFFF;
            Buffer[Pos] = (byte)(Word >> 8);
            Buffer[Pos + 1] = (byte)(Word & 0x00FF);
        }

        public static void SetShortAt(byte[] Buffer, int Pos, int Value)
        {
            Buffer[Pos] = (byte)(Value >> 8);
            Buffer[Pos + 1] = (byte)(Value & 0x00FF);
        }
        public static void SetDWordAt(byte[] Buffer, int Pos, long Value)
        {
            long DWord = Value & 0x0FFFFFFFF;
            Buffer[Pos + 3] = (byte)(DWord & 0xFF);
            Buffer[Pos + 2] = (byte)((DWord >> 8) & 0xFF);
            Buffer[Pos + 1] = (byte)((DWord >> 16) & 0xFF);
            Buffer[Pos] = (byte)((DWord >> 24) & 0xFF);
        }

        public static void SetDIntAt(byte[] Buffer, int Pos, int Value)
        {
            Buffer[Pos + 3] = (byte)(Value & 0xFF);
            Buffer[Pos + 2] = (byte)((Value >> 8) & 0xFF);
            Buffer[Pos + 1] = (byte)((Value >> 16) & 0xFF);
            Buffer[Pos] = (byte)((Value >> 24) & 0xFF);
        }

        public static void SetFloatAt(byte[] Buffer, int Pos, float Value)
        {
            Array.Reverse(Buffer);
            int DInt = BitConverter.ToInt32(Buffer, 0);
            SetDIntAt(Buffer, Pos, DInt);
        }

        public static void SetDateAt(byte[] Buffer, int Pos, DateTime DateTime)
        {
            int Year, Month, Day, Hour, Min, Sec, Dow;


            Year = DateTime.Year;
            Month = DateTime.Month;// + 1;
            Day = DateTime.Day;
            Hour = DateTime.Hour;
            Min = DateTime.Minute;
            Sec = DateTime.Second;
            Dow = (int)DateTime.DayOfWeek;

            if (Year > 1999)
                Year -= 2000;

            Buffer[Pos] = ByteToBCD(Year);
            Buffer[Pos + 1] = ByteToBCD(Month);
            Buffer[Pos + 2] = ByteToBCD(Day);
            Buffer[Pos + 3] = ByteToBCD(Hour);
            Buffer[Pos + 4] = ByteToBCD(Min);
            Buffer[Pos + 5] = ByteToBCD(Sec);
            Buffer[Pos + 6] = 0;
            Buffer[Pos + 7] = ByteToBCD(Dow);
        }

        public static int BCDtoByte(byte B)
        {
            return ((B >> 4) * 10) + (B & 0x0F);
        }

        public static byte ByteToBCD(int Value)
        {
            return (byte)(((Value / 10) << 4) | (Value % 10));
        }

    }
}
