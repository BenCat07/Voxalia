Voxalia
-------

**Voxalia, a game about blocks and such.**

Built using OpenTK (a C# binding of OpenGL for the 3D rendering), BEPUphysics (a capable pure-C# physics engine), and FreneticScript (powerful and adaptive game modding).

Also used libraries:

- Open.NAT (automated port forwarding)
- Opus (network-ready voice compression)
- csogg/csvorbis (.ogg audio file handling)
- LiteDB (save file compression)
- FreneticDataSyntax (used for FreneticScript data handling and some files in Voxalia's save structure)
- lz4net (general data compression)

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

- Open the `Voxalia.sln` file in Microsoft Visual Studio 2015
- Switch the configuration to `Release|x64`
- Build -> Build Solution!

## Compiling on Linux/Mac

- open the `Voxalia.sln` file in MonoDevelop
- Switch the configuration to `Release (Linux)|x64`
- Build as per normal
- Be aware that some things may not function properly!
	- If you are a Linux/Mac|mono|C# developer, consider trying to improve what you can and sending it back through a Github pull request to FreneticXYZ!

## Playing

- Please CONTACT US if you wish to play the game at this time.
	- The game is not publicly available to play currently!

## Also Included

- ModelToVMDConvertor -> converts any given model format (via AssImp) to VMD, the model format used by Voxalia.
- SkeletalAnimationExtractor -> converts .dae animated models to reusable anim files.
- VoxaliaServerSamplePlugin -> a sample of a C# powered plugin for the Voxalia server.

### Licensing pre-note:

- This is an open source project, provided entirely freely, for everyone to use and contribute to.
- If you make any changes that could benefit the community as a whole, please contribute upstream.
- Also, please do not claim or sell the game (or any substantial portion of it) as your own.
	- You are, of course, still free to include small snippets of code from this game in your own projects.

### The short of the license is:

- You can do basically whatever you want (within reason), except you may not hold any developer liable for what you do with the software.

### The long version of the license follows:

The MIT License (MIT)

Copyright (c) 2016 FreneticXYZ

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

----

----

![YourKit](https://www.yourkit.com/images/yklogo.png)

The FreneticXYZ team uses YourKit .NET Profiler to improve performance. We'd like to thank them for their amazing tool and recommend them to all .NET developers!

YourKit supports open source projects with its full-featured .NET Profiler.  
YourKit, LLC is the creator of [YourKit .NET Profiler](https://www.yourkit.com/.net/profiler/index.jsp)  
and [YourKit Java Profiler](https://www.yourkit.com/java/profiler/index.jsp)  
innovative and intelligent tools for profiling .NET and Java applications.
