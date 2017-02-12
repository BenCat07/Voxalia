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

## What is Voxalia

Voxalia is a really cool game that will be lots of fun to play when it's ready!

It's currently in pre-alpha, but it's already got a lot of power to it!

View the [FAQ](/FAQ.md) for more information!

## Builds Status Reports and Badges

| Build Service | Status |
| ------------- | ------ |
| AppVeyor | [![AppVeyor Build Status](https://ci.appveyor.com/api/projects/status/inbj8vbo0fx4a8io/branch/master?svg=true)](https://ci.appveyor.com/project/mcmonkey4eva/voxalia/branch/master) |
| Website ([Frenetic.XYZ](https://frenetic.xyz)) | [![Website](https://img.shields.io/website-up-down-green-red/https/frenetic.xyz.svg)](https://frenetic.xyz) |
| Issues Open | [![Issues_Open](https://img.shields.io/github/issues/FreneticXYZ/Voxalia.svg)](https://github.com/FreneticXYZ/Voxalia/issues) |
| License | [![License](https://img.shields.io/badge/license-Frenetic-blue.svg)](https://github.com/FreneticXYZ/Voxalia/blob/master/LICENSE.txt) |

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
- Also do note that this license pertains only to the files that are code (CS, FS, VS, similar), all else is considered All Rights Reserved.

### The short of the license is:

(This portion is not a legal notice, only the "long version" is!)

- You can do basically whatever you want (within reason), except you may not hold any developer liable for what you do with the software.
- You may also not sell or relicense the game without explicit permission granted by FreneticXYZ.

### "In Human Terms":

(This portion is not a legal notice, only the "long version" is!)

- The license is not complete and shouldn't be relied on or considered final.
- Anyone can do what they want with the code, excluding anything that violates the following conditions:
	- We own this software and we can sell this software. You do not own the software, nor can you sell it!
	- You also cannot change the license at all, ever (for any sufficient portion of code as to be clearly recognizable as being part of this project).
	- We can do what we want with this code, and the license for it.
	- If you copy out the code or any big piece of it, you MUST include a copy of the license!
	- There are things in here under their own licenses, both those licenses and the main license apply to those pieces if they are acquired from within here.
	- You may not hold a developer liable or at fault for anything you or anyone else does with this software.

### The long version of the license follows:

The Frenetic License (FreneticLicense)

Based upon the MIT license, though heavily modified.

Copyright (C) 2016-2017 FreneticXYZ

WARNING: This license is not yet fully reviewed and complete!
AS SUCH: we do not recommend using it!
FURTHER: for this specific version of the license,
if a future version of this license exists within reasonable
access, it is assumed to supersede this license, even for this
older copy of the Software (as defined below) containing this
early notice.
IF NO FUTURE VERSION IS AVAILABLE: The code can be assumed as ARR
("All Rights Reserved")!

Permission is hereby granted, free of charge, to any and all persons
or parties obtaining a copy of this software (IE code and any associated
documentation files, the "Software"), to deal in the Software with
minimal restriction, including without limitation the rights to use,
copy, modify, merge, publish, and/or distribute copies of the Software,
and to permit any parties or persons to whom the Software is distributed
to do so, subject to the following conditions:

A: The Software, or any substantial portion of the Software
shall not be sold, sub-licensed, or relicensed by any person or party other
than that which is named by the copyright notice above (the "Software Owner"),
as denoted by the "Copyright (C) (year)" prefix.
The software may additionally be sold or sub-licensed (but not relicensed)
by any person or party permitted to do so by the Software Owner,
as granted at the Software Owner's discretion. Further, the Software Owner
may permit, at the Softwar Owner's discretion, any seller of the Software
to themselves grant this special right to other persons or parties.

B: Any relicensing of the software is to be done and handled at the
Software Owner's own discretion and control.
No party, whether external or internal to the Software Owner's party,
shall restrict or induce this action in any way, excluding through
an official declaration from the Software Owner's party in and of
itself. (EG in the case of a single person, that person alone may perform
this action).

C: Any portions of the Software not written or created by the Software Owner,
but distributed with this license and same copyright holding notice,
shall be considered copyright to the Software Owner,
under the terms of this license, including all notices and final remarks.

D: The above copyright notice, any associated warnings and notices,
the titling for this license, and this permission notice shall all be
included in full unmodified form in all copies or substantial portions of
the Software.

E: Portions of the Software imported from elsewhere, as denoted by them
containing their own license notice within reasonable availability,
shall be considered to be primarily licensed under the license included
with said portion. HOWEVER, any usage of that portion of the software
must be kept within the terms of this license if acquired from
within this Software's distributions. To use any external software or
software portion fully and solely within its own license,
acquire that software or software portion from original distributions
dedicated to that software or software portion, or distributions that
license that software or software portion under terms that do not contain
this notice or a similar notice.
The above applies in particular to cases in which the software or
software portion in question have been modified beyond original form.

NOTE: In specific terms, one can generally safely copy less
than one file's worth of code without it being considered substantial.
Any amounts higher than that can be decided at the Software Owner's
discretion or a court's decisions. If uncertain, contact the
Software Owner first where possible.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF, OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

----

----

![YourKit](https://www.yourkit.com/images/yklogo.png)

The FreneticXYZ team uses YourKit .NET Profiler to improve performance. We'd like to thank them for their amazing tool and recommend them to all .NET developers!

YourKit supports open source projects with its full-featured .NET Profiler.  
YourKit, LLC is the creator of [YourKit .NET Profiler](https://www.yourkit.com/.net/profiler/index.jsp)  
and [YourKit Java Profiler](https://www.yourkit.com/java/profiler/index.jsp)  
innovative and intelligent tools for profiling .NET and Java applications.
