# Splawft

Splawft does stuff with Unity games. In particular, it's a modding tool that enables you to create crude dumps of GameObjects (or entire scenes), including materials, meshes, scripts and the prefabs/GameObjects used. It is not an exact tool, and it isn't even meant to be one, but it can help understand how a certain object is built in order to mod/reverse engineer it.

# Requirements
Splawft requires a decent/new Unity version (2018.4+ should work) and .NET 4.x as framework. .NET Standard 2.0 could work, but has been untested.

In order to run, a copy of Harmony (LibHarmony; 0Harmony.dll in version 1.x) and JSON.NET (Newtonsoft.Json.dll) are required, as the dumper relies on those two libraries as of now.

# Usage
After referencing Splawft.dll, all actions you want to perform are on the `UnityDumper` class, which you initialize by passing the output folder. This folder will be the base folder in which Splawft will extract the data into; usually this is within an Unity project of yours.

After that, add as many game objects using `UnityDumper.AddGameObject(GameObject)` as you wish. Splawft will automatically include all references, dump all materials and meshes, and so forth.

Once you have dumped all the objects you wish to dump, you can call `UnityDumper.ToString()` to receive the string of the Unity scene created with all the prefabs you've specified. You can save those as an .unity-file in your Unity project, too.

Full example code:

```cs
// Initialize the dumper
const string baseDir = @"C:\Unity\Assets\Dumped";
var dumper = new UnityDumper(baseDir);

// Dump the prefabs
foreach (var prefab in PrefabHelper.GetPrefabs())
    dumper.AddGameObject(prefab);

// Save the scene
File.WriteAllText(Path.Combine(baseDir, "Prefabs.unity"), dumper.ToString());
```

## Utils
In case you're starting fresh on reversing a game, you might find `DebugUtils` useful. It contains, for example, a method that allows you to create the layer collision matrix that is mimicks the one currently active in-game, and dumps it in a string that can be copy-pasted into the corresponding project file within your Unity project.

## Debugging
All log output is sent to Serilog, which is also a requirement of Splawft. In order to get Splawft's output (if requested), you will need to wire it up to some sink and set up `Serilog.Log.Logger` before using any Splawft type.

# Development
In order to set up your development workspace, you'll need to jump through a few hoops:

1. Get a Newtonsoft.Json.dll that works with your Unity version. The game might supply one already.
2. Get a 0Harmony.dll (LibHarmony 1.x) that works with your Unity version. The game or modding community might supply one already.
3. Put the two DLLs into a folder.
4. Copy the `Paths.user.example` to `Paths.user` and adjust the paths:
 - `LibDir` is the path to the folder where you've put the assemblies from step 1 and 2.
 - `UnityDir`is the path to the editor's data directory (which contains the necessary UnityEngine files).
5. You're ready to build!

# Fine-tuning
In case you don't want to dump certain C# components, you may disable them by using the static property `BlacklistedTypes` on `CSharpDumper`. Simply set it to an array of types that you wish to not dump, and Splawft will neither create the corresponding .cs files nor will it serialize its data. This can be useful if you have known MonoBehaviours that are irrelevant or cause weird behaviour.

# Limitations
There are various limitations to consider when using Splawft:

- The output is unoptimized. Meshes are raw .objs, textures are all saved as .pngs.
- All serialization is done on a best-effort basis.
- All efforts at deduplication are done on a best-effort basis.
- _Everything_ is kinda done on a best-effort basis.
- The dumped content is usually not identical to the content that was originally in the game, i.e. it is not an exactly accurate dump.
- The texture dumper cannot really differentiate between albedo, normal and bumpmaps and so forth and will dump all textures as he sees fits. It is possible to fix those textures in Unity however, because that information is available there. A snippet for that is buried somewhere in the code.
- Various things are not dumped, such as
 - AudioClips
 - Animations
 - Certain built-in components
 - ScriptableObjects

# Contributing
At this point there is no way to contribute to this repository.

# Licensing
Splawft is released under the GPLv3 license. This licensing explicitly does not apply to whatever is dumped or created using Splawft or any of its components.
