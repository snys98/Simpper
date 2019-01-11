namespace Simpper.NetFramework.Test
{
    [OrmTable("ShardingEntity_{0}")]
    public class ShardingEntity
    {
        [OrmKey]
        [OrmIdentity]
        public int Id { get; set; }

        [OrmColumn("IntField")]
        public int IntField { get; set; }
    }
}