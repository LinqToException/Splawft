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
                m_PreInfinity = ConvertWrapMode(curve.preWrapMode),
                m_PostInfinity = ConvertWrapMode(curve.postWrapMode),
                m_RotationOrder = 4  // https://xkcd.com/221/ - for reals, are other values possible?
            });
        }

        // Apparently, Unity's internal serialization and WrapMode do not add up
        // (I blame legacy stuff)
        // For AnimationCurves in recent-ish Unity, there's only 3 modes:
        // PingPong (0), Loop (1) and Clamp (2)
        // WrapMode knows a "few" more
        private static int ConvertWrapMode(WrapMode mode)
        {
            switch (mode)
            {
                case WrapMode.Clamp:
                case WrapMode.ClampForever:
                case WrapMode.Default:
                default:
                    return 2;
                case WrapMode.Loop:
                    return 1;
                case WrapMode.PingPong:
                    return 0;
            }
        }
    }
}