using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Skyline.Model.Sharing
{
    public class DataType
    {
        // ReSharper disable LocalizableElement
        public static readonly DataType BOOLEAN = new DataType("http://www.w3.org/2001/XMLSchema#boolean", "BOOLEAN");
        public static readonly DataType STRING = new DataType("http://www.w3.org/2001/XMLSchema#string", "VARCHAR");

        public static readonly DataType INTEGER = new DataType("http://www.w3.org/2001/XMLSchema#int", "INTEGER");
        public static readonly DataType DOUBLE = new DataType("http://www.w3.org/2001/XMLSchema#double", "DOUBLE");
        public static readonly DataType FLOAT = new DataType("http://www.w3.org/2001/XMLSchema#float", "REAL");

        // ReSharper restore LocalizableElement

        public DataType(string rangeUri, string jdbcName)
        {
            RangeUri = rangeUri;
            JdbcName = jdbcName;
        }

        public string RangeUri { get; private set; }
        public string JdbcName { get; private set; }

        public static DataType GetDataType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }

            if (type == typeof(bool))
            {
                return BOOLEAN;
            }

            if (type == typeof(int))
            {
                return INTEGER;
            }

            if (type == typeof(double))
            {
                return DOUBLE;
            }

            if (type == typeof(float))
            {
                return FLOAT;
            }

            return STRING;
        }
    }
}
