using System;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Databinding.Collections
{
    public class ResultKey : IComparable
    {
        public ResultKey(string replicateName, int replicateIndex, int fileIndex)
        {
            ReplicateName = replicateName;
            ReplicateIndex = replicateIndex;
            FileIndex = fileIndex;
        }

        public ResultKey(string replicateName, int replicateIndex, string multiplexName, int fileIndex) 
            : this(replicateName, replicateIndex, fileIndex)
        {
            MultiplexName = multiplexName;
        }

        public ResultKey(Replicate replicate, int fileIndex) : this(replicate.Name, replicate.ReplicateIndex, fileIndex)
        {
            MultiplexName = replicate.MultiplexName;
        }

        public int ReplicateIndex { get; private set; }
        public string ReplicateName { get; private set; }
        public string MultiplexName { get; private set; }
        public int FileIndex { get; private set; }
        public override string ToString()
        {
            var replicateName = ReplicateName;
            if (!string.IsNullOrEmpty(MultiplexName))
            {
                replicateName = TextUtil.SpaceSeparate(replicateName, MultiplexName);
            }
            if (0 == FileIndex)
            {
                return replicateName;
            }
            return string.Format(@"{0}[{1}]", replicateName, FileIndex);
        }

        public int CompareTo(object obj)
        {
            if (null == obj)
            {
                return 1;
            }
            var resultKey = (ResultKey) obj;
            int compareResult = ReplicateIndex.CompareTo(resultKey.ReplicateIndex);
            if (0 == compareResult)
            {
                compareResult = string.CompareOrdinal(MultiplexName, resultKey.MultiplexName);
            }
            if (0 == compareResult)
            {
                compareResult = FileIndex.CompareTo(resultKey.FileIndex);
            }
            return compareResult;
        }

        protected bool Equals(ResultKey other)
        {
            return ReplicateIndex == other.ReplicateIndex && FileIndex == other.FileIndex && MultiplexName == other.MultiplexName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ResultKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = ReplicateIndex;
                result = (result*397) ^ FileIndex;
                result = (result*397) ^ (MultiplexName == null ? 0 : MultiplexName.GetHashCode());
                return result;
            }
        }
    }
}
