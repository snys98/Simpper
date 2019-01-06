namespace Simpper.Test
{
    [Table("ShardingEntity_{0}")]
    public class ShardingEntity
    {
        [Key]
        [Identity]
        public int Id { get; set; }

        [Column("IntField")]
        public int IntField { get; set; }
    }
}