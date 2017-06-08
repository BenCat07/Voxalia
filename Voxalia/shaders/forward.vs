//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

// TODO: FGE version!

#define MCM_VOX 0
#define MCM_GEOM_ACTIVE 0
#define MCM_INVERSE_FADE 0
#define MCM_NO_BONES 0

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec3 texcoords;
layout (location = 3) in vec3 tangent;
layout (location = 4) in vec4 color;
#if MCM_VOX
layout (location = 5) in vec4 tcol;
layout (location = 6) in vec4 thv;
layout (location = 7) in vec4 thw;
#else
#if MCM_GEOM_ACTIVE
#else
layout (location = 5) in vec4 Weights;
layout (location = 6) in vec4 BoneID;
layout (location = 7) in vec4 Weights2;
layout (location = 8) in vec4 BoneID2;
#endif
#endif

#if MCM_GEOM_ACTIVE
out struct vox_out
#else
out struct vox_fout
#endif
{
	mat3 tbn;
#if MCM_GEOM_ACTIVE
#else
	vec3 pos;
#endif
#if MCM_VOX
	vec3 texcoord;
	vec4 tcol;
	vec4 thv;
	vec4 thw;
#else
	vec2 texcoord;
#endif
	vec4 color;
#if MCM_GEOM_ACTIVE
} f;
#else
} fi;
#endif

const int MAX_BONES = 200;

vec4 color_for(in vec4 pos, in vec4 colt);
float snoise2(in vec3 v);

#if MCM_GEOM_ACTIVE
#else
layout (location = 1) uniform mat4 proj_matrix = mat4(1.0);
#endif
layout (location = 2) uniform mat4 mv_matrix = mat4(1.0);
layout (location = 3) uniform vec4 v_color = vec4(1.0);
// ...
layout (location = 6) uniform float time;
// ...
#if MCM_VOX
#else
#if MCM_GEOM_ACTIVE
#else
layout (location = 100) uniform mat4 simplebone_matrix = mat4(1.0);
layout (location = 101) uniform mat4 boneTrans[MAX_BONES];
#endif
#endif

void main()
{
	mat4 mv_mat_simple = mv_matrix;
	mv_mat_simple[3][0] = 0.0;
	mv_mat_simple[3][1] = 0.0;
	mv_mat_simple[3][2] = 0.0;
#if MCM_VOX
	vec4 vpos = vec4(position, 1.0);
	fi.texcoord = texcoords;
	fi.thv = thv;
	fi.thw = thw;
	vec3 tf_normal = (mv_mat_simple * vec4(normal, 0.0)).xyz;
	vec3 tf_tangent = (mv_mat_simple * vec4(tangent, 0.0)).xyz;
	vec3 tf_bitangent = (mv_mat_simple * vec4(cross(tangent, normal), 0.0)).xyz;
	fi.tbn = transpose(mat3(tf_tangent, tf_bitangent, tf_normal)); // TODO: Neccessity of transpose()?
	vec4 vpos_mv = mv_matrix * vpos;
    fi.color = color_for(vpos_mv, color * v_color);
	fi.tcol = color_for(vpos_mv, tcol);
	fi.pos = vpos_mv.xyz;
	gl_Position = proj_matrix * vpos_mv;
#else // MCM_VOX
#if MCM_GEOM_ACTIVE
	f.texcoord = texcoords.xy;
	vec4 normo = mv_mat_simple * vec4(normal, 1.0);
	f.tbn = transpose(mat3(vec3(0.0), vec3(0.0), normo.xyz)); // TODO: Improve for decals?!
	f.color = color * v_color;
	gl_Position = mv_matrix * vec4(position, 1.0);
#else // MCM_GEOM_ACTIVE
	vec4 pos1;
	vec4 norm1;
	fi.texcoord = texcoords.xy;
#if MCM_NO_BONES
	const float rem = 0.0;
#else
	float rem = Weights[0] + Weights[1] + Weights[2] + Weights[3] + Weights2[0] + Weights2[1] + Weights2[2] + Weights2[3];
#endif
	if (rem > 0.01)
	{
		mat4 BT = mat4(1.0);
		BT = boneTrans[int(BoneID[0])] * Weights[0];
		BT += boneTrans[int(BoneID[1])] * Weights[1];
		BT += boneTrans[int(BoneID[2])] * Weights[2];
		BT += boneTrans[int(BoneID[3])] * Weights[3];
		BT += boneTrans[int(BoneID2[0])] * Weights2[0];
		BT += boneTrans[int(BoneID2[1])] * Weights2[1];
		BT += boneTrans[int(BoneID2[2])] * Weights2[2];
		BT += boneTrans[int(BoneID2[3])] * Weights2[3];
		BT += mat4(1.0) * (1.0 - rem);
		pos1 = vec4(position, 1.0) * BT;
		norm1 = vec4(normal, 1.0) * BT;
	}
	else
	{
		pos1 = vec4(position, 1.0);
		norm1 = vec4(normal, 1.0);
	}
	pos1 *= simplebone_matrix;
	norm1 *= simplebone_matrix;
	vec4 fnorm = mv_mat_simple * norm1;
    fi.color = color_for(mv_matrix * vec4(pos1.xyz, 1.0), color * v_color);
	vec4 posser = mv_matrix * vec4(pos1.xyz, 1.0);
	fi.pos = posser.xyz;
	gl_Position = proj_matrix * posser;
	vec3 tf_normal = (mv_mat_simple * vec4(norm1.xyz, 0.0)).xyz; // TODO: Should BT be here?
	vec3 tf_tangent = (mv_mat_simple * vec4(tangent, 0.0)).xyz; // TODO: Should BT be here?
	vec3 tf_bitangent = (mv_mat_simple * vec4(cross(tangent, norm1.xyz), 0.0)).xyz; // TODO: Should BT be here?
	fi.tbn = transpose(mat3(tf_tangent, tf_bitangent, tf_normal)); // TODO: Neccessity of transpose()?
#endif // else - MCM_GEOM_ACTIVE
#endif // else - MCM_VOX
}

const float min_cstrobe = 3.0 / 255.0;

const float min_transp = 1.0 / 255.0;

vec4 color_for(in vec4 pos, in vec4 colt)
{
	if (colt.w <= min_transp)
	{
		if (colt.x == 0.0 && colt.y == 0.0 && colt.z == 0.0)
		{
			float r = snoise2(vec3((pos.x + time) * 0.1, (pos.y + time) * 0.1, (pos.z + time) * 0.1));
			float g = snoise2(vec3((pos.x + 50.0 + time * 2) * 0.1, (pos.y + 127.0 + time * 1.7) * 0.1, (pos.z + 10.0 + time * 2.3) * 0.1));
			float b = snoise2(vec3((pos.x - 50.0 - time) * 0.1, (pos.y - 65.0 - time * 1.56) * 0.1, (pos.z + 73.0 - time * 1.3) * 0.1));
			return vec4(r, g, b, 1.0);
		}
		else if (colt.y < 0.7 && colt.y > 0.3 && colt.x == 0.0 && colt.z == 0.0)
		{
			return colt;
		}
		else if (colt.x > 0.3 && colt.x < 0.7 && colt.y > 0.3 && colt.y < 0.7 && colt.z > 0.3 && colt.z < 0.7)
		{
			return colt;
		}
		else
		{
			float adjust = abs(mod(time * 0.2, 2.0));
			if (adjust > 1.0)
			{
				adjust = 2.0 - adjust;
			}
			return vec4(colt.x * adjust, colt.y * adjust, colt.z * adjust, 1.0);
		}
	}
	else if (colt.w <= min_cstrobe)
	{
			float adjust = abs(mod(time * 0.2, 2.0));
			if (adjust > 1.0)
			{
				adjust = 2.0 - adjust;
			}
			return vec4(1.0 - colt.x * adjust, 1.0 - colt.y * adjust, 1.0 - colt.z * adjust, 1.0);
	}
	return colt;
}

#include glnoise.inc