//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

#define MCM_TRANSP 0
#define MCM_VOX 0
#define MCM_GEOM_ACTIVE 0
#define MCM_NO_ALPHA_CAP 0
#define MCM_BRIGHT 0
#define MCM_INVERSE_FADE 0

#if MCM_VOX
layout (binding = 0) uniform sampler2DArray s;
#else
#if MCM_GEOM_ACTIVE
layout (binding = 0) uniform sampler2DArray s;
#if MCM_INVERSE_FADE
layout (binding = 4) uniform sampler2D depth;
#endif
#else
layout (binding = 0) uniform sampler2D s;
#endif
#endif

// ...

in struct vox_fout
{
	vec3 norm;
#if MCM_VOX
	vec3 texcoord;
	vec4 tcol;
	vec4 thv;
	vec4 thw;
#else
#if MCM_GEOM_ACTIVE
	vec3 texcoord;
#else
	vec2 texcoord;
#endif
#endif
	vec4 color;
#if MCM_INVERSE_FADE
	float size;
#endif
} fi;

// ...
layout (location = 4) uniform vec4 screen_size = vec4(1024, 1024, 0.1, 1000.0);
layout (location = 5) uniform float minimum_light = 0.2;
// ...
layout (location = 10) uniform vec3 sunlightDir = vec3(0.0, 0.0, -1.0);
layout (location = 11) uniform vec3 maximum_light = vec3(0.9, 0.9, 0.9);
layout (location = 12) uniform vec4 fogCol = vec4(0.0);
layout (location = 13) uniform float znear = 1.0;
layout (location = 14) uniform float zfar = 1000.0;

layout (location = 0) out vec4 color;

float linearizeDepth(in float rinput) // Convert standard depth (stretched) to a linear distance (still from 0.0 to 1.0).
{
	return (2.0 * znear) / (zfar + znear - rinput * (zfar - znear));
}

void applyFog()
{
	float dist = linearizeDepth(gl_FragCoord.z);
	float fogMod = dist * exp(fogCol.w) * fogCol.w;
	float fmz = min(fogMod, 1.0);
	color.xyz = min(color.xyz * (1.0 - fmz) + fogCol.xyz * fmz + vec3(fogMod - fmz), vec3(1.0));
}

void main()
{
	vec4 col = texture(s, fi.texcoord);
#if MCM_VOX
	// TODO: Special color handlers?
	col *= fi.tcol;
#endif
#if MCM_NO_ALPHA_CAP
#else
#if MCM_TRANSP
	if (col.w * fi.color.w >= 0.99)
	{
		discard;
	}
#else
	if (col.w * fi.color.w < 0.99)
	{
		discard;
	}
#endif
#endif
	color = col * fi.color;
#if MCM_BRIGHT
#else
	// TODO: Maybe read the normal texture too, to increase "prettiness"? (Optionally, probably!)
	color.xyz *= min(max(dot(-fi.norm, sunlightDir) * maximum_light, max(0.2, minimum_light)), 1.0);
	applyFog();
#endif
	if (fogCol.w > 1.0)
	{
		applyFog();
	}
#if MCM_INVERSE_FADE
	float dist = linearizeDepth(gl_FragCoord.z);
	vec2 fc_xy = gl_FragCoord.xy / screen_size.xy;
	float depthval = linearizeDepth(texture(depth, fc_xy).x);
	float mod = min(max(0.001 / max(depthval - dist, 0.001), 0.0), 1.0);
	if (mod < 0.8)
	{
		discard;
	}
#endif
}
