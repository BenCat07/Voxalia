//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

// TODO: FGE version!

#define MCM_TRANSP 0
#define MCM_GEOM_ACTIVE 0
#define MCM_NO_ALPHA_CAP 0
#define MCM_BRIGHT 0
#define MCM_INVERSE_FADE 0
#define MCM_FADE_DEPTH 0
#define MCM_LIGHTS 0
#define MCM_SHADOWS 0
#define MCM_TH 0
#define MCM_SKY_FOG 0
#define MCM_ANTI_TRANSP 0
#define MCM_SIMPLE_LIGHT 0
#define MCM_SLOD_LIGHT 0
#define MCM_SPECIAL_FOG 0

layout (binding = 0) uniform sampler2DArray s;
#if MCM_LIGHTS
layout (binding = 1) uniform sampler2DArray htex;
#endif
layout (binding = 2) uniform sampler2DArray normal_tex;
layout (binding = 4) uniform sampler2D depth;
layout (binding = 5) uniform sampler2DArray shadowtex;
// ...

in struct vox_fout
{
	mat3 tbn;
	vec3 pos;
	vec3 texcoord;
	vec4 tcol;
	vec4 thv;
	vec4 thw;
	// TODO: thv2, thw2
	vec4 color;
#if MCM_INVERSE_FADE
	float size;
#endif
#if MCM_FADE_DEPTH
	float size;
#endif
} fi;

const int LIGHTS_MAX = 38;

// ...
layout (location = 4) uniform vec4 screen_size = vec4(1024, 1024, 0.1, 1000.0);
// ...
layout (location = 6) uniform float time;
layout (location = 7) uniform float volume;
layout (location = 8) uniform float tex_wid = 1.0;
// ...
layout (location = 10) uniform vec3 sunlightDir = vec3(0.0, 0.0, -1.0);
layout (location = 11) uniform vec3 maximum_light = vec3(0.9, 0.9, 0.9);
layout (location = 12) uniform vec4 fogCol = vec4(0.0);
layout (location = 13) uniform float fogDist = 1.0 / 100000.0;
layout (location = 14) uniform vec2 zdist = vec2(1.0, 1000.0);
layout (location = 15) uniform float lights_used = 0.0;
layout (location = 16) uniform float minimum_light = 0.2;
#if MCM_LIGHTS
layout (location = 20) uniform mat4 shadow_matrix_array[LIGHTS_MAX];
layout (location = 58) uniform mat4 light_data_array[LIGHTS_MAX];
#endif

layout (location = 0) out vec4 color;
layout (location = 1) out vec4 position;
layout (location = 2) out vec4 nrml;
// ...
layout (location = 4) out vec4 renderhint2;

float snoise2(in vec3 v);

vec4 unused_nonsense() // Prevent shader compiler from claiming variables are unused (Even if they /are/ unused!)
{
	return screen_size + fogCol + vec4(zdist, zdist);
}

float linearizeDepth(in float rinput) // Convert standard depth (stretched) to a linear distance (still from 0.0 to 1.0).
{
	return (2.0 * zdist.x) / (zdist.y + zdist.x - rinput * (zdist.y - zdist.x));
}

void applyFog()
{
#if MCM_SKY_FOG
	float fmza = 1.0 - max(min((fi.pos.z - 1000.0) / 2000.0, 1.0), 0.0);
	color.xyz = min(color.xyz * (1.0 - fmza) + fogCol.xyz * fmza, vec3(1.0));
#endif
#if MCM_BRIGHT
	if (fogCol.w > 1.0)
#endif
	{
		float dist = pow(dot(fi.pos, fi.pos) * fogDist, 0.6);
		float fogMod = dist * exp(fogCol.w) * fogCol.w;
		float fmz = min(fogMod, 1.0);
#if MCM_SPECIAL_FOG
		fmz *= fmz * fmz * fmz;
#endif
		color.xyz = min(color.xyz * (1.0 - fmz) + fogCol.xyz * fmz, vec3(1.0));
	}
}

float fix_sqr(in float inTemp)
{
	return 1.0 - (inTemp * inTemp);
}

vec4 read_texture(in sampler2DArray samp_in, in vec3 texcrd)
{
	return textureLod(samp_in, texcrd, textureQueryLod(samp_in, fi.texcoord.xy).x);
	//return texture(samp_in, texcrd);
}

void main()
{
	position = vec4(fi.pos, 1.0);
	vec4 col = read_texture(s, fi.texcoord);
	float extra_specular = 0.0;
	float rhBlur = 0.0;
	float reflecto = 0.0;
	if (fi.tcol.w == 0.0 && fi.tcol.x == 0.0 && fi.tcol.z == 0.0 && fi.tcol.y > 0.3 && fi.tcol.y < 0.7)
	{
		rhBlur = (fi.tcol.y - 0.31) * ((1.0 / 0.38) * (3.14159 * 2.0));
	}
	else if (fi.tcol.w == 0.0 && fi.tcol.x == 0.0 && fi.tcol.z == 0.0 && fi.tcol.y > 0.3 && fi.tcol.y < 0.7)
	{
		col *= fi.tcol;
	}
	else if (fi.tcol.w == 0.0 && fi.tcol.x > 0.3 && fi.tcol.x < 0.7 && fi.tcol.y > 0.3 && fi.tcol.y < 0.7 && fi.tcol.z > 0.3 && fi.tcol.z < 0.7)
	{
		if (fi.tcol.z > 0.51)
		{
			col.xyz = vec3(1.0) - col.xyz;
		}
		// TODO: color shifts effect normals, specular, ...
		else if (fi.tcol.x > 0.51)
		{
			if (fi.tcol.x > (146.0 / 255.0))
			{
				if (fi.tcol.x > (150.0 / 255.0))
				{
					float genNoise = snoise2(vec3(ivec3(fi.pos) + ivec3(time)));
					float sparkleX = mod(genNoise * 10.0, 0.8) + 0.1;
					float sparkleY = mod(genNoise * 27.3, 0.8) + 0.1;
					float intensity = mod(genNoise * 123.4, 0.5) + 0.5;
					float intensity_rel = (time - float(int(time)));
					if (intensity_rel > 0.5)
					{
						intensity_rel = (1.0 - intensity_rel);
					}
					intensity_rel *= 2.0;
					vec2 dist_xy = fi.texcoord.xy - vec2(sparkleX, sparkleY);
					float intensity_dist = 20.0 * max(0.0, 0.05 - abs(dist_xy.x)) * (1.0 - 5.0 * min(0.2, abs(dist_xy.y))) + 20.0 * max(0.0, 0.05 - abs(dist_xy.y)) * (1.0 - 5.0 * min(0.2, abs(dist_xy.x)));
					col = vec4(min(vec3(1.0), col.xyz + vec3(intensity * intensity_rel * intensity_dist)), 1.0);
				}
				else if (fi.tcol.x > (148.0 / 255.0))
				{
					vec2 tcfix = vec2(mod(fi.texcoord.x * 3.0, 1.0), mod(fi.texcoord.y * 3.0, 1.0));
					tcfix.x = tcfix.x > 1.0 ? tcfix.x - 1.0 : (tcfix.x < 0.0 ? tcfix.x + 1.0 : tcfix.x);
					tcfix.y = tcfix.y > 1.0 ? tcfix.y - 1.0 : (tcfix.y < 0.0 ? tcfix.y + 1.0 : tcfix.y);
					col = read_texture(s, vec3(tcfix, fi.texcoord.z));
				}
				else
				{
					vec2 tcfix = vec2(mod(fi.texcoord.x * 2.0, 1.0), mod(fi.texcoord.y * 2.0, 1.0));
					tcfix.x = tcfix.x > 1.0 ? tcfix.x - 1.0 : (tcfix.x < 0.0 ? tcfix.x + 1.0 : tcfix.x);
					tcfix.y = tcfix.y > 1.0 ? tcfix.y - 1.0 : (tcfix.y < 0.0 ? tcfix.y + 1.0 : tcfix.y);
					col = read_texture(s, vec3(tcfix, fi.texcoord.z));
				}
			}
			else
			{
				reflecto = 0.75;
				extra_specular = 1.0;
			}
		}
		else if (fi.tcol.y > (172.0 / 255.0))
		{
			if (fi.tcol.y > (174.0 / 255.0))
			{
				float newY = mod(fi.texcoord.y - time * 0.5, 1.0);
				vec2 tcfix = vec2(fi.texcoord.x, newY < 0.0 ? newY + 1.0 : newY);
				col = read_texture(s, vec3(tcfix, fi.texcoord.z));
			}
			else
			{
				float newX = mod(fi.texcoord.x - time * 0.5, 1.0);
				vec2 tcfix = vec2(newX < 0.0 ? newX + 1.0 : newX, fi.texcoord.y);
				col = read_texture(s, vec3(tcfix, fi.texcoord.z));
			}
		}
		else if (fi.tcol.y > (168.0 / 255.0))
		{
			if (fi.tcol.y > (170.0 / 255.0))
			{
				col *= vec4(vec3(snoise2(fi.texcoord * 10.0)), 1.0);
			}
			else
			{
				float snx = snoise2(vec3(fi.texcoord.xy, volume * 2.0));
				float sny = snoise2(vec3(fi.texcoord.xy, 3.1 + volume * 2.0));
				float snz = snoise2(vec3(fi.texcoord.xy, 17.25 + volume * 2.0));
				col *= mix(vec4(1.0), vec4(snx, sny, snz, 1.0), volume);
			}
		}
		else if (fi.tcol.y > (162.0 / 255.0))
		{
			if (fi.tcol.y > (166.0 / 255.0))
			{
				float rot_factorCOS = cos(time * 2.0 + dot(fi.texcoord - 0.5, fi.texcoord - 0.5) * 3.0);
				float rot_factorSIN = sin(time * 2.0 + dot(fi.texcoord - 0.5, fi.texcoord - 0.5) * 3.0);
				vec2 tcfix = vec2((fi.texcoord.x - 0.5) * rot_factorCOS - (fi.texcoord.y - 0.5) * rot_factorSIN, (fi.texcoord.y - 0.5) * rot_factorCOS + (fi.texcoord.x - 0.5) * rot_factorSIN) + vec2(0.5);
				tcfix.x = tcfix.x > 1.0 ? tcfix.x - 1.0 : (tcfix.x < 0.0 ? tcfix.x + 1.0 : tcfix.x);
				tcfix.y = tcfix.y > 1.0 ? tcfix.y - 1.0 : (tcfix.y < 0.0 ? tcfix.y + 1.0 : tcfix.y);
				col = read_texture(s, vec3(tcfix, fi.texcoord.z));
			}
			else if (fi.tcol.y > (164.0 / 255.0))
			{
				float rot_factorCOS = cos(time * 0.5);
				float rot_factorSIN = sin(time * 0.5);
				vec2 tcfix = vec2((fi.texcoord.x - 0.5) * rot_factorCOS - (fi.texcoord.y - 0.5) * rot_factorSIN, (fi.texcoord.y - 0.5) * rot_factorCOS + (fi.texcoord.x - 0.5) * rot_factorSIN) + vec2(0.5);
				tcfix.x = tcfix.x > 1.0 ? tcfix.x - 1.0 : (tcfix.x < 0.0 ? tcfix.x + 1.0 : tcfix.x);
				tcfix.y = tcfix.y > 1.0 ? tcfix.y - 1.0 : (tcfix.y < 0.0 ? tcfix.y + 1.0 : tcfix.y);
				col = read_texture(s, vec3(tcfix, fi.texcoord.z));
			}
			else
			{
				const float ROT_FACTOR = 0.707106;
				vec2 tcfix = vec2((fi.texcoord.x - 0.5) * ROT_FACTOR - (fi.texcoord.y - 0.5) * ROT_FACTOR, (fi.texcoord.y - 0.5) * ROT_FACTOR + (fi.texcoord.x - 0.5) * ROT_FACTOR) + vec2(0.5);
				tcfix.x = tcfix.x > 1.0 ? tcfix.x - 1.0 : (tcfix.x < 0.0 ? tcfix.x + 1.0 : tcfix.x);
				tcfix.y = tcfix.y > 1.0 ? tcfix.y - 1.0 : (tcfix.y < 0.0 ? tcfix.y + 1.0 : tcfix.y);
				col = read_texture(s, vec3(tcfix, fi.texcoord.z));
			}
		}
		else if (fi.tcol.y > (156.0 / 255.0))
		{
			if (fi.tcol.y > (160.0 / 255.0))
			{
				vec2 tcfix = vec2(fi.texcoord.x, mod(fi.texcoord.y + time * 0.5, 1.0));
				col = read_texture(s, vec3(tcfix, fi.texcoord.z));
			}
			else if (fi.tcol.y > (158.0 / 255.0))
			{
				vec2 tcfix = vec2(mod(fi.texcoord.x + time * 0.5, 1.0), fi.texcoord.y);
				col = read_texture(s, vec3(tcfix, fi.texcoord.z));
			}
			else
			{
				float shift_x = snoise2(vec3(float(int(fi.pos.x)) + time * 0.075, float(int(fi.pos.y)) + time * 0.05, float(int(fi.pos.z)) + time * 0.1));
				float shift_y = snoise2(vec3(float(int(fi.pos.x)) + time * 0.1, float(int(fi.pos.y)) + time * 0.05, float(int(fi.pos.z)) + time * 0.075));
				vec2 tcfix = vec2(mod(fi.texcoord.x + shift_x, 1.0), mod(fi.texcoord.y + shift_y, 1.0));
				col = read_texture(s, vec3(tcfix, fi.texcoord.z));
			}
		}
		else if (fi.tcol.y > 0.51)
		{
			if (fi.tcol.y > (150.0 / 255.0))
			{
				if (fi.tcol.y > (152.0 / 255.0))
				{
					float res_fix = (fi.tcol.y > (154.0 / 255.0) ? 8 : 32);
					vec2 tcfix = vec2(float(int(fi.texcoord.x * res_fix)) / res_fix, float(int(fi.texcoord.y * res_fix)) / res_fix);
					col = read_texture(s, vec3(tcfix, fi.texcoord.z));
				}
				else
				{
					col *= vec4(vec3(((int(fi.texcoord.x * 8.0)) % 2 != (int(fi.texcoord.y * 8.0) % 2)) ? 1.0 : 0.1), 1.0);
				}
			}
			else
			{
				float shift = (fi.tcol.y > (148.0 / 255.0)) ? 0.25 : (fi.tcol.y > (146.0 / 255.0)) ? 0.5 : 0.75;
				col *= mix(vec4(read_texture(s, vec3(fi.texcoord.xy, 1)).xyz, 1.0), vec4(1.0), shift);
			}
		}
		else
		{
			col *= mix(vec4(read_texture(s, vec3(fi.texcoord.xy, 0)).xyz, 1.0), vec4(1.0), (fi.tcol.x - 0.3) * 3.0);
		}
	}
	else
	{
		col *= fi.tcol;
	}
	// TODO: Th's effect spec, normal
	vec2 tc_h = vec2(fi.texcoord.x / tex_wid - 0.5, fi.texcoord.y / tex_wid - 0.5);
	const float multo = 0.75;
	vec3 tempCol = vec3(0.0);
	// X+
	float xpMulto = max(0.0, 1.0 - dot(tc_h - vec2(1.0, 0.0), tc_h - vec2(1.0, 0.0)));
	tempCol += xpMulto * multo * fi.thw.x * read_texture(s, vec3(fi.texcoord.xy, fi.thv.x)).xyz;
	// X-
	float xmMulto = max(0.0, 1.0 - dot(tc_h - vec2(-1.0, 0.0), tc_h - vec2(-1.0, 0.0)));
	tempCol += xmMulto * multo * fi.thw.y * read_texture(s, vec3(fi.texcoord.xy, fi.thv.y)).xyz;
	// Y+
	float ypMulto = max(0.0, 1.0 - dot(tc_h - vec2(0.0, 1.0), tc_h - vec2(0.0, 1.0)));
	tempCol += ypMulto * multo * fi.thw.z * read_texture(s, vec3(fi.texcoord.xy, fi.thv.z)).xyz;
	// Y-
	float ymMulto = max(0.0, 1.0 - dot(tc_h - vec2(0.0, -1.0), tc_h - vec2(0.0, -1.0)));
	tempCol += ymMulto * multo * fi.thw.w * read_texture(s, vec3(fi.texcoord.xy, fi.thv.w)).xyz;
	float influ = max(0.0, xpMulto * multo * fi.thw.x + xmMulto * multo * fi.thw.y + ypMulto * multo * fi.thw.z + ymMulto * multo * fi.thw.w);
	float influUse = min(1.0, influ);
	tempCol = col.xyz * (1.0 - influUse) + tempCol;
	float thStr = influ + (1.0 - influUse);
	col.xyz = tempCol / thStr;
#if MCM_LIGHTS
	vec4 hintter = read_texture(htex, fi.texcoord);
	float specularStrength = max(hintter.x, extra_specular);
#if MCM_TRANSP
#else // MCM_TRANSP
	reflecto = max(reflecto, hintter.z);
	renderhint2 = vec4(0.0, reflecto, 0.0, 1.0);
#endif // else - MCM_TRANSP
#endif // MCM_LIGHTS
	float rhb_fx = 1.0;
	if (rhBlur > 0.0 && col.w < 0.99)
	{
		rhb_fx = sqrt(length(fi.pos.xyz) * 0.05);
	}
#if MCM_NO_ALPHA_CAP
	if (col.w * fi.color.w * rhb_fx <= 0.01)
	{
		discard;
	}
#if MCM_ANTI_TRANSP
	col.w = 1.0;
#endif
#else // MCM_NO_ALPHA_CAP
#if MCM_TRANSP
	if (col.w * fi.color.w * rhb_fx >= 0.99)
	{
		discard;
	}
#else // MCM_TRANSP
	if (col.w * fi.color.w * rhb_fx < 0.99)
	{
		discard;
	}
#endif // else - MCM_TRANPS
#endif // ELSE - MCM_NO_ALPHA_CAP
	color = col * fi.color;
#if MCM_BRIGHT
#else // MCM_BRIGHT
	float opac_min = 0.0;
	vec3 norms = read_texture(normal_tex, fi.texcoord).xyz * 2.0 - vec3(1.0);
	vec3 tf_normal = normalize(fi.tbn * norms);
	nrml = vec4(tf_normal, 1.0);
#if MCM_LIGHTS
	vec3 res_color = vec3(0.0);
	int count = int(lights_used);
	for (int i = 0; i < count; i++)
	{
		mat4 light_data = light_data_array[i];
		mat4 shadow_matrix = shadow_matrix_array[i];
		// Light data.
		vec3 light_pos = vec3(light_data[0][0], light_data[0][1], light_data[0][2]); // The position of the light source.
		float diffuse_albedo = light_data[0][3]; // The diffuse albedo of this light (diffuse light is multiplied directly by this).
		float specular_albedo = light_data[1][0]; // The specular albedo (specular power is multiplied directly by this).
		float should_sqrt = light_data[1][1]; // 0 to not use square-root trick, 1 to use it (see implementation for details).
		vec3 light_color = vec3(light_data[1][2], light_data[1][3], light_data[2][0]); // The color of the light.
		float light_radius = light_data[2][1]; // The maximum radius of the light.
		vec3 eye_pos = vec3(light_data[2][2], light_data[2][3], light_data[3][0]); // The position of the camera eye.
		float light_type = light_data[3][1]; // What type of light this is: 0 is standard (point, sky, etc.), 1 is conical (spot light).
		int is_point = light_type >= 1.5 ? 1 : 0;
		float tex_size = light_data[3][2]; // If shadows are enabled, this is the inverse of the texture size of the shadow map.
		// float unused = light_data[3][3];
		vec4 f_spos = is_point == 1 ? vec4(0.0, 0.0, 0.0, 1.0) : shadow_matrix * vec4(fi.pos, 1.0); // Calculate the position of the light relative to the view.
		f_spos /= f_spos.w; // Standard perspective divide.
		vec3 light_path = light_pos - fi.pos; // What path a light ray has to travel down in theory to get from the source to the current pixel.
		float light_length = length(light_path); // How far the light is from this pixel.
		float d = light_length / light_radius; // How far the pixel is from the end of the light.
		float atten = clamp(1.0 - (d * d), 0.0, 1.0); // How weak the light is here, based purely on distance so far.
		if (is_point == 0 && light_type >= 0.5) // If this is a conical (spot light)...
		{
			atten *= 1.0 - (f_spos.x * f_spos.x + f_spos.y * f_spos.y); // Weaken the light based on how far towards the edge of the cone/circle it is. Bright in the center, dark in the corners.
		}
		if (atten <= 0.0) // If light is really weak...
		{
			continue; // Forget this light, move on already!
		}
		if (should_sqrt >= 0.5) // If inverse square trick is enabled (generally this will be 1.0 or 0.0)
		{
			f_spos.x = sign(f_spos.x) * fix_sqr(1.0 - abs(f_spos.x)); // Inverse square the relative position while preserving the sign. Shadow creation buffer also did this.
			f_spos.y = sign(f_spos.y) * fix_sqr(1.0 - abs(f_spos.y)); // This section means that coordinates near the center of the light view will have more pixels per area available than coordinates far from the center.
		}
#if MCM_SIMPLE_LIGHT
		const float depth = 1.0;
#else
		vec3 fs = vec3(0.0);
		if (is_point == 0)
		{
			// Create a variable representing the proper screen/texture coordinate of the shadow view (ranging from 0 to 1 instead of -1 to 1).
			fs = f_spos.xyz * 0.5 + vec3(0.5, 0.5, 0.5); 
			if (fs.x < 0.0 || fs.x > 1.0
				|| fs.y < 0.0 || fs.y > 1.0
				|| fs.z < 0.0 || fs.z > 1.0) // If any coordinate is outside view range...
			{
				continue; // We can't light it! Discard straight away!
			}
		}
		// TODO: maybe HD well blurred shadows?
#if MCM_SHADOWS
		float shadowID = float(i);
		float mdX = 1.0, mdY = 1.0, rdX = 0.0, rdY = 0.0;
		if (i >= 10)
		{
			shadowID = float((i - 10) / 4);
			int ltCO = (i - 10) % 4;
			rdY = float(ltCO / 2) * 0.5;
			rdX = float(ltCO % 2) * 0.5;
			mdX = 0.5;
			mdY = 0.5;
		}
#if 1 // TODO: MCM_SHADOW_BLURRING?
		float depth = 1.0;
		if (is_point == 0)
		{
			int loops = 0;
			for (float x = -1.0; x <= 1.0; x += 0.5)
			{
				for (float y = -1.0; y <= 1.0; y += 0.5)
				{
					loops++;
					float rd = texture(shadowtex, vec3((fs.x + x * tex_size) * mdX + rdX, (fs.y + y * tex_size) * mdY + rdY, shadowID)).r; // Calculate the depth of the pixel.
					depth += (rd >= (fs.z - 0.001) ? 1.0 : 0.0);
				}
			}
			depth /= loops;
		}
#else
		float depth = 1.0;
		if (is_point == 0)
		{
			float rd = texture(shadowtex, vec3(fs.x * mdX + rdX, fs.y * mdY + rdY, shadowID)).r; // Calculate the depth of the pixel.
			depth = (rd >= (fs.z - 0.001) ? 1.0 : 0.0); // If we have a bad graphics card, just quickly get a 0 or 1 depth value. This will be pixelated (hard) shadows!
		}
#endif
		if (depth <= 0.0)
		{
			continue;
		}
#else
		const float depth = 1.0;
#endif
#endif
		vec3 L = light_path / light_length; // Get the light's movement direction as a vector
		vec3 diffuse = max(dot(tf_normal, L), 0.0) * vec3(diffuse_albedo); // Find out how much diffuse light to apply
		vec3 reller = normalize(fi.pos - eye_pos);
		float spec_res = pow(max(dot(reflect(L, -tf_normal), reller), 0.0), 200.0) * specular_albedo * specularStrength;
		spec_res = max(0.0, spec_res);
		opac_min += spec_res;
		vec3 specular = vec3(spec_res); // Find out how much specular light to apply.
		res_color += (vec3(depth, depth, depth) * atten * (diffuse * light_color) * color.xyz) + (min(specular, 1.0) * light_color * atten * depth); // Put it all together now.
	}
	color.xyz = min(res_color * (1.0 - max(0.2, minimum_light)) + color.xyz * max(0.2, minimum_light), vec3(1.0));
#else // MCM_LIGHTS
	float dotted = dot(-tf_normal, sunlightDir);
	dotted = dotted <= 0.0 ? 0.0 : dotted;
#if MCM_SLOD_LIGHT
	dotted = dotted * 0.5 + 0.5;
#endif // MCM_SLOD_LIGHT
	color.xyz *= min(max(dotted * maximum_light, max(0.2, minimum_light)), 1.0) * 0.75;
#endif // else - MCM_LIGHTS
	applyFog();
#endif // else - MCM_BRIGHT
	color.w *= rhb_fx + opac_min;
	color.w = min(color.w, 1.0);
	applyFog();
#if MCM_INVERSE_FADE
	float dist = linearizeDepth(gl_FragCoord.z);
	vec2 fc_xy = gl_FragCoord.xy / screen_size.xy;
	float depthval = linearizeDepth(texture(depth, fc_xy).x);
	float mod2 = min(max(0.001 / max(depthval - dist, 0.001), 0.0), 1.0);
	if (mod2 < 0.8)
	{
		discard;
	}
#endif // MCM_INVERSE_FADE
#if MCM_FADE_DEPTH
	float dist = linearizeDepth(gl_FragCoord.z);
	vec2 fc_xy = gl_FragCoord.xy / screen_size.xy;
	float depthval = linearizeDepth(texture(depth, fc_xy).x);
	color.w *= min(max((depthval - dist) * fi.size * 0.5 * (screen_size.w - screen_size.z), 0.0), 1.0);
#endif // MCM_FADE_DEPTH
}

#include glnoise.inc
