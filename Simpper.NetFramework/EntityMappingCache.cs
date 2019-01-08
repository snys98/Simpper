using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Simpper.NetFramework
{
    public class EntityMappingCache<TEntity> : EntityMappingCache
    {
        

        public EntityMappingCache()
        {
            var tableAttr = typeof(TEntity).GetCustomAttribute<TableAttribute>();
            base._sharding = tableAttr.Sharding;
            base._rawTableName = tableAttr.Name;
            base.IdPropertyInfos = typeof(TEntity).GetIdProperties();
            base.MappedProperties = typeof(TEntity).GetMappedProperties();
            base.MutableProperties = typeof(TEntity).GetMutableProperties();
            base.PropertyColumnMappings = this.MappedProperties.ToDictionary(x => x, x =>
            {
                var attr = x.GetCustomAttribute<ColumnAttribute>();
                var columnName = attr == null ? x.Name : attr.Name;
                return columnName;
            });
        }

    }

    public class EntityMappingCache
    {
        public List<PropertyInfo> IdPropertyInfos { get; protected set; }
        public List<PropertyInfo> MappedProperties { get; protected set; }
        public List<PropertyInfo> MutableProperties { get; protected set; }
        public Dictionary<PropertyInfo, string> PropertyColumnMappings { get; protected set; }

        protected EntityMappingCache()
        {
            
        }

        protected bool _sharding;
        protected string _rawTableName;

        public string GetTableName(string shardingIndex)
        {
            return _sharding ? string.Format(_rawTableName, shardingIndex) : _rawTableName;
        }
    }
}
