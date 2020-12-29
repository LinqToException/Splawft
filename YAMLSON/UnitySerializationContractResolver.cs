using HarmonyLib;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace Splawft.YAMLSON
{
    internal class UnitySerializedContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(System.Type objectType)
        {
            List<MemberInfo> fields = new List<MemberInfo>();
            if (objectType.IsAnonymous())
            {
                foreach (var property in objectType.GetProperties())
                {
                    if (!property.PropertyType.IsUnitySerializable())
                        continue;

                    fields.Add(property);
                }
            }

            // Fields
            foreach (var field in objectType.GetFields(AccessTools.all))
            {
                if (!field.IsUnitySerializable())
                    continue;

                fields.Add(field);
            }

            return fields;
        }
    }
}