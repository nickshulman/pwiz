using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zlib;

namespace SkydbApi.ChromatogramData
{
    public static class DataUtil
    {
        public static byte[] GetHashCode(byte[] bytes)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                return sha1.ComputeHash(bytes);
            }
        }

        public static byte[] Compress(byte[] bytes)
        {
            using (var ms = new MemoryStream())
            {
                using (var compressor =
                    new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.Default))
                {
                    compressor.Write(bytes, 0, bytes.Length);
                }
                return ms.ToArray();
            }
        }

        public static T[] PrimitivesFromByteArray<T>(byte[] bytes)
        {
            if (null == bytes)
            {
                return null;
            }
            T[] result = new T[bytes.Length / Buffer.ByteLength(new T[1])];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return result;
        }

        public static byte[] PrimitivesToByteArray<T>(T[] array)
        {
            byte[] result = new byte[Buffer.ByteLength(array)];
            Buffer.BlockCopy(array, 0, result, 0, result.Length);
            return result;
        }
    }
}
