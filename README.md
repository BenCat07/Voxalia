Voxalia
-------

**Voxalia, a game about blocks and such.**

Built within our own in-house Frenetic Game Engine!

Also built using: OpenTK (a C# binding of OpenGL for the 3D rendering), BEPUphysics (a capable pure-C# physics engine), and FreneticScript (powerful and adaptive game modding).

Also used libraries:

- Open.NAT (automated port forwarding)
- Opus (network-ready voice compression)
- csogg/csvorbis (.ogg audio file handling)
- LiteDB (save file compression)
- FreneticDataSyntax (used for FreneticScript data handling and some files in Voxalia's save structure)
- lz4net (general data compression)

## What is Voxalia

Voxalia is a really cool game that will be lots of fun to play when it's ready!

It's currently in pre-alpha, but it's already got a lot of power to it!

View the [FAQ](/FAQ.md) for more information!

## Builds Status Reports and Badges

| Build Service | Status |
| ------------- | ------ |
| AppVeyor | [![AppVeyor Build Status](https://ci.appveyor.com/api/projects/status/inbj8vbo0fx4a8io/branch/master?svg=true)](https://ci.appveyor.com/project/mcmonkey4eva/voxalia/branch/master) |
| Website ([Freneticllc.com](https://freneticllc.com)) | [![Website](https://img.shields.io/website-up-down-green-red/https/freneticllc.com.svg)](https://freneticllc.com) |
| Website ([Voxalia.net](https://voxalia.net)) | [![Website](https://img.shields.io/website-up-down-green-red/https/voxalia.net.svg)](https://voxalia.net) |
| Issues Open | [![Issues_Open](https://img.shields.io/github/issues/FreneticLLC/Voxalia.svg)](https://github.com/FreneticLLC/Voxalia/issues) |
| CLA Assistant | [![CLA assistant](https://cla-assistant.io/readme/badge/FreneticLLC/Voxalia)](https://cla-assistant.io/FreneticLLC/Voxalia) |

## Windows Install Notes

- Requires OpenAL (Run `oalinst.exe` from the assets repo)
- Requires reasonably up to date graphics drivers
- Requires a 64-bit system
- Windows build is primarily tested on Windows 10 with latest NVidia drivers.

## Linux Install Notes

- Requires mono.
- Requires LibOpus.
- Requires fully up to date non-Intel graphics drivers (Must support OpenGL 4.3).
- Requires "espeak" program/package. Must be a valid "espeak" executable in path.
- Requires a 64-bit system.
- Linux build is primarily tested on Ubuntu 16.04 with latest NVidia drivers.
- Swap the folder `xulrunner` for the one in the Linux tarball.

## Mac Install Notes

- Should be similar to Linux requirements...
- Swap the contents of the folder `xulrunner` for that in the Mac compressed folder. May require some trickery here...
- ??? (Untested!)

## Compiling

- Open the `Voxalia.sln` file in Microsoft Visual Studio 2017
- Switch the configuration to `Release|x64`
- Build -> Build Solution!

## Compiling on Linux/Mac

- open the `Voxalia.sln` file in MonoDevelop
- Switch the configuration to `Release (Linux)|x64`
- Build as per normal
- Be aware that some things may not function properly!
	- If you are a Linux/Mac|mono|C# developer, consider trying to improve what you can and sending it back through a Github pull request to FreneticLLC!

## Playing

- Please CONTACT US if you wish to play the game at this time.
	- The game is not publicly available to play currently!

## Also Included

- SkeletalAnimationExtractor -> converts .dae animated models to reusable anim files.
- VoxaliaServerSamplePlugin -> a sample of a C# powered plugin for the Voxalia server.

### The License

Voxalia is Copyright (C) 2016-2017 Frenetic LLC, All Rights Reserved.

Voxalia binary assets (not necessarily included herein) are additionally Copyright (C) 2016-2017 Frenetic LLC, All Rights Reserved.

Special exceptions may apply.

Individual sub-licenses are in the source folders alongside sub-licensed files.

----

----

![YourKit](https://www.yourkit.com/images/yklogo.png)

The FreneticLLC team uses YourKit .NET Profiler to improve performance. We'd like to thank them for their amazing tool and recommend them to all .NET developers!

YourKit supports open source projects with its full-featured .NET Profiler.  
YourKit, LLC is the creator of [YourKit .NET Profiler](https://www.yourkit.com/.net/profiler/index.jsp)  
and [YourKit Java Profiler](https://www.yourkit.com/java/profiler/index.jsp)  
innovative and intelligent tools for profiling .NET and Java applications.
