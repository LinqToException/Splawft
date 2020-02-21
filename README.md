# Splawft

Splawft does stuff with Unity games. In particular, it's a modding tool that enables you to create crude dumps of GameObjects (or entire scenes), including materials, meshes, scripts and the prefabs/GameObjects used. It is not an exact tool, and it isn't even meant to be one, but it can help understand how a certain object is built in order to mod/reverse engineer it.

# Requirements
Splawft requires a decent/new Unity version (2018.4+ should work) and .NET 4.x as framework. .NET Standard 2.0 could work, but has been untested.

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
File.WriteAllText(Path.Combine(baseDir, "Prefabs.unity"));
```

# Fine-tuning
In case you don't want to dump certain C# components, you may disable them by using the static property `BlacklistedTypes` on `CSharpDumper`. Simply set it to an array of types that you wish to not dump, and Splawft will neither create the corresponding .cs files nor will it serialize its data. This can be useful if you have known MonoBehaviours that are irrelevant or cause weird behaviour.

# Limitations
There are various limitations to consider when using Splawft:

- The output is unoptimized. Meshes are raw .objs, textures are all saved as .pngs.
- All serialization is done on a best-effort basis.
- All efforts at deduplication are done on a best-effort basis.
- _Everything_ is kinda done on a best-effort basis.
- The dumped content is usually not identical to the content that was originally in the game, i.e. it is not an exactly accurate dump.
- Various things are not dumped, such as
 - AudioClips
 - Animations
 - Certain built-in components
 - ScriptableObjects

# Contributing
At this point there is no way to contribute to this repository.

# Licensing
Splawft is released under the GPLv3 license. This licensing explicitly does not apply to whatever is dumped or created using Splawft or any of its components.
