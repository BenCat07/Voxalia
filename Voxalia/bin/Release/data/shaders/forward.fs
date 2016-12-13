#version 430 core

#define MCM_TRANSP 0
#define MCM_VOX 0
#define MCM_GEOM_ACTIVE 0
#define MCM_NO_ALPHA_CAP 0
#define MCM_BRIGHT 0

#if MCM_VOX
layout (binding = 0) uniform sampler2DArray s;
#else
#if MCM_GEOM_ACTIVE
layout (binding = 0) uniform sampler2DArray s;
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
} fi;

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
#endif
	float dist = linearizeDepth(gl_FragCoord.z);
	float fogMod = dist * exp(fogCol.w) * fogCol.w;
	color.xyz = color.xyz * (1.0 - fogMod) + fogCol.xyz * fogMod;
}
