//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

layout (location = 0) in vec4 screenrect;
layout (location = 1) in vec4 texcoord;
layout (location = 2) in vec4 color;
layout (location = 3) in float tcind;

layout (location = 3) uniform vec3 v_color;

layout (location = 0) out vec4 f_color;
layout (location = 1) out vec3 f_texcoord;

out struct text_out {
	vec4 color;
	vec4 texcoord;
	vec4 screenrect;
	float tcind;
} f;

void main()
{
	f.color = vec4(color.xyz * v_color.xyz, color.w);
	f.texcoord = texcoord;
	f.screenrect = screenrect;
	f.tcind = tcind;
}
