using System;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace LethalInternship.Utils
{
    /// <summary>
    /// Utilitary class for debug infos of objects
    /// </summary>
    internal static class PropertiesAndFieldsUtils
    {
        public static void ListNamesOfObjectsArray<T>(T[] array)
        {
            Type typeObj = typeof(T);

            Plugin.LogDebug(" ");
            if (array == null)
            {
                Plugin.LogDebug($"Array of type {typeObj} is null");
                return;
            }
            if (array.Length == 0)
            {
                Plugin.LogDebug($"Array of type {typeObj} is empty");
                return;
            }

            PropertyInfo[] arrObjProperties = GetReadablePropertiesOf(typeObj);

            Plugin.LogDebug($"- List of type : {typeObj}");
            for (int i = 0; i < array.Length; i++)
            {
                T obj = array[i];
                if (obj == null)
                {
                    continue;
                }

                Plugin.LogDebug($" Object {i}: \"{NameOfObject(obj, arrObjProperties)}\"");
            }
        }

        public static void ListPropertiesAndFieldsOfArray<T>(T[] array, bool hasToListProperties = true, bool hasToListFields = true)
        {
            Type typeObj = typeof(T);

            if (array == null)
            {
                Plugin.LogDebug($"Array of type {typeObj} is null");
                return;
            }
            if (array.Length == 0)
            {
                Plugin.LogDebug($"Array of type {typeObj} is empty");
                return;
            }

            PropertyInfo[] arrObjProperties = GetReadablePropertiesOf(typeObj);
            FieldInfo[] arrObjFields = GetAllFields(typeObj);
            for (int i = 0; i < array.Length; i++)
            {
                if (hasToListProperties)
                {
                    LogProperties(array[i], typeObj, arrObjProperties);
                }
                Plugin.LogDebug(" ");
                Plugin.LogDebug($"- Fields of \"{NameOfObject(array[i], arrObjProperties)}\" of type \"{typeObj}\" :");
                if (hasToListFields)
                {
                    LogFields(array[i], typeObj, arrObjFields);
                }
            }
        }

        public static void ListPropertiesAndFields<T>(T obj, bool hasToListProperties = true, bool hasToListFields = true)
        {
            Type typeObj = typeof(T);
            PropertyInfo[] arrObjProperties = GetReadablePropertiesOf(typeObj);
            if (hasToListProperties)
            {
                LogProperties(obj, typeObj, arrObjProperties);
            }
            Plugin.LogDebug(" ");
            Plugin.LogDebug($"- Fields of \"{NameOfObject(obj, arrObjProperties)}\" of type \"{typeObj}\" :");
            FieldInfo[] arrObjFields = GetAllFields(typeObj);
            if (hasToListFields)
            {
                LogFields(obj, typeObj, arrObjFields);
            }
        }

        public static NetworkObject? GetNetworkObjectByHash(uint hashID)
        {
            FieldInfo info = typeof(NetworkObject).GetField("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null)
            {
                Plugin.LogError("GlobalObjectIdHash field is null.");
                return null;
            }
            foreach (NetworkObject obj in Resources.FindObjectsOfTypeAll<NetworkObject>())
            {
                uint GlobalObjectIdHash = (uint)info.GetValue(obj);
                if (GlobalObjectIdHash == hashID)
                {
                    return obj;
                }
            }

            return null;
        }

        private static void LogProperties<T>(T obj, Type typeObj, PropertyInfo[] arrObjProperties)
        {
            if (obj == null)
            {
                Plugin.LogDebug($"Object of type {typeObj} is null");
                return;
            }

            Plugin.LogDebug(" ");
            Plugin.LogDebug($"- Properties of \"{NameOfObject(obj, arrObjProperties)}\" of type \"{typeObj}\" :");
            foreach (PropertyInfo prop in arrObjProperties)
            {
                Plugin.LogDebug($" {prop.Name} = {GetValueOfProperty(obj, prop)}");
            }
        }

        private static string NameOfObject<T>(T obj, PropertyInfo[] propertyInfos)
        {
            object? objNameProperty = GetValueOfProperty(obj, propertyInfos.FirstOrDefault(x => x.Name == "name"));
            return objNameProperty == null ? "name null" : objNameProperty.ToString();
        }

        private static void LogFields<T>(T obj, Type typeObj, FieldInfo[] arrObjFields)
        {
            if (obj == null)
            {
                Plugin.LogDebug($"Object of type {typeObj} is null");
                return;
            }

            Plugin.LogDebug(" ");
            foreach (FieldInfo field in arrObjFields)
            {
                Plugin.LogDebug($" {field.Name} = {GetValueOfField(obj, field)}");
            }
        }

        public static FieldInfo[] GetAllFields(System.Type t)
        {
            if (t == null)
            {
                return new FieldInfo[0];
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Static | BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;
            return t.GetFields(flags).Concat(GetAllFields(t.BaseType)).ToArray();
        }

        private static PropertyInfo[] GetReadablePropertiesOf(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Where(x => x.CanRead).ToArray();
            //return type.GetProperties().Where(x => x.CanRead).ToArray();
        }

        private static object? GetValueOfProperty<T>(T obj, PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                return null;
            }
            return propertyInfo.GetAccessors(nonPublic: true)[0].IsStatic ? propertyInfo.GetValue(null) : propertyInfo.GetValue(obj, null);
        }

        private static object? GetValueOfField<T>(T obj, FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                return null;
            }
            return fieldInfo.IsStatic ? fieldInfo.GetValue(null) : fieldInfo.GetValue(obj);
        }
    }
}
