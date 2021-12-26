using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydbApi.DataApi
{
    public static class TypeConverter
    {
        public static bool? BoolToNullable(bool value)
        {
            return value ? (bool?) true : null;
        }

        public static bool NullableToBool(bool? value)
        {
            return value.GetValueOrDefault();
        }
    }
}
