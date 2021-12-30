using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace SkydbStorage.DataAccess
{
    public class Sha1HashValue
    {
        private byte[] _bytes;
        private Sha1HashValue(byte[] bytes)
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

            var hash = obj as Sha1HashValue;
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

        public static Sha1HashValue HashBytes(byte[] bytes)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                return new Sha1HashValue(sha1.ComputeHash(bytes));
            }

        }
    }
}
