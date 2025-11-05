using MonadDb.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Util
{
    public  class BinaryValueSerializer
    {
        public byte[] Serialize(object value, ColumnMetadata col)
        {
            if (value == null)
                return Empty(col);

            switch (col.Type)
            {
                case "int":
                    return SerializeInt(value, col.Size);

                case "long":
                    return SerializeLong(value, col.Size);

                case "bool":
                    return SerializeBool(value, col.Size);

                case "double":
                    return SerializeDouble(value, col.Size);

                case "string":
                    return SerializeFixedString(value.ToString(), col.Size);

                default:
                    throw new Exception($"Unsupported column type '{col.Type}'.");
            }
        }

        public  object Deserialize(byte[] data, ColumnMetadata col)
        {
            switch (col.Type)
            {
                case "int":
                    return BitConverter.ToInt32(data, 0);

                case "long":
                    return BitConverter.ToInt64(data, 0);

                case "bool":
                    return data[0] == 1;

                case "double":
                    return BitConverter.ToDouble(data, 0);

                case "string":
                    return Encoding.UTF8.GetString(data).TrimEnd('\0');

                default:
                    throw new Exception($"Unsupported column type '{col.Type}'.");
            }
        }

        private static byte[] Empty(ColumnMetadata col) =>
            new byte[col.Size];

        private static byte[] SerializeInt(object value, int size)
        {
            if (size != 4)
                throw new Exception($"Invalid size for INT: expected 4, got {size}");

            return BitConverter.GetBytes(Convert.ToInt32(value));
        }

        private static byte[] SerializeLong(object value, int size)
        {
            if (size != 8)
                throw new Exception($"Invalid size for LONG: expected 8, got {size}");

            return BitConverter.GetBytes(Convert.ToInt64(value));
        }

        private static byte[] SerializeBool(object value, int size)
        {
            if (size < 1)
                throw new Exception("Invalid size for BOOL");

            byte[] buffer = new byte[size];
            buffer[0] = (Convert.ToBoolean(value) ? (byte)1 : (byte)0);
            return buffer;
        }

        private static byte[] SerializeDouble(object value, int size)
        {
            if (size != 8)
                throw new Exception($"Invalid size for DOUBLE: expected 8, got {size}");

            return BitConverter.GetBytes(Convert.ToDouble(value));
        }

        private static byte[] SerializeFixedString(string value, int size)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);

            if (data.Length > size)
                throw new Exception($"String '{value}' exceeds fixed size {size}.");

            Array.Resize(ref data, size); // pad com zeros

            return data;
        }
    }
}

