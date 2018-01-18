using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace DataTransfer.Core.CVS
{
    /// <summary>
    /// 属性解析类
    /// </summary>
    internal class PropertyResolver
    {
        private Type _type;
        private ReadOnlyCollection<PropertyInfo> _properties;
        private Dictionary<PropertyInfo, LateBoundPropertyGet> _propertyValueGetFunctions;
        private PropertyColumnComparer _propertyColumnComparer;
        internal ReadOnlyCollection<PropertyInfo> Properties
        {
            get
            {
                return _properties;
            }
        }
        internal Dictionary<PropertyInfo, LateBoundPropertyGet> PropertyGetValueFunctions
        {
            get
            {
                return _propertyValueGetFunctions;
            }
        }
        public PropertyResolver(Type type)
        {
            _type = type;
            _propertyColumnComparer = new PropertyColumnComparer();
            _properties = GetProperties();
            _propertyValueGetFunctions = BuildPropertyValueGetFunctions();
        }
        private ReadOnlyCollection<PropertyInfo> GetProperties()
        {
            var list = _type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite).ToList();
            list.Sort(_propertyColumnComparer);
            return list.AsReadOnly();
        }
        private Dictionary<PropertyInfo, LateBoundPropertyGet> BuildPropertyValueGetFunctions()
        {
            if (_properties == null)
                _properties = GetProperties();
            var lateBondPropertyGetList = new Dictionary<PropertyInfo, LateBoundPropertyGet>(_properties.Count);
            foreach (var propertyInfo in _properties)
            {
                lateBondPropertyGetList.Add(propertyInfo, DelegateFactory.CreateGet(propertyInfo));
            }
            return lateBondPropertyGetList;
        }
        class PropertyColumnComparer : IComparer<PropertyInfo>
        {
            public int Compare(PropertyInfo x, PropertyInfo y)
            {
                var xColumnAttribute = x.PropertyType.GetCustomAttribute<ColumnIndexAttribute>(true);
                var yColumnAttribute = y.PropertyType.GetCustomAttribute<ColumnIndexAttribute>(true);
                if (xColumnAttribute != null && yColumnAttribute != null)
                {
                    return xColumnAttribute.Index - yColumnAttribute.Index;
                }
                if (xColumnAttribute == null && yColumnAttribute == null)
                {
                    return 0;
                }
                if (xColumnAttribute != null)
                {
                    return -1;
                }
                if (yColumnAttribute != null)
                {
                    return 1;
                }
                return 0;
            }
        }
    }
}
