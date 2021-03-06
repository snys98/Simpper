﻿using System;
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
        [Column("DateTimeField")]
        public DateTime DateTimeField { get; set; } = DateTime.Now;
        [Column("NullableDateTimeField")]
        public DateTime? NullableDateTimeField { get; set; }
        [Column("EnumField")]
        public TestEnum EnumField { get; set; }
        [Column("DecimalField")]
        public decimal DecimalField { get; set; }
        [Column("GuidField")]
        public Guid GuidField { get; set; }
        [Column("LongField")]
        public long LongField { get; set; }
        public string ExtraReadOnly {
            get { return this.StringField + this.IntField; }
        }
        [NotMapped]
        public string ExtraNotMapped { get; set; }
        [NotMapped]
        public string ExtraNotMappedWithBackingField { get; set; }
    }

    public enum TestEnum
    {
        Default,
        One
    }
}
