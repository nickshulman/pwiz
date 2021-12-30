using System;
using System.Reflection;
using SkydbStorage.Internal.Orm;

namespace SkydbStorage.DataAccess
{
    public abstract class ColumnInfo
    {
        protected ColumnInfo(string name, Type valueType, Type foreignEntityType)
        {
            Name = name;
            ValueType = valueType;
            ForeignEntityType = foreignEntityType;
        }

        public string Name { get; }
        public Type ValueType { get; }
        public virtual Type ForeignEntityType
        {
            get;
        }

        public abstract object GetValue(Entity entity);
        public abstract void SetValue(Entity entity, object value);

        public class Property : ColumnInfo
        {
            public Property(PropertyInfo propertyInfo, Type foreignTableType) : base(propertyInfo.Name, propertyInfo.PropertyType, foreignTableType)
            {
                PropertyInfo = propertyInfo;
            }

            public PropertyInfo PropertyInfo { get; }

            public override object GetValue(Entity entity)
            {
                return PropertyInfo.GetValue(entity);
            }

            public override void SetValue(Entity entity, object value)
            {
                PropertyInfo.SetValue(entity, value);
            }
        }

        public class Score : ColumnInfo
        {
            public Score(string scoreName) : base(scoreName, typeof(double?), null)
            {

            }

            public override object GetValue(Entity entity)
            {
                return ((Scores) entity).GetScore(Name);
            }

            public override void SetValue(Entity entity, object value)
            {
                if (value == null)
                {
                    ((Scores) entity).RemoveScore(Name);
                }
                else
                {
                    ((Scores) entity).SetScore(Name, (double) value);
                }
            }
        }
    }
}
