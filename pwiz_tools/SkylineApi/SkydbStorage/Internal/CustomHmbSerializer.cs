using System.Collections;
using System.Reflection;
using NHibernate.Mapping.Attributes;

namespace SkydbStorage.Internal
{
    public class CustomHbmWriter : HbmWriterEx
    {
        public bool EnforceReferentialIntegrity { get; set; }
        public override ArrayList GetSortedAttributes(MemberInfo member)
        {
            var result = base.GetSortedAttributes(member);
            if (!EnforceReferentialIntegrity)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    var manyToOneAttribute = result[i] as ManyToOneAttribute;
                    if (manyToOneAttribute != null)
                    {
                        manyToOneAttribute.NotFound = NotFoundMode.Ignore;
                    }
                }
            }

            return result;
        }
    }
}
