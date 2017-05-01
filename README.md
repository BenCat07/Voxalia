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
| Issues Open | [![Issues_Open](https://img.shields.io/github/issues/FreneticLLC/Voxalia.svg)](https://github.com/FreneticLLC/Voxalia/issues) |
| License | [![License](https://img.shields.io/badge/license-Frenetic-blue.svg)](https://github.com/FreneticLLC/Voxalia/blob/master/LICENSE.txt) |

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

- ModelToVMDConvertor -> converts any given model format (via AssImp) to VMD, the model format used by Voxalia.
- SkeletalAnimationExtractor -> converts .dae animated models to reusable anim files.
- VoxaliaServerSamplePlugin -> a sample of a C# powered plugin for the Voxalia server.

### Licensing pre-note:

- This is an open source project, provided entirely freely, for everyone to use and contribute to.
- If you make any changes that could benefit the community as a whole, please contribute upstream.
- Also, please do not claim or sell the game (or any substantial portion of it) as your own.
	- You are, of course, still free to include small snippets of code from this game in your own projects.
- Also do note that this license pertains only to the files that are code (CS, FS, VS, similar), all else is considered All Rights Reserved.

### The long version of the license follows:

Voxalia is Copyright (C) 2016-2017 Frenetic LLC.

Special LEGAL NOTICES (Human-terms sub-license):

- You may NOT remove or replace the authorization system included within Voxalia. (The system by which users may only join a non-local server if they have a Frenetic LLC authorized account!)
- You may NOT sell Voxalia without explicit written approval from Frenetic LLC!
- Any section of code subject to this license written in this file is to be considered Voxalia for the purposes of this special licensing rule.
	- Note that this means any amount of code gotten from this codebase, sufficiently complete as to be clearly recognized as a part of Voxalia, is subject to the rule of no-sales.
- ADDITIONALLY NOTE THAT this license only applies to the code contained in the primary repository, which bear a notice indicating to read this file...
	- Any data not contained by that rule (in particular, binary assets) are to be considered ALL RIGHTS RESERVED by Frenetic LLC, and may not be sold, distributed, or reused in any way outside of explicit exceptions granted by Frenetic LLC.

                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Copyright [yyyy] [name of copyright owner]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

----

----

![YourKit](https://www.yourkit.com/images/yklogo.png)

The FreneticLLC team uses YourKit .NET Profiler to improve performance. We'd like to thank them for their amazing tool and recommend them to all .NET developers!

YourKit supports open source projects with its full-featured .NET Profiler.  
YourKit, LLC is the creator of [YourKit .NET Profiler](https://www.yourkit.com/.net/profiler/index.jsp)  
and [YourKit Java Profiler](https://www.yourkit.com/java/profiler/index.jsp)  
innovative and intelligent tools for profiling .NET and Java applications.
