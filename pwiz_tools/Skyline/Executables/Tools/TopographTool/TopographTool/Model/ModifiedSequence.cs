using System.Collections.Generic;
using System.Linq;
using System.Text;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace TopographTool.Model
{
    public class ModifiedSequence : Immutable
    {
        public ModifiedSequence(string unmodifiedSequence, IEnumerable<KeyValuePair<int, string>> modifications)
        {
            UnmodifiedSequence = unmodifiedSequence;
            Modifications = ImmutableList.ValueOfOrEmpty(modifications);
        }

        public string UnmodifiedSequence { get; private set; }
        public ImmutableList<KeyValuePair<int, string>> Modifications { get; private set; }

        public static ModifiedSequence Parse(string modifiedSequence)
        {
            StringBuilder unmodifiedSequence = new StringBuilder();
            int? modificationStart = null;
            var modifications = new List<KeyValuePair<int, string>>();
            for (int i = 0; i < modifiedSequence.Length; i++)
            {
                char ch = modifiedSequence[i];
                if (modificationStart.HasValue)
                {
                    if (ch == ']')
                    {
                        string strModification =
                            modifiedSequence.Substring(modificationStart.Value, i - modificationStart.Value);
                        modifications.Add(new KeyValuePair<int, string>(
                            unmodifiedSequence.Length - 1, strModification));
                        modificationStart = null;
                    }
                }
                else
                {
                    if (ch == '[')
                    {
                        modificationStart = i + 1;
                    }
                    else
                    {
                        unmodifiedSequence.Append(ch);
                    }
                }
            }
            return new ModifiedSequence(unmodifiedSequence.ToString(), ImmutableList.ValueOf(modifications));
        }

        protected bool Equals(ModifiedSequence other)
        {
            return string.Equals(UnmodifiedSequence, other.UnmodifiedSequence) && Modifications.Equals(other.Modifications);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ModifiedSequence) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (UnmodifiedSequence.GetHashCode() * 397) ^ Modifications.GetHashCode();
            }
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            var modificationsByIndex = Modifications.ToLookup(mod => mod.Key);
            for (int i = 0; i < UnmodifiedSequence.Length; i++)
            {
                result.Append(UnmodifiedSequence[i]);
                foreach (var mod in modificationsByIndex[i])
                {
                    result.Append("[" + mod.Value + "]");
                }
            }
            return result.ToString();
        }
    }
}
