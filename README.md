# MiSide-AssetLoader
Firstly planned as a simple mod, very fast turned into a universal asset loader for any Unity game. You can use it as a reference for your own projects.

### Features:
 - Standard-like Unity methods which implemented through reflection
 - Assets loading
   - Textures (any format supported)
   - Audio (only OGG)
   - Meshed (FBX and OBJ)
   - Video (MP4)
 - Config files with simplified YAML
 - Scene loading handling
 - BAT scripts for easy deploying the mod

#### This mod can import skinned meshes from FBX! I have never seen implemented it elsewhere, so feel free to take notes.

### Technologies used:
 - [BepInEx](https://github.com/BepInEx/BepInEx) as modding tool 
 - [NVorbis](https://github.com/NVorbis/NVorbis) as audio loader
 - [Runtime OBJ Loader](https://assetstore.unity.com/packages/tools/modeling/runtime-obj-importer-49547) as OBJ loader
 - [Assimp](https://github.com/assimp/assimp) as FBX loader

### Requirements
Everything is already inserted into the project. The only thing you need is a .NET 6.0 Runtime AND .NET 6.0 SDKs installed properly (including MSBuildSDKsPath specified in PATHS).
I use VS Code for coding, it works perfectly for me, so it should work for you too, no need for Visual Studio Community Edition or something like that.

### Deploying
Just run ./build_with_assets.bat or ./build.bat for only building the library. And make sure to specify game folder in these files.
