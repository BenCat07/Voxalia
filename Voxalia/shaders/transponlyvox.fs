//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core
// transponlyvox.fs

#define MCM_GOOD_GRAPHICS 0
#define MCM_LIT 0
#define MCM_SHADOWS 0
#define MCM_LL 0

#define AB_SIZE 16
#define P_SIZE 4

// TODO: more dynamically defined?
#define ab_shared_pool_size (8 * 1024 * 1024)

layout (binding = 0) uniform sampler2DArray tex;
layout (binding = 1) uniform sampler2DArray htex;
layout (binding = 2) uniform sampler2DArray normal_tex;
layout (binding = 3) uniform sampler2DArray shadowtex;
#if MCM_LL
layout(size1x32, binding = 4) coherent uniform uimage2DArray ui_page;
layout(size4x32, binding = 5) coherent uniform imageBuffer uib_spage;
layout(size1x32, binding = 6) coherent uniform uimageBuffer uib_llist;
layout(size1x32, binding = 7) coherent uniform uimageBuffer uib_cspage;
#endif

in struct vox_out
{
	vec4 position;
	vec3 texcoord;
	vec4 color;
	vec4 tcol;
	mat3 tbn;
	vec2 scrpos;
	float z;
} f;

const int LIGHTS_MAX = 38; // How many lights we can ever have.

layout (location = 4) uniform float desaturationAmount = 1.0;
// ...
layout (location = 6) uniform float time;
layout (location = 7) uniform float volume;
layout (location = 8) uniform vec2 u_screensize = vec2(1024.0, 1024.0);
layout (location = 9) uniform mat4 lights_used_helper;
// ...
layout (location = 16) uniform float minimum_light;
layout (location = 17) uniform float tex_width = 256.0;
// ...
layout (location = 20) uniform mat4 shadow_matrix_array[LIGHTS_MAX];
layout (location = 58) uniform mat4 light_details_array[LIGHTS_MAX];

#if MCM_LL
#else
out vec4 fcolor;
#endif

float snoise2(in vec3 v);

vec3 desaturate(vec3 c)
{
	return mix(c, vec3(0.95, 0.77, 0.55) * dot(c, vec3(1.0)), desaturationAmount);
}

float linearizeDepth(in float rinput) // Convert standard depth (stretched) to a linear distance (still from 0.0 to 1.0).
{
	return (2.0 * lights_used_helper[0][1]) / (lights_used_helper[0][2] + lights_used_helper[0][1] - rinput * (lights_used_helper[0][2] - lights_used_helper[0][1]));
}

const int TEX_REQUIRED_BITS = (256 * 256 * 5);

const int NUM_WAVES = 9;

// TODO: (Semi-)dynamic waves?
const vec4 waves[NUM_WAVES] = vec4[NUM_WAVES](
	// XSpeed, YSpeed, Amplitude, Frequency
	vec4(0.1, 10.0, 1.0, 0.5),
	vec4(7.5, 5.0, 1.0, 0.7),
	vec4(20.4, -2.3, 4.0, 0.1),
	vec4(0.0, 0.01, 4.0, 0.3),
	vec4(-5.0, 1.3, 2.5, 0.02),
	vec4(-5.0, -7, 2.5, 0.02),
	vec4(-2.0, -12, 2.5, 0.5),
	vec4(1.2, -0.1, 2.1, 0.3),
	vec4(-12.0, 2.5, 2.1, 0.5)
);

float get_wave_height(in vec3 w_pos)
{
	vec3 f_pos = vec3(w_pos.x * (sin(time) * 0.5 + 0.5), w_pos.y * (sin(time * 0.5) * 0.5 + 0.5), w_pos.z * (sin(time * 2.0) * 0.5 + 0.5));
	// TODO: base motion off wind?
	float h = 0.0;
	for (int i = 0; i < NUM_WAVES; i++)
	{
		h += waves[i].z * sin(dot(waves[i].xyz / (waves[i].x + waves[i].y), w_pos) * waves[i].w + waves[i].w * (waves[i].x + waves[i].y) * time);
	}
	return h;// / NUM_WAVES;
}

void main()
{
	float opacity_mod = 1.0;
	int id_hint = int(f.texcoord);
	int id_tw = int(tex_width);
	int id_z = id_hint / id_tw;
	int id_xy = id_hint % id_tw;
	vec3 id_data = vec3(float(id_xy % id_tw) / float(id_tw), float(id_xy / id_tw) / float(id_tw), float(id_z));
	int tex_min = max(1, TEX_REQUIRED_BITS / (id_tw * id_tw));
#if MCM_LL
	vec4 fcolor;
#endif
	vec4 tcolor = texture(tex, f.texcoord);
	vec4 dets = texture(htex, f.texcoord);
    float spec = dets.x; // TODO: Refract / reflect?
	float rhBlur = 0.0;
	vec3 force_norm_tex = vec3(0.0);
	int do_force_norm_tex = 0;
	if (f.tcol.w == 0.0 && f.tcol.x == 0.0 && f.tcol.z == 0.0 && f.tcol.y > 0.3 && f.tcol.y < 0.7)
	{
		rhBlur = (f.tcol.y - 0.31) * ((1.0 / 0.38) * (3.14159 * 2.0));
	}
	else if (f.tcol.w == 0.0 && f.tcol.x > 0.3 && f.tcol.x < 0.7 && f.tcol.y > 0.3 && f.tcol.y < 0.7 && f.tcol.z > 0.3 && f.tcol.z < 0.7)
	{
		if (f.tcol.z > 0.51)
		{
			tcolor.xyz = vec3(1.0) - tcolor.xyz;
		}
		// TODO: color shifts effect normals, specular, etc. maps!
		else if (f.tcol.x > 0.51)
		{
			if (f.tcol.x > (152.0 / 255.0))
			{
				//reflecto = 0.5;
				spec = 1.0;
				rhBlur = 0.5;
				opacity_mod = (!(tcolor.w <= 0.99)) ? 1.0 : sqrt(length(f.position.xyz) * 0.05);
				opacity_mod = max(1.0, opacity_mod);
				float w_adder = (1.0 / tex_width);
				//float w_cc = get_wave_height(f.position.xyz);
				float w_xp = get_wave_height(f.position.xyz + f.tbn * vec3(w_adder, 0.0, 0.0));
				float w_yp = get_wave_height(f.position.xyz + f.tbn * vec3(0.0, w_adder, 0.0));
				float w_xm = get_wave_height(f.position.xyz + f.tbn * vec3(-w_adder, 0.0, 0.0));
				float w_ym = get_wave_height(f.position.xyz + f.tbn * vec3(0.0, -w_adder, 0.0));
				const float w_inv_amp = 0.5;
				vec3 w_vx = (vec3(w_inv_amp, 0.0, w_xp - w_xm));
				vec3 w_vy = (vec3(0.0, w_inv_amp, w_yp - w_ym));
				force_norm_tex = normalize(cross(w_vx, w_vy));
				do_force_norm_tex = 1;
				//float w_lmod = force_norm_tex.z;
				const float w_inv_amp2 = 0.07;
				vec3 w_vx2 = (vec3(w_inv_amp2, 0.0, w_xp - w_xm));
				vec3 w_vy2 = (vec3(0.0, w_inv_amp2, w_yp - w_ym));
				float w_lmod = normalize(cross(w_vx2, w_vy2)).z;
				tcolor.xyz *= (2.0 - w_lmod);
			}
			else if (f.tcol.x > (150.0 / 255.0))
			{
					float genNoise = snoise2(vec3(ivec3(f.position.xyz) + ivec3(time)));
					float sparkleX = mod(genNoise * 10.0, 0.8) + 0.1;
					float sparkleY = mod(genNoise * 27.3, 0.8) + 0.1;
					float intensity = mod(genNoise * 123.4, 0.5) + 0.5;
					float intensity_rel = (time - float(int(time)));
					if (intensity_rel > 0.5)
					{
						intensity_rel = (1.0 - intensity_rel);
					}
					intensity_rel *= 2.0;
					vec2 dist_xy = f.texcoord.xy - vec2(sparkleX, sparkleY);
					float intensity_dist = 20.0 * max(0.0, 0.05 - abs(dist_xy.x)) * (1.0 - 5.0 * min(0.2, abs(dist_xy.y))) + 20.0 * max(0.0, 0.05 - abs(dist_xy.y)) * (1.0 - 5.0 * min(0.2, abs(dist_xy.x)));
					tcolor = vec4(min(vec3(1.0), tcolor.xyz + vec3(intensity * intensity_rel * intensity_dist)), 0.8);
			}
			else if (f.tcol.x > (146.0 / 255.0))
			{
				if (f.tcol.x > (148.0 / 255.0))
				{
					vec2 tcfix = vec2(mod(f.texcoord.x * 3.0, 1.0), mod(f.texcoord.y * 3.0, 1.0));
					tcfix.x = tcfix.x > 1.0 ? tcfix.x - 1.0 : (tcfix.x < 0.0 ? tcfix.x + 1.0 : tcfix.x);
					tcfix.y = tcfix.y > 1.0 ? tcfix.y - 1.0 : (tcfix.y < 0.0 ? tcfix.y + 1.0 : tcfix.y);
					tcolor = texture(tex, vec3(tcfix, f.texcoord.z));
				}
				else
				{
					vec2 tcfix = vec2(mod(f.texcoord.x * 2.0, 1.0), mod(f.texcoord.y * 2.0, 1.0));
					tcfix.x = tcfix.x > 1.0 ? tcfix.x - 1.0 : (tcfix.x < 0.0 ? tcfix.x + 1.0 : tcfix.x);
					tcfix.y = tcfix.y > 1.0 ? tcfix.y - 1.0 : (tcfix.y < 0.0 ? tcfix.y + 1.0 : tcfix.y);
					tcolor = texture(tex, vec3(tcfix, f.texcoord.z));
				}
			}
			else
			{
				spec = 1.0;
				//refl = 0.75;
			}
		}
		else if (f.tcol.y > (172.0 / 255.0))
		{
			if (f.tcol.y > (174.0 / 255.0))
			{
				float newY = mod(f.texcoord.y - time * 0.5, 1.0);
				vec2 tcfix = vec2(f.texcoord.x, newY < 0.0 ? newY + 1.0 : newY);
				tcolor = texture(tex, vec3(tcfix, f.texcoord.z));
			}
			else
			{
				float newX = mod(f.texcoord.x - time * 0.5, 1.0);
				vec2 tcfix = vec2(newX < 0.0 ? newX + 1.0 : newX, f.texcoord.y);
				tcolor = texture(tex, vec3(tcfix, f.texcoord.z));
			}
		}
		else if (f.tcol.y > (168.0 / 255.0))
		{
			if (f.tcol.y > (170.0 / 255.0))
			{
				tcolor *= vec4(vec3(snoise2(f.texcoord * 10.0)), 1.0);
			}
			else
			{
				float snx = snoise2(vec3(f.texcoord.xy, volume * 2.0));
				float sny = snoise2(vec3(f.texcoord.xy, 3.1 + volume * 2.0));
				float snz = snoise2(vec3(f.texcoord.xy, 17.25 + volume * 2.0));
				tcolor *= mix(vec4(1.0), vec4(snx, sny, snz, 1.0), volume);
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
				tcolor = texture(tex, vec3(tcfx, f.texcoord.z));
			}
			else if (f.tcol.y > (164.0 / 255.0))
			{
				float rot_factorCOS = cos(time * 0.5);
				float rot_factorSIN = sin(time * 0.5);
				vec2 tcfx = vec2((f.texcoord.x - 0.5) * rot_factorCOS - (f.texcoord.y - 0.5) * rot_factorSIN, (f.texcoord.y - 0.5) * rot_factorCOS + (f.texcoord.x - 0.5) * rot_factorSIN) + vec2(0.5);
				tcfx.x = tcfx.x > 1.0 ? tcfx.x - 1.0 : (tcfx.x < 0.0 ? tcfx.x + 1.0 : tcfx.x);
				tcfx.y = tcfx.y > 1.0 ? tcfx.y - 1.0 : (tcfx.y < 0.0 ? tcfx.y + 1.0 : tcfx.y);
				tcolor = texture(tex, vec3(tcfx, f.texcoord.z));
			}
			else
			{
				const float ROT_FACTOR = 0.707106;
				vec2 tcfx = vec2((f.texcoord.x - 0.5) * ROT_FACTOR - (f.texcoord.y - 0.5) * ROT_FACTOR, (f.texcoord.y - 0.5) * ROT_FACTOR + (f.texcoord.x - 0.5) * ROT_FACTOR) + vec2(0.5);
				tcfx.x = tcfx.x > 1.0 ? tcfx.x - 1.0 : (tcfx.x < 0.0 ? tcfx.x + 1.0 : tcfx.x);
				tcfx.y = tcfx.y > 1.0 ? tcfx.y - 1.0 : (tcfx.y < 0.0 ? tcfx.y + 1.0 : tcfx.y);
				tcolor = texture(tex, vec3(tcfx, f.texcoord.z));
			}
		}
		else if (f.tcol.y > (156.0 / 255.0))
		{
			if (f.tcol.y > (160.0 / 255.0))
			{
				vec2 tcfix = vec2(f.texcoord.x, mod(f.texcoord.y + time * 0.5, 1.0));
				tcolor = texture(tex, vec3(tcfix, f.texcoord.z));
			}
			else if (f.tcol.y > (158.0 / 255.0))
			{
				vec2 tcfix = vec2(mod(f.texcoord.x + time * 0.5, 1.0), f.texcoord.y);
				tcolor = texture(tex, vec3(tcfix, f.texcoord.z));
			}
			else
			{
				float shift_x = snoise2(vec3(float(int(f.position.x)) + time * 0.075, float(int(f.position.y)) + time * 0.05, float(int(f.position.z)) + time * 0.1));
				float shift_y = snoise2(vec3(float(int(f.position.x)) + time * 0.1, float(int(f.position.y)) + time * 0.05, float(int(f.position.z)) + time * 0.075));
				vec2 tcfix = vec2(mod(f.texcoord.x + shift_x, 1.0), mod(f.texcoord.y + shift_y, 1.0));
				tcolor = texture(tex, vec3(tcfix, f.texcoord.z));
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
					tcolor = texture(tex, vec3(tcfix, f.texcoord.z));
				}
				else
				{
					tcolor *= vec4(vec3(((int(f.texcoord.x * 8.0)) % 2 != (int(f.texcoord.y * 8.0) % 2)) ? 1.0 : 0.1), 1.0);
				}
			}
			else
			{
				float shift = (f.tcol.y > (148.0 / 255.0)) ? 0.25 : (f.tcol.y > (146.0 / 255.0)) ? 0.5 : 0.75;
				tcolor *= mix(vec4(texture(tex, vec3(f.texcoord.xy, 1)).xyz, 1.0), vec4(1.0), shift);
			}
		}
		else
		{
			tcolor *= mix(vec4(texture(tex, vec3(f.texcoord.xy, 0)).xyz, 1.0), vec4(1.0), (f.tcol.x - 0.3) * 3.0);
		}
	}
	else
	{
		tcolor *= f.tcol;
	}
	if (tcolor.w * f.color.w >= 0.99)
	{
		discard;
	}
    if (tcolor.w * f.color.w < 0.01 && rhBlur == 0.0)
    {
        discard;
    }
	vec4 color = tcolor;
	fcolor = color;
	vec3 eye_rel = normalize(f.position.xyz);
	float opac_min = 0.0;
#if MCM_LIT
	fcolor = vec4(0.0);
	vec3 norms = do_force_norm_tex == 1 ? force_norm_tex : texture(normal_tex, f.texcoord).xyz * 2.0 - 1.0;
	int count = int(lights_used_helper[0][0]);
	for (int i = 0; i < count; i++)
	{
	mat4 light_details = light_details_array[i];
	mat4 shadow_matrix = shadow_matrix_array[i];
	// Loop body
	float light_radius = light_details[0][0];
	vec3 diffuse_albedo = vec3(0.7);
	vec3 specular_albedo = vec3(0.7);
	float light_type = light_details[1][3];
	float should_sqrt = light_details[2][0];
	float tex_size = light_details[2][1];
	float depth_jump = 0.5;
	float lightc = light_details[2][3];
	if (minimum_light > 0.99)
	{
		fcolor += vec4(color.xyz / lightc, color.w);
		continue;
	}
	vec4 bambient = vec4(minimum_light, minimum_light, minimum_light, 0.0) / lightc;
	vec3 eye_pos = vec3(0.0);
	vec3 light_pos = vec3(light_details[0][1], light_details[0][2], light_details[0][3]);
	float exposure = light_details[2][2];
	vec3 light_color = vec3(light_details[1][0], light_details[1][1], light_details[1][2]);
	vec4 x_spos = shadow_matrix * f.position;
	vec3 N = normalize(-(f.tbn * norms));
	vec3 light_path = light_pos - f.position.xyz;
	float light_length = length(light_path);
	float d = light_length / light_radius;
	float atten = clamp(1.0 - (d * d), 0.0, 1.0);
	if (light_type == 1.0)
	{
		vec4 fst = x_spos / x_spos.w;
		atten *= 1 - (fst.x * fst.x + fst.y * fst.y);
		if (atten < 0)
		{
			atten = 0;
		}
	}
	if (should_sqrt >= 1.0)
	{
		x_spos.x = sign(x_spos.x) * sqrt(abs(x_spos.x));
		x_spos.y = sign(x_spos.y) * sqrt(abs(x_spos.y));
	}
	vec4 fs = x_spos / x_spos.w / 2.0 + vec4(0.5, 0.5, 0.5, 0.0);
	fs.w = 1.0;
	if (fs.x < 0.0 || fs.x > 1.0
		|| fs.y < 0.0 || fs.y > 1.0
		|| fs.z < 0.0 || fs.z > 1.0)
	{
		fcolor += vec4(0.0, 0.0, 0.0, color.w);
		continue;
	}
#if MCM_SHADOWS
#if MCM_GOOD_GRAPHICS
	vec2 dz_duv;
	vec3 duvdist_dx = dFdx(fs.xyz);
	vec3 duvdist_dy = dFdy(fs.xyz);
	dz_duv.x = duvdist_dy.y * duvdist_dx.z - duvdist_dx.y * duvdist_dy.z;
	dz_duv.y = duvdist_dx.x * duvdist_dy.z - duvdist_dy.x * duvdist_dx.z;
	float tlen = (duvdist_dx.x * duvdist_dy.y) - (duvdist_dx.y * duvdist_dy.x);
	dz_duv /= tlen;
	float oneoverdj = 1.0 / depth_jump;
	float jump = tex_size * depth_jump;
	float depth = 0.0;
	float depth_count = 0.0;
	// TODO: Make me more efficient
	for (float x = -oneoverdj * 2; x < oneoverdj * 2 + 1; x++)
	{
		for (float y = -oneoverdj * 2; y < oneoverdj * 2 + 1; y++)
		{
			float offz = dot(dz_duv, vec2(x * jump, y * jump)) * 1000.0;
			if (offz > -0.000001)
			{
				offz = -0.000001;
			}
			offz -= 0.001;
			float rd = texture(shadowtex, vec3(fs.x + x * jump, -(fs.y + y * jump), float(i))).r;
			depth += (rd >= (fs.z + offz) ? 1.0 : 0.0);
			depth_count++;
		}
	}
	depth = depth / depth_count;
#else // good graphics
	float rd = texture(shadowtex, vec3(fs.x, fs.y, float(i))).r;
	float depth = (rd >= (fs.z - 0.001) ? 1.0 : 0.0);
#endif // else-good graphics
#else // shadows
	const float depth = 1.0;
#endif // else-shadows
	vec3 L = light_path / light_length;
	vec4 diffuse = vec4(max(dot(N, -L), 0.0) * diffuse_albedo, 1.0);
	float powered = pow(max(dot(reflect(L, N), eye_rel), 0.0), 128.0);
	float spec_res = max(min(powered * spec, 1.0), 0.0);
	vec3 specular = spec_res * specular_albedo;
	opac_min += spec_res;
	fcolor += vec4((bambient * color + (vec4(depth, depth, depth, 1.0) * atten * (diffuse * vec4(light_color, 1.0)) * color) + (vec4(specular, 1.0) * vec4(light_color, 1.0) * atten * depth)).xyz, color.w);
	}
#endif // lit
#if MCM_GOOD_GRAPHICS
    fcolor = vec4(desaturate(fcolor.xyz), 1.0); // TODO: Make available to all, not just good graphics only! Or a separate CVar!
#endif
	vec4 fogCol = lights_used_helper[3];
	float dist = linearizeDepth(gl_FragCoord.z);
	float fogMod = dist * exp(fogCol.w) * fogCol.w;
	float fmz = min(fogMod, 1.0);
	fcolor.xyz = fcolor.xyz * (1.0 - fmz) + fogCol.xyz * fmz + vec3(fogMod - fmz);
	fcolor = vec4(fcolor.xyz, min(tcolor.w * f.color.w * max(opacity_mod + opac_min, 0.9), 1.0));
#if MCM_LL
	uint page = 0;
	uint frag = 0;
	uint frag_mod = 0;
	ivec2 scrpos = ivec2(f.scrpos * u_screensize);
	int i = 0;
	while (imageAtomicExchange(ui_page, ivec3(scrpos, 2), 1U) != 0U && i < 100) // TODO: 100 -> uniform var?!
	{
		memoryBarrier();
		i++;
	}
	/*if (i == 100)
	{
		return;
	}*/
	page = imageLoad(ui_page, ivec3(scrpos, 0)).x;
	frag = imageLoad(ui_page, ivec3(scrpos, 1)).x;
	frag_mod = frag % P_SIZE;
	if (frag_mod == 0)
	{
		uint npage = imageAtomicAdd(uib_cspage, 0, P_SIZE);
		if (npage < ab_shared_pool_size)
		{
			imageStore(uib_llist, int(npage / P_SIZE), uvec4(page, 0U, 0U, 0U));
			imageStore(ui_page, ivec3(scrpos, 0), uvec4(npage, 0U, 0U, 0U));
			page = npage;
		}
		else
		{
			page = 0;
		}
	}
	if (page > 0)
	{
		imageStore(ui_page, ivec3(scrpos, 1), uvec4(frag + 1, 0U, 0U, 0U));
	}
	frag = frag_mod;
	memoryBarrier();
	imageAtomicExchange(ui_page, ivec3(scrpos, 2), 0U);
	vec4 abv = fcolor;
	abv.z = float(int(fcolor.z * 255) & 255 | int(fcolor.w * 255 * 255) & (255 * 255));
	abv.w = f.z;
	imageStore(uib_spage, int(page + frag), abv);
#endif
}

#include glnoise.inc
