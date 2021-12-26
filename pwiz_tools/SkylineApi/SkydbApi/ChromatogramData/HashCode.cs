using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace SkydbApi.ChromatogramData
{
    public class HashValue
    {
        private byte[] _bytes;
        private HashValue(byte[] bytes)
        {
            _bytes = bytes;
        }

        public IEnumerable<byte> Bytes
        {
            get { return _bytes.AsEnumerable(); }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var hash = obj as HashValue;
            if (hash == null)
            {
                return false;
            }

            return _bytes.SequenceEqual(hash._bytes);
        }

        public override int GetHashCode()
        {
            int result = 0;
            foreach (var b in _bytes)
            {
                result = result * 397 + b;
            }

            return result;
        }

        public static HashValue HashBytes(byte[] bytes)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                return new HashValue(sha1.ComputeHash(bytes));
            }

        }
    }
}
