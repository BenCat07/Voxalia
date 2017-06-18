//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

#define MCM_REFRACT 0
#define MCM_TH 0

layout (binding = 0) uniform sampler2DArray s;
layout (binding = 1) uniform sampler2DArray htex;
layout (binding = 2) uniform sampler2DArray normal_tex;

layout (location = 3) uniform vec4 v_color = vec4(1.0);
// ...
layout (location = 6) uniform float time;
// ...
layout (location = 8) uniform vec2 light_clamp = vec2(0.0, 1.0);
// ...
layout (location = 16) uniform float minimum_light = 0.0;

in struct vox_out
{
	vec4 position;
	vec3 texcoord;
	vec4 color;
	vec4 tcol;
	mat3 tbn;
#if MCM_TH
	vec4 thv;
	vec4 thw;
	vec4 thv2;
	vec4 thw2;
#endif
} f;

layout (location = 0) out vec4 color;
layout (location = 1) out vec4 position;
layout (location = 2) out vec4 normal;
layout (location = 3) out vec4 renderhint;
layout (location = 4) out vec4 renderhint2;

float snoise2(in vec3 v);

void main()
{
	vec4 dets = texture(htex, f.texcoord);
#if MCM_REFRACT
	float refr_rhblur = 0.0;
	if (f.tcol.w == 0.0 && f.tcol.x == 0.0 && f.tcol.z == 0.0 && f.tcol.y > 0.3 && f.tcol.y < 0.7)
	{
		refr_rhblur = (f.tcol.y - 0.31) * ((1.0 / 0.38) * (3.14159 * 2.0));
	}
	if (dets.z > 0.01) // TODO: Use the exact refraction value?!
	{
		vec3 tnorms = f.tbn * (texture(normal_tex, f.texcoord).xyz * 2.0 - vec3(1.0));
		color = vec4(0.0);
		position = vec4(0.0);
		normal = vec4(0.0);
		if (refr_rhblur > 0.0)
		{
			renderhint = vec4(0.0, refr_rhblur, 0.0, 1.0);
		}
		else
		{
			renderhint = vec4(0.0);
		}
		renderhint2 = vec4(tnorms, 1.0);
		return;
	}
	else if (refr_rhblur > 0.0)
	{
		color = vec4(0.0);
		position = vec4(0.0);
		normal = vec4(0.0);
		renderhint = vec4(0.0, refr_rhblur, 0.0, 1.0);
		renderhint2 = vec4(0.0);
		return;
	}
	else
	{
		discard;
	}
#endif
	vec4 col = textureLod(s, f.texcoord, textureQueryLod(s, f.texcoord.xy).x);
	vec3 t_normal = texture(normal_tex, f.texcoord).xyz;
#if MCM_TH
	// Setup
	vec3 thval = vec3(0.0); // Value
	float thstr = 0.0; // Strength
	vec3 thnorm = vec3(0.0); // Normal
	vec2 th_pos = vec2(f.texcoord.x - 0.5, f.texcoord.y - 0.5);
	float sep = 1.5;
	// Self (0,0)
	float t_w = max(1.0 - (dot(th_pos, th_pos) * sep), 0.0);
	thval += col.xyz * t_w;
	thnorm += t_normal * t_w;
	thstr += t_w;
	// X+ (1,0)
	vec2 t_r = th_pos - vec2(1.0, 0.0);
	const float MULTO = 2.0;
	t_w = max((1.0 - (dot(t_r, t_r) * sep)) * f.thw.x * MULTO, 0.0);
	thval += texture(s, vec3(f.texcoord.xy, f.thv.x)).xyz * t_w;
	thnorm += texture(normal_tex, vec3(f.texcoord.xy, f.thv.x)).xyz * t_w;
	thstr += t_w;
	// X- (-1,0)
	t_r = th_pos - vec2(-1.0, 0.0);
	t_w = max((1.0 - (dot(t_r, t_r) * sep)) * f.thw.y * MULTO, 0.0);
	thval += texture(s, vec3(f.texcoord.xy, f.thv.y)).xyz * t_w;
	thnorm += texture(normal_tex, vec3(f.texcoord.xy, f.thv.y)).xyz * t_w;
	thstr += t_w;
	// Y+ (0,1)
	t_r = th_pos - vec2(0.0, 1.0);
	t_w = max((1.0 - (dot(t_r, t_r) * sep)) * f.thw.z * MULTO, 0.0);
	thval += texture(s, vec3(f.texcoord.xy, f.thv.z)).xyz * t_w;
	thnorm += texture(normal_tex, vec3(f.texcoord.xy, f.thv.z)).xyz * t_w;
	thstr += t_w;
	// Y- (0,-1)
	t_r = th_pos - vec2(0.0, -1.0);
	t_w = max((1.0 - (dot(t_r, t_r) * sep)) * f.thw.w * MULTO, 0.0);
	thval += texture(s, vec3(f.texcoord.xy, f.thv.w)).xyz * t_w;
	thnorm += texture(normal_tex, vec3(f.texcoord.xy, f.thv.w)).xyz * t_w;
	thstr += t_w;
	// X+Y+ (1,1)
	t_r = th_pos - vec2(1.0, 1.0);
	t_w = max((1.0 - (dot(t_r, t_r) * sep)) * f.thw2.x * MULTO, 0.0);
	thval += texture(s, vec3(f.texcoord.xy, f.thv2.x)).xyz * t_w;
	thnorm += texture(normal_tex, vec3(f.texcoord.xy, f.thv2.x)).xyz * t_w;
	thstr += t_w;
	// X+Y- (1,-1)
	t_r = th_pos - vec2(1.0, -1.0);
	t_w = max((1.0 - (dot(t_r, t_r) * sep)) * f.thw2.y * MULTO, 0.0);
	thval += texture(s, vec3(f.texcoord.xy, f.thv2.y)).xyz * t_w;
	thnorm += texture(normal_tex, vec3(f.texcoord.xy, f.thv2.y)).xyz * t_w;
	thstr += t_w;
	// X-Y+ (-1,1)
	t_r = th_pos - vec2(-1.0, 1.0);
	t_w = max((1.0 - (dot(t_r, t_r) * sep)) * f.thw2.z * MULTO, 0.0);
	thval += texture(s, vec3(f.texcoord.xy, f.thv2.z)).xyz * t_w;
	thnorm += texture(normal_tex, vec3(f.texcoord.xy, f.thv2.z)).xyz * t_w;
	thstr += t_w;
	// X-Y- (-1,-1)
	t_r = th_pos - vec2(-1.0, -1.0);
	t_w = max((1.0 - (dot(t_r, t_r) * sep)) * f.thw2.w * MULTO, 0.0);
	thval += texture(s, vec3(f.texcoord.xy, f.thv2.w)).xyz * t_w;
	thnorm += texture(normal_tex, vec3(f.texcoord.xy, f.thv2.w)).xyz * t_w;
	thstr += t_w;
	/*
	float trel = max(min(1.0 - thstr, 1.0), 0.0);
	thval += col.xyz * trel;
	thstr += trel;
	*/
	col.xyz = thval / thstr;
	t_normal = thnorm / thstr;
#endif
	float rhBlur = 0.0;
    float spec = dets.x;
    float refl = dets.y;
	if (f.tcol.w == 0.0 && f.tcol.x == 0.0 && f.tcol.z == 0.0 && f.tcol.y > 0.3 && f.tcol.y < 0.7)
	{
		rhBlur = (f.tcol.y - 0.31) * ((1.0 / 0.38) * (3.14159 * 2.0));
	}
	else if (f.tcol.w == 0.0 && f.tcol.x > 0.3 && f.tcol.x < 0.7 && f.tcol.y > 0.3 && f.tcol.y < 0.7 && f.tcol.z > 0.3 && f.tcol.z < 0.7)
	{
		if (f.tcol.z > 0.51)
		{
			col.xyz = vec3(1.0) - col.xyz;
		}
		else if (f.tcol.x > 0.51)
		{
			spec = 1.0;
			refl = 0.75;
		}
		// TODO: color shifts effect normals, specular, ...
		else if (f.tcol.y > (168.0 / 255.0))
		{
			if (f.tcol.y > (170.0 / 255.0))
			{
				col *= vec4(vec3(snoise2(f.texcoord * 10.0)), 1.0);
			}
			else
			{
				// TODO: Implement!
			}
		}
		else if (f.tcol.y > (162.0 / 255.0))
		{
			if (f.tcol.y > (166.0 / 255.0))
			{
				float rot_factorCOS = cos(time * 2.0 + dot(f.texcoord - 0.5, f.texcoord - 0.5) * 3.0);
				float rot_factorSIN = sin(time * 2.0 + dot(f.texcoord - 0.5, f.texcoord - 0.5) * 3.0);
				vec2 tcfx = vec2((f.texcoord.x - 0.5) * rot_factorCOS - (f.texcoord.y - 0.5) * rot_factorSIN, (f.texcoord.y - 0.5) * rot_factorCOS + (f.texcoord.x - 0.5) * rot_factorSIN) + vec2(0.5);
				tcfx.x = tcfx.x > 1.0 ? tcfx.x - 1.0 : (tcfx.x < 0.0 ? tcfx.x + 1.0 : tcfx.x);
				tcfx.y = tcfx.y > 1.0 ? tcfx.y - 1.0 : (tcfx.y < 0.0 ? tcfx.y + 1.0 : tcfx.y);
				col = texture(s, vec3(tcfx, f.texcoord.z));
			}
			else if (f.tcol.y > (164.0 / 255.0))
			{
				float rot_factorCOS = cos(time * 0.5);
				float rot_factorSIN = sin(time * 0.5);
				vec2 tcfx = vec2((f.texcoord.x - 0.5) * rot_factorCOS - (f.texcoord.y - 0.5) * rot_factorSIN, (f.texcoord.y - 0.5) * rot_factorCOS + (f.texcoord.x - 0.5) * rot_factorSIN) + vec2(0.5);
				tcfx.x = tcfx.x > 1.0 ? tcfx.x - 1.0 : (tcfx.x < 0.0 ? tcfx.x + 1.0 : tcfx.x);
				tcfx.y = tcfx.y > 1.0 ? tcfx.y - 1.0 : (tcfx.y < 0.0 ? tcfx.y + 1.0 : tcfx.y);
				col = texture(s, vec3(tcfx, f.texcoord.z));
			}
			else
			{
				const float ROT_FACTOR = 0.707106;
				vec2 tcfx = vec2((f.texcoord.x - 0.5) * ROT_FACTOR - (f.texcoord.y - 0.5) * ROT_FACTOR, (f.texcoord.y - 0.5) * ROT_FACTOR + (f.texcoord.x - 0.5) * ROT_FACTOR) + vec2(0.5);
				tcfx.x = tcfx.x > 1.0 ? tcfx.x - 1.0 : (tcfx.x < 0.0 ? tcfx.x + 1.0 : tcfx.x);
				tcfx.y = tcfx.y > 1.0 ? tcfx.y - 1.0 : (tcfx.y < 0.0 ? tcfx.y + 1.0 : tcfx.y);
				col = texture(s, vec3(tcfx, f.texcoord.z));
			}
		}
		else if (f.tcol.y > (156.0 / 255.0))
		{
			if (f.tcol.y > (160.0 / 255.0))
			{
				vec2 tcfix = vec2(f.texcoord.x, mod(f.texcoord.y + time * 0.5, 1.0));
				col = texture(s, vec3(tcfix, f.texcoord.z));
			}
			else if (f.tcol.y > (158.0 / 255.0))
			{
				vec2 tcfix = vec2(mod(f.texcoord.x + time * 0.5, 1.0), f.texcoord.y);
				col = texture(s, vec3(tcfix, f.texcoord.z));
			}
			else
			{
				float shift_x = snoise2(vec3(float(int(f.position.x)) + time * 0.075, float(int(f.position.y)) + time * 0.05, float(int(f.position.z)) + time * 0.1));
				float shift_y = snoise2(vec3(float(int(f.position.x)) + time * 0.1, float(int(f.position.y)) + time * 0.05, float(int(f.position.z)) + time * 0.075));
				vec2 tcfix = vec2(mod(f.texcoord.x + shift_x, 1.0), mod(f.texcoord.y + shift_y, 1.0));
				col = texture(s, vec3(tcfix, f.texcoord.z));
			}
		}
		else if (f.tcol.y > 0.51)
		{
			if (f.tcol.y > (150.0 / 255.0))
			{
				if (f.tcol.y > (152.0 / 255.0))
				{
					float res_fix = (f.tcol.y > (154.0 / 255.0) ? 8 : 32);
					vec2 tcfix = vec2(float(int(f.texcoord.x * res_fix)) / res_fix, float(int(f.texcoord.y * res_fix)) / res_fix);
					col = texture(s, vec3(tcfix, f.texcoord.z));
				}
				else
				{
					col *= vec4(vec3(((int(f.texcoord.x * 8.0)) % 2 != (int(f.texcoord.y * 8.0) % 2)) ? 1.0 : 0.1), 1.0);
				}
			}
			else
			{
				float shift = (f.tcol.y > (148.0 / 255.0)) ? 0.25 : (f.tcol.y > (146.0 / 255.0)) ? 0.5 : 0.75;
				col *= mix(vec4(texture(s, vec3(f.texcoord.xy, 1)).xyz, 1.0), vec4(1.0), shift);
			}
		}
		else
		{
			col *= mix(vec4(texture(s, vec3(f.texcoord.xy, 0)).xyz, 1.0), vec4(1.0), (f.tcol.x - 0.3) * 3.0);
		}
	}
	else
	{
		col *= f.tcol;
	}
	if (col.w * v_color.w < 0.99)
	{
		discard;
	}
	vec3 lightcol = f.color.xyz;
	vec3 norms = t_normal * 2.0 - vec3(1.0);
	color = col * v_color;
	position = vec4(f.position.xyz, 1.0);
	normal = vec4(normalize(f.tbn * norms), 1.0);
	float light_min = clamp(minimum_light + dets.a, 0.0, 1.0);
	color = vec4(color.xyz * lightcol, color.w);
	renderhint = vec4(spec, rhBlur, light_min, 1.0);
	renderhint2 = vec4(0.0, refl, 0.0, 1.0);
}

#include glnoise.inc
