using System;
using System.Collections.Generic;

namespace SkydbStorage.DataAccess
{
    public class EntityIdMap
    {
        private Dictionary<Type, Dictionary<long, long>> _maps 
            = new Dictionary<Type, Dictionary<long, long>>();

        public long? GetNewId(Type entityType, long oldId)
        {
            if (!_maps.TryGetValue(entityType, out Dictionary<long, long> dictionary))
            {
                return null;
            }

            if (dictionary.TryGetValue(oldId, out long newId))
            {
                return newId;
            }

            return null;
        }

        public void SetNewId(Type entityType, long oldId, long newId)
        {
            if (!_maps.TryGetValue(entityType, out Dictionary<long, long> dictionary))
            {
                dictionary = new Dictionary<long, long>();
                _maps.Add(entityType, dictionary);
            }
            dictionary.Add(oldId, newId);
        }
    }
}
