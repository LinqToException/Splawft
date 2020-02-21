using System.Collections.Generic;
using System.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Harmony;
using UnityEngine;
using Object = UnityEngine.Object;
using Newtonsoft.Json;
using Splawft.YAMLSON;

namespace Splawft
{
    public partial class UnityDumper
    {
        private const string MissingComponentGuid = "f158462da24fc38419788cdc1b4d5bc0";
        private static readonly Dictionary<Type, string> MonoBehaviourGuids = new Dictionary<Type, string>();
        private readonly JsonSerializerSettings jsonSettings;
        private readonly Serilog.ILogger logger = Serilog.Log.ForContext<UnityDumper>();
        private CSharpDumper dumper;

        /// <summary>
        /// If set, all .cs-files dumped will be overwritten
        /// </summary>
        public static bool ForceOverwriteCSharpFiles { get; set; } = true;

        private HashSet<Object> dumped = new HashSet<Object>();
        private StringBuilder sb = new StringBuilder();
        private MeshDumper meshDumper;
        private MaterialDumper materialDumper;
        private TextureDumper textureDumper;

        private static string Stringify(Vector3 v) => $"{{x: {v.x}, y: {v.y}, z: {v.z}}}";
        private static string Stringify(Quaternion q) => $"{{x: {q.x}, y: {q.y}, z: {q.z}, w: {q.w}}}";
        private static string Escape(string s) => $"'{s.Replace("'", "''")}'";

        public UnityDumper(string path = null, bool assemblyNameAsSubfolder = true)
        {
            sb.AppendLine(@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:");

            if (!string.IsNullOrEmpty(path))
            {
                if (!ForceOverwriteCSharpFiles)
                    RefreshGuidsUsingEvilHack(path);

                dumper = new CSharpDumper(ForceOverwriteCSharpFiles ? (ICollection<Type>)new List<Type>() : MonoBehaviourGuids.Keys, path, assemblyNameAsSubfolder);
                meshDumper = new MeshDumper(Path.Combine(path, "models"));
                textureDumper = new TextureDumper(Path.Combine(path, "textures"));
                materialDumper = new MaterialDumper(this, Path.Combine(path, "materials"), textureDumper);
            }

            jsonSettings = new JsonSerializerSettings()
            {
                ContractResolver = new UnitySerializedContractResolver()
            };

            jsonSettings.Converters.Add(new AnimationCurveConverter());
            jsonSettings.Converters.Add(new UnityObjectTracker(this));
            jsonSettings.Converters.Add(new BoolConverter());
        }

        [Conditional("DEBUG")]
        internal static void RefreshGuidsUsingEvilHack(string directory)
        {
            foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                if (CSharpDumper.TryGetGuid(file, out var guid, out var type))
                    MonoBehaviourGuids[type] = guid;
            }
        }

        public void AddGameObject(GameObject go)
        {
            // Not entirely sure why yet, but go can, sometimes, be null
            // Absolutely no idea where THAT comes from
            if (!dumped.Add(go) || go == null)
                return;
            sb.AppendLine($@"--- !u!1 &{go.GetInstanceID()}
GameObject:
  m_ObjectHideFlags: {(int)go.hideFlags}
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInternal: {{fileID: 0}}
  serializedVersion: 6
  m_Component:");

            // Avoid that we're cross-writing here
            var oldSb = this.sb;
            this.sb = new StringBuilder();
            foreach (var component in go.GetComponents<Object>())
            {
                if (TryAddComponent(component))
                    oldSb.AppendLine($"  - component: {{fileID: {component.GetInstanceID()}}}");
            }

            oldSb.AppendLine($@"  m_Layer: {go.layer}
  m_Name: {Escape(go.name)}
  m_TagString: {Escape(go.tag)}
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {(go.activeSelf ? 1 : 0)}");

            // We've finished writing the gameObject; push it back
            oldSb.Append(this.sb);
            this.sb = oldSb;
        }

        /// <summary>
        /// Tries to add a component; returns true if it was successful, false if it wasn't
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        internal bool TryAddComponent(UnityEngine.Object component)
        {
            if (component == null)
                return false;

            if (component is MonoBehaviour mb)
            {
                if (MonoBehaviourGuids.TryGetValue(mb.GetType(), out var t))
                    AddMonoBehaviour(mb, t);
                else if (dumper != null && dumper.DumpIfNotExists(mb.GetType(), out var guids))
                {
                    foreach (var p in guids)
                        MonoBehaviourGuids[p.Key] = p.Value;
                    AddMonoBehaviour(mb, MonoBehaviourGuids[mb.GetType()]);
                }
                // Don't dump missing MonoBehaviours for the time being.
                //else
                //    AddMissingMonoBehaviour(mb);
                else
                    return false;

                return true;
            }

            // This desperately needs something better.
            // Like a pluggable system, or something less...
            // hardcoded-y.
            switch (component)
            {
                case BoxCollider bc:
                    AddBoxCollider(bc);
                    return true;
                case CapsuleCollider cc:
                    AddCapsuleCollider(cc);
                    return true;
                case Rigidbody r:
                    AddRigidbody(r);
                    return true;
                case HingeJoint hj:
                    AddHingeJoint(hj);
                    return true;
                case ConfigurableJoint cj:
                    AddConfigurableJoint(cj);
                    return true;
                case Transform t:
                    AddTransform(t);
                    return true;
                case GameObject go:
                    AddGameObject(go);
                    return true;
                case AudioSource auso:
                    AddAudioSource(auso);
                    return true;
                case MeshFilter mf:
                    AddMeshFilter(mf);
                    return true;
                case MeshRenderer mr:
                    AddMeshRenderer(mr);
                    return true;
                default:
                    return false;
            }
        }

        public void AddTransform(Transform transform)
        {
            if (!dumped.Add(transform))
                return;

            var siblings = transform.parent?.OfType<Transform>().ToList();
            int rootOrder = 0;
            if (siblings != null)
                rootOrder = siblings.IndexOf(transform);

            sb.AppendLine($@"--- !u!4 &{transform.GetInstanceID()}
Transform:
  m_ObjectHideFlags: {(int)transform.hideFlags}
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInternal: {{fileID: 0}}
  m_GameObject: {{fileID: {transform.gameObject.GetInstanceID()}}}
  m_LocalRotation: {Stringify(transform.localRotation)}
  m_LocalPosition: {Stringify(transform.localPosition)}
  m_LocalScale: {Stringify(transform.localScale)}
  m_Children:");
            foreach (Transform child in transform)
            {
                sb.AppendLine($"  - {{fileID: {child.GetInstanceID()}}}");
            }

            sb.AppendLine($@"  m_Father: {{fileID: {transform.parent?.GetInstanceID() ?? 0}}}
  m_RootOrder: {rootOrder}
  m_LocalEulerAnglesHint: {Stringify(transform.localEulerAngles)}");

            AddGameObject(transform.gameObject);
            if (transform.parent != null)
                AddTransform(transform.parent);
            foreach (Transform child in transform)
                AddTransform(child);
        }

        public override string ToString() => sb.ToString().UnifyNewlines();
        internal string Jsonify(object value) => JsonConvert.SerializeObject(value, Formatting.None, jsonSettings);

        private void AddAudioSource(AudioSource a)
        {
            if (!dumped.Add(a))
                return;

            sb.AppendLine($@"--- !u!82 &{a.GetInstanceID()}
AudioSource:
  m_ObjectHideFlags: {(int)a.hideFlags}
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInternal: {{fileID: 0}}
  m_GameObject: {{fileID: {a.gameObject.GetInstanceID()}}}
  m_Enabled: {a.enabled}
  serializedVersion: 4
  OutputAudioMixerGroup: {{fileID: 0}}
  m_audioClip: {{fileID: 0}}
  m_PlayOnAwake: {(a.playOnAwake ? 1 : 0)}
  m_Volume: {a.volume}
  m_Pitch: {a.pitch}
  Loop: {(a.loop ? 1 : 0)}
  Mute: {(a.mute ? 1 : 0)}
  Spatialize: {(a.spatialize ? 1 : 0)}
  SpatializePostEffects: {(a.spatializePostEffects ? 1 : 0)}
  Priority: {a.priority}
  DopplerLevel: {a.dopplerLevel}
  MinDistance: {a.minDistance}
  MaxDistance: {a.maxDistance}
  Pan2D: {a.panStereo}
  rolloffMode: {(int)a.rolloffMode}
  BypassEffects: {(a.bypassEffects ? 1 : 0)}
  BypassListenerEffects: {(a.bypassListenerEffects ? 1 : 0)}
  BypassReverbZones: {(a.bypassReverbZones ? 1 : 0)}
  rolloffCustomCurve: {Jsonify(a.GetCustomCurve(AudioSourceCurveType.CustomRolloff))}
  panLevelCustomCurve: {Jsonify(a.GetCustomCurve(AudioSourceCurveType.SpatialBlend))}
  spreadCustomCurve: {Jsonify(a.GetCustomCurve(AudioSourceCurveType.Spread))}
  reverbZoneMixCustomCurve: {Jsonify(a.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix))}");
            AddGameObject(a.gameObject);
        }

        private void AddMonoBehaviour(MonoBehaviour o, string guid)
        {
            if (!dumped.Add(o))
                return;

            sb.AppendLine($@"--- !u!114 &{o.GetInstanceID()}
MonoBehaviour:
  m_ObjectHideFlags: {(int)o.hideFlags}
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInternal: {{fileID: 0}}
  m_GameObject: {{fileID: {o.gameObject.GetInstanceID()}}}
  m_Enabled: {(o.enabled ? 1 : 0)}
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: ");

            var oldSb = sb;
            this.sb = new StringBuilder();
            List<MonoBehaviour> mbs = new List<MonoBehaviour>();
            foreach (var field in o.GetType().GetFields(AccessTools.all))
            {
                if (!field.IsUnitySerializable())
                    continue;

                var ft = field.FieldType;
                var val = field.GetValue(o);

                var converted = Jsonify(val);
                if (converted == "null" || string.IsNullOrEmpty(converted))
                {
                    logger.Debug("Did not serialize {FieldType} {TypeName}:{FieldName}; null", ft.FullName, o.GetType().FullName, field.Name);
                    continue;
                }
                oldSb.AppendLine($"  {field.Name}: {converted}");
            }

            oldSb.AppendLine(this.sb.ToString());
            this.sb = oldSb;
            AddGameObject(o.gameObject);
        }

        private void AddMeshFilter(MeshFilter mf)
        {
            if (!dumped.Add(mf))
                return;

            // No dumper for meshes defined.
            if (this.meshDumper == null)
                return;

            var mesh = mf.sharedMesh != null ? mf.sharedMesh : mf.mesh;
            var meshData = this.meshDumper?.DumpMesh(mesh);
            sb.AppendLine($@"--- !u!33 &{mf.GetInstanceID()}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {mf.gameObject.GetInstanceID()}}}
  m_Mesh: {{fileID: {meshData?.FileId ?? 0}, guid: {meshData?.Guid ?? "deadbeef"}, type: {meshData.Type} }}");
        }

        private static object FormatMaterialReference(string guid)
        {
            if (guid == null)
                return new { fileID = 0 };

            return new
            {
                fileID = 2100000,
                guid = guid,
                type = 2
            };
        }

        private void AddMeshRenderer(MeshRenderer mr)
        {
            if (!dumped.Add(mr))
                return;

            if (this.meshDumper == null)
                return;

            var materials = Jsonify(mr.sharedMaterials.Select(this.materialDumper.DumpMaterial).Select(FormatMaterialReference));
            sb.AppendLine($@"--- !u!23 &{mr.GetInstanceID()}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {mr.gameObject.GetInstanceID()}}}
  m_Enabled: {(mr.enabled ? 1 : 0)}
  m_CastShadows: {(int)mr.shadowCastingMode}
  m_ReceiveShadows: {(mr.receiveShadows ? 1 : 0)}
  m_DynamicOccludee: {(mr.allowOcclusionWhenDynamic ? 1 : 0)}
  m_MotionVectors: {(int)mr.motionVectorGenerationMode}
  m_LightProbeUsage: {(int)mr.lightProbeUsage}
  m_ReflectionProbeUsage: {(int)mr.reflectionProbeUsage}
  m_RenderingLayerMask: {mr.renderingLayerMask}
  m_RendererPriority: {mr.rendererPriority}
  m_Materials: {materials}");
        }
    }
}
