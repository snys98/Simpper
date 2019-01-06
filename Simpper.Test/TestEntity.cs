using System;
using System.Collections.Generic;
using System.Text;

namespace Simpper.Test
{
    [Table("TestEntity")]
    public class TestEntity
    {
        [Key]
        [Identity]
        public int Id { get; set; }

        [Column("StringField")]
        public string StringField { get; set; }
        [Column("IntField")]
        public int IntField { get; set; }
        public string ExtraReadOnly {
            get { return this.StringField + this.IntField; }
        }
        [NotMapped]
        public string ExtraNotMapped { get; set; }
        [NotMapped]
        public string ExtraNotMappedWithBackingField { get; set; }
    }
}
