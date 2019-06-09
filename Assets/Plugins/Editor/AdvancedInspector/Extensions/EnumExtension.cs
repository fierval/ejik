using System;
using System.Reflection;

namespace AdvancedInspector
{
    /// <summary>
    /// Extends Enum with some bitfields related method.
    /// </summary>
    public static class EnumExtension
    {
        /// <summary>
        /// Add a value to a bitfield.
        /// </summary>
        public static T Append<T>(this System.Enum type, T value) where T : struct
        {
            return (T)(ValueType)(((int)(ValueType)type | (int)(ValueType)value));
        }

        /// <summary>
        /// Remove a value from a bitfield.
        /// </summary>
        public static T Remove<T>(this System.Enum type, T value) where T : struct
        {
            return (T)(ValueType)(((int)(ValueType)type & ~(int)(ValueType)value));
        }

        /// <summary>
        /// Test if a bitfield has a value.
        /// </summary>
        public static bool Has<T>(this System.Enum type, T value) where T : struct
        {
            return (((int)(ValueType)type & (int)(ValueType)value) == (int)(ValueType)value);
        }

        /// <summary>
        /// Gets an attribute on an enum field value
        /// </summary>
        /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
        /// <param name="enumVal">The enum value</param>
        /// <returns>The attribute of type T that exists on the enum value</returns>
        public static T GetAttribute<T>(this Enum enumVal)
        {
            Type type = enumVal.GetType();
            MemberInfo[] memInfo = type.GetMember(enumVal.ToString());
            object[] attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
            return (attributes.Length > 0) ? (T)attributes[0] : default(T);
        }
    }
}