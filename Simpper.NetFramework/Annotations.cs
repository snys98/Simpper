using System;

namespace Simpper.NetFramework
{
    /// <summary>
    ///     Optional Table attribute.
    ///     You can use the System.ComponentModel.DataAnnotations version in its place to specify the table name of a poco
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class OrmTableAttribute : Attribute
    {
        /// <summary>
        ///     Optional Table attribute.
        /// </summary>
        /// <param name="tableName"></param>
        public OrmTableAttribute(string tableName)
        {
            Name = tableName;
        }

        /// <summary>
        ///     Name of the table
        /// </summary>
        public string Name { get; private set; }

        public bool Sharding {
            get { return this.Name.Contains("_{0}"); }
        }
    }

    /// <summary>
    ///     Optional Column attribute.
    ///     You can use the System.ComponentModel.DataAnnotations version in its place to specify the table name of a poco
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OrmColumnAttribute : Attribute
    {
        /// <summary>
        ///     Optional Column attribute.
        /// </summary>
        /// <param name="columnName"></param>
        public OrmColumnAttribute(string columnName)
        {
            Name = columnName;
        }

        /// <summary>
        ///     Name of the column
        /// </summary>
        public string Name { get; private set; }
    }

    /// <summary>
    ///     Optional Key attribute.
    ///     You can use the System.ComponentModel.DataAnnotations version in its place to specify the Primary Key of a poco
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OrmKeyAttribute : Attribute
    {

        public OrmKeyAttribute()
        {
        }
    }

    /// <summary>
    ///     Optional Key attribute.
    ///     You can use the System.ComponentModel.DataAnnotations version in its place to specify the Primary Key of a poco
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OrmIdentityAttribute : Attribute
    {

        public OrmIdentityAttribute()
        {
        }
    }

    /// <summary>
    ///     Optional NotMapped attribute.
    ///     You can use the System.ComponentModel.DataAnnotations version in its place to specify that the property is not
    ///     mapped
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OrmNotMappedAttribute : Attribute
    {
    }
}
