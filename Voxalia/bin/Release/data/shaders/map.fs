//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

#define MCM_VOX 0

#if MCM_VOX
layout (binding = 0) uniform sampler2DArray s;
#else
layout (binding = 0) uniform sampler2D s;
#endif

in struct vox_out
{
#if MCM_VOX
	vec3 texcoord;
#else
	vec2 texcoord;
#endif
	vec3 tcol;
} f;

layout (location = 0) out vec4 color;

void main()
{
	color = vec4(texture(s, f.texcoord).xyz * f.tcol, 1.0);
}
