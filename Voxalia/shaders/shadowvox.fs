//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

layout (binding = 0) uniform sampler2DArray s;

layout (location = 4) uniform float allow_transp = 0.0;

layout (location = 0) in vec4 f_pos;
layout (location = 1) in vec3 f_texcoord;
layout (location = 2) in vec4 f_color;

layout (location = 0) out float color;

void main()
{
	vec4 col = texture(s, f_texcoord) * f_color;
	if (col.w < 0.9 && ((col.w < 0.05) || (allow_transp <= 0.5)))
	{
		discard;
	}
	color = gl_FragCoord.z;
}
