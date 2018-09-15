using System;

namespace BLEConnecter
{
    public static class Common
    {
        // 短縮UUIDから完全UUIDを作成する
        public static Guid CreateFullUUID(string sUUID)
        {
            string fullUUID = $"0000{sUUID}-0000-1000-8000-00805f9b34fb";
            return (new Guid(fullUUID));
        }

        // byte配列値をfloatに変換する
        public enum ConvType
        {
            IEEE_1073_16bit_float,
            IEEE_1073_32bit_float,
        }

        public static float ConvertToFloat(byte[] value, ConvType type)
        {
            switch (type) {
                case ConvType.IEEE_1073_16bit_float:
                    return (Common.tofloat_from11073_16bit_float(value));
                case ConvType.IEEE_1073_32bit_float:
                    return (Common.tofloat_from11073_32bit_float(value));
            }
            return 0.0f;
        }

        // IEEE - 11073 16-bit FLOAT
        private static float tofloat_from11073_16bit_float(byte[] value)
        {
            float ret = 0.0f;

            if (value.Length != 2)
                throw new ArgumentException();

            var uint16val = BitConverter.ToUInt16(value, 0);

            var mantissa = uint16val & 0x0FFF;

            int expoent = (Int16)uint16val >> 12;

            if (mantissa >= 0x07FE && mantissa <= 0x0802) {
                // Error-Untested
                throw new ArgumentException();
            } else {
                if (mantissa >= 0x0800) {
                    // Untested
                    var longval = -((0x0FFF + 1) - mantissa);
                    mantissa = (int)longval;
                }
                ret = (float)(mantissa * Math.Pow(10.0f, expoent));
            }

            return (ret);
        }

        // IEEE - 11073 32-bit FLOAT型
        private static float tofloat_from11073_32bit_float(byte[] value)
        {
            float ret = 0.0f;

            if (value.Length != 4)
                throw new ArgumentException();

            var uint32val = BitConverter.ToUInt32(value, 0);

            var mantissa = uint32val & 0xFFFFFF;

            int expoent = (Int32)uint32val >> 24;

            if (mantissa >= 0x007FFFFE && mantissa <= 0x00800002) {
                // Error-Untested
                throw new ArgumentException();
            } else {
                if (mantissa >= 0x800000) {
                    // Untested
                    var longval = -((0xFFFFFF + 1) - mantissa);
                    mantissa = (uint)longval;
                }
                ret = (float)(mantissa * Math.Pow(10.0f, expoent));
            }

            return (ret);
        }

    }
}
