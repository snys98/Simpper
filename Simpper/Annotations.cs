using System;
using System.Collections.Generic;
using System.Text;

namespace Simpper
{
    /// <summary>
    ///     Optional Table attribute.
    ///     You can use the System.ComponentModel.DataAnnotations version in its place to specify the table name of a poco
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        ///     Optional Table attribute.
        /// </summary>
        /// <param name="tableName"></param>
        public TableAttribute(string tableName)
        {
            Name = tableName;
            Sharding = tableName.Contains("_{0}");
        }

        /// <summary>
        ///     Name of the table
        /// </summary>
        public string Name { get; }

        public bool Sharding { get; private set; }
    }

    /// <summary>
    ///     Optional Column attribute.
    ///     You can use the System.ComponentModel.DataAnnotations version in its place to specify the table name of a poco
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        /// <summary>
        ///     Optional Column attribute.
        /// </summary>
        /// <param name="columnName"></param>
        public ColumnAttribute(string columnName)
        {
            Name = columnName;
        }

        /// <summary>
        ///     Name of the column
        /// </summary>
        public string Name { get; }
    }

    /// <summary>
    ///     Optional Key attribute.
    ///     You can use the System.ComponentModel.DataAnnotations version in its place to specify the Primary Key of a poco
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute
    {

        public KeyAttribute()
        {
        }
    }

    /// <summary>
    ///     Optional Key attribute.
    ///     You can use the System.ComponentModel.DataAnnotations version in its place to specify the Primary Key of a poco
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IdentityAttribute : Attribute
    {

        public IdentityAttribute()
        {
        }
    }

    /// <summary>
    ///     Optional NotMapped attribute.
    ///     You can use the System.ComponentModel.DataAnnotations version in its place to specify that the property is not
    ///     mapped
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NotMappedAttribute : Attribute
    {
    }
}
