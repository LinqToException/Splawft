using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Splawft
{
    /// <summary>
    /// Provides some extensions that may come in handy.
    /// </summary>
    internal static class Extensions
    {
        // Unity can be a nuisance.
        internal static string UnifyNewlines(this string str) => str.Replace("\r\n", "\n").Replace("\n", "\r\n");

        /// <summary>
        /// Attempts to get the most-inner type of <paramref name="type"/> if it is a container.
        /// </summary>
        public static Type DeArray(this Type type)
        {
            while (type.IsArray && type.HasElementType)
                type = type.GetElementType();

            return type;
        }

        /// <summary>
        /// Returns the type name as a C# identifier.
        /// </summary>
        public static string GetClassName(this Type type, bool includeNamespace = true, bool includeDeclaring = true)
        {
            if (type == null)
            {
                Debug.LogWarning("null type passed: " + UnityEngine.StackTraceUtility.ExtractStackTrace());
                return "null";
            }

            if (type.IsGenericParameter)
                return type.Name;

            if (type.IsArray && type.HasElementType)
                return $"{GetClassName(type.GetElementType())}[]";

            var result = "";
            if (includeNamespace && !string.IsNullOrEmpty(type.Namespace))
                result += $"{type.Namespace}.";

            if (includeDeclaring && type.IsNested)
                result += $"{GetClassName(type.DeclaringType, false, true)}.";

            var name = type.Name;
            if (type.IsGenericType && type.GetGenericTypeDefinition() != null && type.GetGenericArguments() != null)
            {
                var t = type.GetGenericTypeDefinition().Name;
                if (t.Contains("`"))
                    t = t.Remove(t.IndexOf("`"));

                result += t + "<" + string.Join(", ", type.GetGenericArguments().Select(s => GetClassName(s))) + ">";
            }
            else
                result += type.Name;

            return result;
        }

        /// <summary>
        /// Returns the root (non-nested) type
        /// </summary>
        public static Type GetRoot(this Type type)
        {
            while (type.IsNested)
                type = type.DeclaringType;

            return type;
        }

        /// <summary>
        /// Returns whether a field can (likely) be Unity serialized or not.
        /// </summary>
        public static bool IsUnitySerializable(this FieldInfo field)
        {
            if (field.IsStatic || (field.IsInitOnly && !IsAnonymous(field.FieldType)))
                return false;

            var fieldType = field.FieldType;

            var attrib = field.GetCustomAttributes(false);

            if (attrib.OfType<NonSerializedAttribute>().Any())
                return false;

            if (!field.IsPublic && !attrib.OfType<SerializeField>().Any())
                return false;

            return IsUnitySerializable(fieldType);
        }

        /// <summary>
        /// Gets whether a type is Unity-serializable or not.
        /// </summary>
        public static bool IsUnitySerializable(this Type type)
        {
            if (type.IsPrimitive
                || type == typeof(bool)
                || type == typeof(decimal)
                || type == typeof(string)
                || type.IsEnum
                || type == typeof(Vector2)
                || type == typeof(Vector3)
                || type == typeof(Vector4)
                || type == typeof(Quaternion)
                || type == typeof(Matrix4x4)
                || type == typeof(Rect)
                || type == typeof(Color)
                || type == typeof(LayerMask)
                || type == typeof(AnimationCurve))
                return true;

            if (type.IsArray && type.HasElementType)
                return IsUnitySerializable(type.GetElementType());

            if (((type.IsNested && type.IsNestedPublic) || type.IsPublic) && (typeof(UnityEngine.Object).IsAssignableFrom(type) || type.GetCustomAttributes(true).OfType<SerializableAttribute>().Any()))
                return true;

            // because sometimes, we want to wrap things in a hacky way.
            if (type.IsAnonymous())
                return true;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return IsUnitySerializable(type.GetGenericArguments()[0]);

            return false;
        }

        /// <summary>
        /// Attempts to figure out if a type is anonymous.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsAnonymous(this Type type)
        {
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && type.Attributes.HasFlag(TypeAttributes.NotPublic);
        }

        /// <summary>
        /// Creates a "guid" out of a string by md5'ing it.
        /// </summary>
        public static string ToMd5Guid(this string str) => ToMd5Guid(Encoding.UTF8.GetBytes(str));

        /// <summary>
        /// Creates a "guid" out of a byte array by md5'ing it.
        /// </summary>
        public static string ToMd5Guid(this byte[] str)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
                return string.Concat(md5.ComputeHash(str).Select(i => Convert.ToString(i, 16).PadLeft(2, '0')));
        }
    }
}