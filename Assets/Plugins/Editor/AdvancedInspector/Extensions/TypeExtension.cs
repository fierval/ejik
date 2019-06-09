using System;

namespace AdvancedInspector
{
    /// <summary>
    /// Extends Enum with some bitfields related method.
    /// </summary>
    public static class TypeExtension
    {
        /// <summary>
        /// Returns the nearest parent class that is generic. 
        /// </summary>
        public static Type GetBaseGenericType(this Type type)
        {
            if (type.IsGenericType)
                return type;

            if (type.BaseType == null)
                return null;

            return type.BaseType.GetBaseGenericType();
        }
    }
}