using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Splawft
{
    /// <summary>
    /// Class that attempts to dump signatures of existing types into C#
    /// </summary>
    public class CSharpDumper
    {
        private readonly Serilog.ILogger logger = Serilog.Log.ForContext<CSharpDumper>();

        /// <summary>
        /// List of types that we can reference. Any other Unity-type not in this list
        /// will not be added to a class as we assume it's not dumpable.
        /// </summary>
        private static readonly Type[] WhitelistedTypes = new[]
        {
            typeof(AnimationCurve),
            typeof(AudioSource),
            typeof(AudioClip),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Matrix4x4),
            typeof(Color),
            typeof(Rect),
            typeof(LayerMask)
        };

        /// <summary>
        /// The list of types that we are not going to serialize.
        /// </summary>
        public static Type[] BlacklistedTypes = new Type[0];

        private static readonly Regex guidMatchingRegex = new Regex(@"typeof\((.+?)\), *""(.+?)""");

        private readonly string outDirectory;
        private readonly bool assemblyNameAsSubFolder;
        private readonly HashSet<Type> dumpedTypes = new HashSet<Type>();
        private readonly Queue<Type> referencedTypes = new Queue<Type>();

        /// <summary>
        /// Initializes a new dumper instance with a specified out directory and
        /// a list of types that have already been dumped.
        /// </summary>
        /// <param name="dumpedTypes">Types that already have been dumped and will not be re-dumped.</param>
        /// <param name="outDirectory">Directory that will contain the .cs files.</param>
        public CSharpDumper(IEnumerable<Type> dumpedTypes, string outDirectory, bool assemblyNameAsSubFolder)
        {
            this.dumpedTypes = new HashSet<Type>(dumpedTypes);
            this.outDirectory = outDirectory;
            this.assemblyNameAsSubFolder = assemblyNameAsSubFolder;
        }

        /// <summary>
        /// Dumps a type as script, if the script is not already existing.
        /// </summary>
        /// <param name="type">Type that should be dumped.</param>
        /// <param name="guids">
        /// Dictionary of types and guids that have been dumped. 
        /// If the return value is <see langword="false"/>, this will be <see langword="null" />
        /// </param>
        /// <returns><see langword="true"/> if the type was dumped, <see langword="false"/> otherwise.</returns>
        public bool DumpIfNotExists(Type type, out Dictionary<Type, string> guids)
        {
            type = type.DeArray().GetRoot();

            if (type.IsGenericType)
            {
                foreach (var arg in type.GetGenericArguments())
                {
                    if (arg.IsGenericParameter)
                        continue;

                    referencedTypes.Enqueue(arg);
                }

                type = type.GetGenericTypeDefinition();
            }

            if (!dumpedTypes.Add(type) || !ShouldDumpType(type))
            {
                logger.Verbose("Do not dump {Type} (ShouldDump: {Bool})", type.FullName, ShouldDumpType(type));
                guids = null;
                return false;
            }

            guids = new Dictionary<Type, string>();
            guids[type] = DumpType(type);

            // Dump all referenced types
            while (referencedTypes.Count > 0)
            {
                var childType = referencedTypes.Dequeue().DeArray().GetRoot();

                if (childType.IsGenericType)
                {
                    foreach (var arg in childType.GetGenericArguments())
                    {
                        if (arg.IsGenericParameter)
                            continue;

                        referencedTypes.Enqueue(arg);
                    }

                    childType = childType.GetGenericTypeDefinition();
                }

                if (!dumpedTypes.Add(childType) || !ShouldDumpType(childType))
                    continue;

                guids[childType] = DumpType(childType);
            }

            return true;
        }

        /// <summary>
        /// Attempts to parse a given .cs previously dumped by <see cref="CSharpDumper"/>
        /// to extract its GUID.
        /// </summary>
        /// <param name="filename">Path to the file that should be inspected.</param>
        /// <param name="guid">The GUID of the file, if any.</param>
        /// <param name="type">The type the file contains, if any.</param>
        /// <returns><see langword="true"/> if the type could be found, <see langword="false"/> otherwise.</returns>
        public static bool TryGetGuid(string filename, out string guid, out Type type)
        {
            Match m;
            using (var sr = new StreamReader(File.OpenRead(filename)))
                m = guidMatchingRegex.Match(sr.ReadLine());

            if (!m.Success)
            {
                guid = null;
                type = null;
                return false;
            }

            guid = m.Groups[2].Value;
            type = AccessTools.TypeByName(m.Groups[1].Value);
            if (type == null)
            {
                guid = null;
                type = null;
                return false;
            }

            return true;
        }
        
        private string DumpType(Type type)
        {
            logger.Verbose("Dump {Type}", type.FullName);
            var cs = CreateCSharp(type);
            var dir = outDirectory;

            // Separate files by assembly to enable .asmdef magicks
            if (assemblyNameAsSubFolder)
                dir = Path.Combine(dir, type.Assembly.GetName().Name);

            // Unity naming requires that the MonoBehaviour has the same name as the file
            // so namespaces are simulated using folders
            if (!string.IsNullOrEmpty(type.Namespace))
                dir = Path.Combine(dir, type.Namespace);

            // Assure that the folder exists.
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var csFileName = Path.Combine(dir, type.GetClassName(false, true).Replace('<', '(').Replace('>', ')') + ".cs");
            if (File.Exists(csFileName) && TryGetGuid(csFileName, out var guid, out var newType))
            {
                if (type != newType)
                    throw new ArgumentException($"Cannot update {type.FullName}: file {csFileName} contains reference to {newType.FullName}");
            }
            else
            {
                guid = type.FullName.ToMd5Guid();
            }

            var header = $@"// {{ typeof({type.FullName}), ""{guid}"" }}";
            File.WriteAllText(csFileName, string.Join("\r\n", header, cs).UnifyNewlines());
            File.WriteAllText(csFileName + ".meta", $@"fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
");
            return guid;
        }

        private string CreateCSharp(Type type)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            if (type.Namespace != null)
                sb.AppendLine($"namespace {type.Namespace} {{");

            if (type.IsEnum)
            {
                var ut = type.GetEnumUnderlyingType();
                sb.AppendLine($"public enum {type.Name} : {ut.FullName} {{");
                sb.AppendLine(string.Join(",\n", Enum.GetValues(type).OfType<object>().Select(val => $"  {val} = {Convert.ChangeType(val, ut)}")));
                sb.AppendLine("}");
            }
            else
            {
                if (ShouldDumpType(type.BaseType))
                    referencedTypes.Enqueue(type.BaseType.GetRoot());

                if (type.IsGenericType)
                {
                    foreach (var arg in type.GetGenericArguments())
                        if (ShouldDumpType(arg))
                            referencedTypes.Enqueue(arg.GetRoot());
                }

                DumpClass(type, sb, type.Namespace == null ? -4 : 0);
            }

            if (type.Namespace != null)
                sb.AppendLine("}");

            return sb.ToString();
        }

        private void DumpClass(Type type, StringBuilder sb, int indentionLevel, bool shorten = false)
        {
            string ind = new string(' ', indentionLevel + 4);
            if (type.IsEnum)
            {
                var ut = type.GetEnumUnderlyingType();
                sb.AppendLine($"{ind}    public enum {type.Name} : {ut.FullName} {{");
                sb.AppendLine(string.Join(",\n", Enum.GetValues(type).OfType<object>().Select(val => $"{ind}        {val} = {Convert.ChangeType(val, ut)}")));
            }
            else
            {
                if (type.GetCustomAttributes(true).OfType<SerializableAttribute>().Any())
                    sb.AppendLine($"{ind}[System.Serializable]");

                sb.AppendLine($"{ind}public {(type.IsAbstract && !type.IsInterface ? "abstract " : "")}partial {(type.IsInterface ? "interface" : "class")} {type.GetClassName(false, !shorten)}{(type.BaseType != null ? " : " + type.BaseType.GetClassName() : "")} {{");
                foreach (var field in AccessTools.GetDeclaredFields(type))
                {
                    if (!field.IsPublic && !field.GetCustomAttributes(false).OfType<SerializeField>().Any())
                        continue;

                    if (field.GetCustomAttributes(false).OfType<NonSerializedAttribute>().Any())
                        continue;

                    if (field.IsStatic || field.IsInitOnly)
                        continue;

                    var ft = field.FieldType;

                    if (ft.IsArray)
                        ft = ft.GetElementType();

                    // Don't do types that are not assiganble
                    if (!ft.IsEnum
                        && !typeof(UnityEngine.Object).IsAssignableFrom(ft)
                        && !ft.GetCustomAttributes(true).OfType<SerializableAttribute>().Any()
                        && !WhitelistedTypes.Contains(ft))
                    {
                        logger.Debug("Skip incompatible type {Fieldtype} {Type}.{Name}", ft.FullName, type.FullName, field.Name);
                        continue;
                    }

                    if (BlacklistedTypes.Contains(ft))
                        continue;

                    // Do not dump generic fields - they can't be serialized anyway.
                    if (ft.IsGenericType || ft.IsConstructedGenericType)
                        continue;

                    sb.AppendLine($"{ind}    public {field.FieldType.GetClassName()} {field.Name};");
                    referencedTypes.Enqueue(field.FieldType.GetRoot());
                }

                foreach (var nested in type.GetNestedTypes())
                {
                    if (ShouldDumpNested(nested))
                    {
                        sb.AppendLine();
                        DumpClass(nested, sb, indentionLevel + 4, true);
                    }
                    else
                        logger.Debug("Do not dump {NestedType} (pub: {IsPublic}, obj: {IsObject}, serial: {IsSerializable}, enum: {IsEnum}.", nested.FullName, nested.IsNestedPublic, typeof(UnityEngine.Object).IsAssignableFrom(nested), nested.GetCustomAttributes(true).OfType<SerializableAttribute>().Any(), nested.IsEnum);
                }
            }

            sb.AppendLine($"{ind}}}");
        }

        private bool ShouldDumpNested(Type type)
        {
            if (!ShouldDumpType(type))
                return false;

            if (type.IsNestedPublic && type.IsEnum)
                return true;

            if (!type.IsNestedPublic || (!typeof(UnityEngine.Object).IsAssignableFrom(type) && !type.GetCustomAttributes(true).OfType<SerializableAttribute>().Any()))
                return false;

            return true;
        }

        private bool ShouldDumpType(Type type)
        {
            if (type == null)
                return false;

            var assemblyName = type.Assembly.GetName().Name;
            if (assemblyName.StartsWith("UnityEngine") || assemblyName == "mscorlib" || assemblyName.StartsWith("System"))
                return false;

            if (type.Namespace?.StartsWith("UnityEngine") == true || type.Namespace?.StartsWith("TMPro") == true)
                return false;

            if (BlacklistedTypes.Contains(type))
                return false;

            return true;
        }
    }
}
