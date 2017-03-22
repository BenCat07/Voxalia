//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

#define MCM_TRANSP 0
#define MCM_VOX 0
#define MCM_GEOM_ACTIVE 0
#define MCM_NO_ALPHA_CAP 0
#define MCM_BRIGHT 0
#define MCM_INVERSE_FADE 0
#define MCM_FADE_DEPTH 0
#define MCM_LIGHTS 0
#define MCM_SHADOWS 0
#define MCM_NORMALS 0

#if MCM_VOX
layout (binding = 0) uniform sampler2DArray s;
#if MCM_NORMALS
layout (binding = 2) uniform sampler2DArray normal_tex;
#endif
#else
#if MCM_GEOM_ACTIVE
layout (binding = 0) uniform sampler2DArray s;
#if MCM_NORMALS
layout (binding = 1) uniform sampler2DArray normal_tex;
#endif
#else
layout (binding = 0) uniform sampler2D s;
#if MCM_NORMALS
layout (binding = 1) uniform sampler2D normal_tex;
#endif
#endif
#endif
layout (binding = 4) uniform sampler2D depth;

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
#if MCM_FADE_DEPTH
	float size;
#endif
} fi;

const int LIGHTS_MAX = 10;

// ...
layout (location = 4) uniform vec4 screen_size = vec4(1024, 1024, 0.1, 1000.0);
layout (location = 5) uniform float minimum_light = 0.2;
// ...
layout (location = 10) uniform vec3 sunlightDir = vec3(0.0, 0.0, -1.0);
layout (location = 11) uniform vec3 maximum_light = vec3(0.9, 0.9, 0.9);
layout (location = 12) uniform vec4 fogCol = vec4(0.0);
layout (location = 13) uniform float znear = 1.0;
layout (location = 14) uniform float zfar = 1000.0;
#if MCM_SHADOWS
layout (location = 20) uniform mat4 shadow_matrix_array[LIGHTS_MAX];
#endif
#if MCM_LIGHTS
layout (location = 230) uniform mat4 light_data_array[LIGHTS_MAX];
#endif

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
	if (fi.tcol.w == 0.0 && fi.tcol.x == 0.0 && fi.tcol.z == 0.0 && fi.tcol.y > 0.3 && fi.tcol.y < 0.7)
	{
		col *= fi.tcol;
	}
	else if (fi.tcol.w == 0.0 && fi.tcol.x > 0.3 && fi.tcol.x < 0.7 && fi.tcol.y > 0.3 && fi.tcol.y < 0.7 && fi.tcol.z > 0.3 && fi.tcol.z < 0.7)
	{
		if (fi.tcol.z > 0.51)
		{
			col.xyz = vec3(1.0) - col.xyz;
		}
		else if (fi.tcol.x > 0.51)
		{
			col *= fi.tcol;
		}
		else
		{
			col *= vec4(texture(s, vec3(fi.texcoord.xy, 0)).xyz, 1.0);
		}
	}
	else
	{
		col *= fi.tcol;
	}
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
	float modder = 1.0;
	modder *= dot(-fi.norm, sunlightDir);
#if MCM_NORMALS
	vec3 norm = texture(normal_tex, fi.texcoord).xyz;
#if MCM_LIGHTS
	// TODO: Lighting
#else
	modder *= dot(-norm, sunlightDir);
#endif
#endif
	color.xyz *= min(max(modder * maximum_light, max(0.2, minimum_light)), 1.0);
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
	float mod2 = min(max(0.001 / max(depthval - dist, 0.001), 0.0), 1.0);
	if (mod2 < 0.8)
	{
		discard;
	}
#endif
#if MCM_FADE_DEPTH
	float dist = linearizeDepth(gl_FragCoord.z);
	vec2 fc_xy = gl_FragCoord.xy / screen_size.xy;
	float depthval = linearizeDepth(texture(depth, fc_xy).x);
	color.w *= min(max((depthval - dist) * fi.size * 0.5 * (screen_size.w - screen_size.z), 0.0), 1.0);
#endif
}
