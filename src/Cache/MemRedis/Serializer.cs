
using CYQ.Data.Tool;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace CYQ.Data.Cache
{
    internal enum SerializedType : ushort
    {
        None = 255,
        ByteArray = 0,
        Object = 1,
        String = 2,
        Datetime = 3,
        Bool = 4,
        //SByte		= 5, //Makes no sense.
        Byte = 6,
        Short = 7,
        UShort = 8,
        Int = 9,
        UInt = 10,
        Long = 11,
        ULong = 12,
        Float = 13,
        Double = 14,
        ObjectJson = 15,


        CompressedByteArray = 50,
        CompressedObject = 51,
        CompressedString = 52,
        CompressedObjectJson = 53,
    }

    internal class Serializer
    {
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="type">返回序列化类型</param>
        /// <param name="compressionThreshold">指定超过长度时启用压缩功能</param>
        /// <returns></returns>
        public static byte[] Serialize(object value, out SerializedType type, uint compressionThreshold)
        {
            byte[] bytes;
            if (value is byte[])
            {
                bytes = (byte[])value;
                type = SerializedType.ByteArray;
                if (bytes.Length > compressionThreshold)
                {
                    bytes = compress(bytes);
                    type = SerializedType.CompressedByteArray;
                }
            }
            else if (value is string)
            {
                bytes = Encoding.UTF8.GetBytes((string)value);
                type = SerializedType.String;
                if (bytes.Length > compressionThreshold)
                {
                    bytes = compress(bytes);
                    type = SerializedType.CompressedString;
                }
            }
            else if (value is DateTime)
            {
                bytes = BitConverter.GetBytes(((DateTime)value).Ticks);
                type = SerializedType.Datetime;
            }
            else if (value is bool)
            {
                bytes = new byte[] { (byte)((bool)value ? 1 : 0) };
                type = SerializedType.Bool;
            }
            else if (value is byte)
            {
                bytes = new byte[] { (byte)value };
                type = SerializedType.Byte;
            }
            else if (value is short)
            {
                bytes = BitConverter.GetBytes((short)value);
                type = SerializedType.Short;
            }
            else if (value is ushort)
            {
                bytes = BitConverter.GetBytes((ushort)value);
                type = SerializedType.UShort;
            }
            else if (value is int)
            {
                bytes = BitConverter.GetBytes((int)value);
                type = SerializedType.Int;
            }
            else if (value is uint)
            {
                bytes = BitConverter.GetBytes((uint)value);
                type = SerializedType.UInt;
            }
            else if (value is long)
            {
                bytes = BitConverter.GetBytes((long)value);
                type = SerializedType.Long;
            }
            else if (value is ulong)
            {
                bytes = BitConverter.GetBytes((ulong)value);
                type = SerializedType.ULong;
            }
            else if (value is float)
            {
                bytes = BitConverter.GetBytes((float)value);
                type = SerializedType.Float;
            }
            else if (value is double)
            {
                bytes = BitConverter.GetBytes((double)value);
                type = SerializedType.Double;
            }
            else
            {
                type = SerializedType.ObjectJson;
                Type t = value.GetType();
                switch (ReflectTool.GetSystemType(ref t))
                {
                    case SysType.Base:
                    case SysType.Enum:
                    case SysType.Custom:
                        if (t.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0)
                        {
                            type = SerializedType.Object;
                        }
                        break;
                }
                if (type == SerializedType.Object)
                {
                    //可序列化 List<object> 如果 object 不支持序列化，会挂。
                    using (MemoryStream ms = new MemoryStream())
                    {
                        new BinaryFormatter().Serialize(ms, value);
                        bytes = ms.ToArray();
                        if (bytes.Length > compressionThreshold)
                        {
                            bytes = compress(bytes);
                            type = SerializedType.CompressedObject;
                        }
                    }
                }
                else
                {
                    string json = JsonHelper.ToJson(value);
                    bytes = Encoding.UTF8.GetBytes(json);
                    if (bytes.Length > compressionThreshold)
                    {
                        bytes = compress(bytes);
                        type = SerializedType.CompressedObjectJson;
                    }
                }
            }
            return bytes;
        }

        private static byte[] compress(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (DeflateStream gzs = new DeflateStream(ms, CompressionMode.Compress, false))
                {
                    gzs.Write(bytes, 0, bytes.Length);
                }
                ms.Close();
                return ms.ToArray();
            }
        }

        private static byte[] decompress(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes, false))
            {
                using (DeflateStream gzs = new DeflateStream(ms, CompressionMode.Decompress, false))
                {
                    using (MemoryStream dest = new MemoryStream())
                    {
                        byte[] tmp = new byte[bytes.Length];
                        int read;
                        while ((read = gzs.Read(tmp, 0, tmp.Length)) != 0)
                        {
                            dest.Write(tmp, 0, read);
                        }
                        dest.Close();
                        return dest.ToArray();
                    }
                }
            }
        }

        public static object DeSerialize(byte[] bytes, SerializedType type)
        {
            switch (type)
            {
                case SerializedType.String:
                    return Encoding.UTF8.GetString(bytes);
                case SerializedType.Datetime:
                    return new DateTime(BitConverter.ToInt64(bytes, 0));
                case SerializedType.Bool:
                    return bytes[0] == 1;
                case SerializedType.Byte:
                    return bytes[0];
                case SerializedType.Short:
                    return BitConverter.ToInt16(bytes, 0);
                case SerializedType.UShort:
                    return BitConverter.ToUInt16(bytes, 0);
                case SerializedType.Int:
                    return BitConverter.ToInt32(bytes, 0);
                case SerializedType.UInt:
                    return BitConverter.ToUInt32(bytes, 0);
                case SerializedType.Long:
                    return BitConverter.ToInt64(bytes, 0);
                case SerializedType.ULong:
                    return BitConverter.ToUInt64(bytes, 0);
                case SerializedType.Float:
                    return BitConverter.ToSingle(bytes, 0);
                case SerializedType.Double:
                    return BitConverter.ToDouble(bytes, 0);
                case SerializedType.Object:
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        return new BinaryFormatter().Deserialize(ms);
                    }
                case SerializedType.ObjectJson:
                    return Encoding.UTF8.GetString(bytes);
                case SerializedType.CompressedByteArray:
                    return DeSerialize(decompress(bytes), SerializedType.ByteArray);
                case SerializedType.CompressedString:
                    return DeSerialize(decompress(bytes), SerializedType.String);
                case SerializedType.CompressedObject:
                    return DeSerialize(decompress(bytes), SerializedType.Object);
                case SerializedType.CompressedObjectJson:
                    return DeSerialize(decompress(bytes), SerializedType.ObjectJson);
                case SerializedType.ByteArray:
                default:
                    return bytes;
            }
        }
    }
}