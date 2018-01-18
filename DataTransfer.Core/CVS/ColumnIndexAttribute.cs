using System;

namespace DataTransfer.Core.CVS
{
    /// <summary>
    /// 类转换的列排序
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnIndexAttribute:Attribute
    {
        public int Index { get; set; }
    }
}
