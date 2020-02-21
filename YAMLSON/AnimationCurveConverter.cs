using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using System;

namespace Splawft.YAMLSON
{
    internal class AnimationCurveConverter : JsonConverter
    {
        public override bool CanConvert(System.Type objectType) => objectType == typeof(AnimationCurve);
        public override bool CanRead => false;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var curve = (AnimationCurve)value;
            serializer.Serialize(writer, new
            {
                m_Curve = curve.keys.Select(key => new
                {
                    serializedVersion = 3,
                    key.time,
                    key.value,
                    inSlope = key.inTangent,
                    outSlope = key.outTangent,
                    tangentMode = (int)key.tangentMode,
                    weightedMode = key.weightedMode,
                    inWeight = key.inWeight,
                    outWeight = key.outWeight
                }).ToList(),
                m_PreInfinity = (int)curve.preWrapMode,
                m_PostInfinity = (int)curve.postWrapMode,
                m_RotationOrder = 4  // https://xkcd.com/221/ - for reals, are other values possible?
            });
        }
    }
}