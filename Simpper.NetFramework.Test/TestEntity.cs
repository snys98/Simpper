using System;

namespace Simpper.NetFramework.Test
{
    [OrmTable("TestEntity")]
    public class TestEntity
    {
        public TestEntity()
        {
            DateTimeField = DateTime.Now;
        }

        [OrmKey]
        [OrmIdentity]
        public int Id { get; set; }

        [OrmColumn("StringField")]
        public string StringField { get; set; }
        [OrmColumn("IntField")]
        public int IntField { get; set; }
        [OrmColumn("DateTimeField")]
        public DateTime DateTimeField { get; set; }
        [OrmColumn("NullableDateTimeField")]
        public DateTime? NullableDateTimeField { get; set; }
        [OrmColumn("EnumField")]
        public TestEnum EnumField { get; set; }
        [OrmColumn("DecimalField")]
        public decimal DecimalField { get; set; }
        [OrmColumn("GuidField")]
        public Guid GuidField { get; set; }
        [OrmColumn("LongField")]
        public long LongField { get; set; }
        public string ExtraReadOnly {
            get { return this.StringField + this.IntField; }
        }
        [OrmNotMapped]
        public string ExtraNotMapped { get; set; }
        [OrmNotMapped]
        public string ExtraNotMappedWithBackingField { get; set; }
    }

    public enum TestEnum
    {
        Default,
        One
    }
}
