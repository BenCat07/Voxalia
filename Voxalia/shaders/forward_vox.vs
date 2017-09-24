//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

// TODO: FGE version!

#define MCM_GEOM_ACTIVE 0
#define MCM_INVERSE_FADE 0
#define MCM_NO_BONES 0
#define MCM_TH 0
#define MCM_GEOM_THREED_TEXTURE 0

layout (location = 0) in vec4 position;
layout (location = 1) in vec4 normal;
layout (location = 2) in vec4 texcoords;
layout (location = 3) in vec4 tangent;
layout (location = 4) in vec4 color;
layout (location = 5) in vec4 tcol;
#if MCM_TH
layout (location = 6) in vec4 thv;
layout (location = 7) in vec4 thw;
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
	vec3 texcoord;
	vec4 tcol;
	vec4 thv;
	vec4 thw;
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

void main()
{
	mat4 mv_mat_simple = mv_matrix;
	mv_mat_simple[3][0] = 0.0;
	mv_mat_simple[3][1] = 0.0;
	mv_mat_simple[3][2] = 0.0;
	vec4 vpos = vec4(position.xyz, 1.0);
	fi.texcoord = texcoords.xyz;
	vec3 tf_normal = (mv_mat_simple * vec4(normal.xyz, 0.0)).xyz;
	vec3 tf_tangent = (mv_mat_simple * vec4(tangent.xyz, 0.0)).xyz;
	vec3 tf_bitangent = (mv_mat_simple * vec4(cross(tangent.xyz, normal.xyz), 0.0)).xyz;
	fi.tbn = (mat3(tf_tangent, tf_bitangent, tf_normal)); // TODO: Neccessity of transpose()?
	vec4 vpos_mv = mv_matrix * vpos;
#if MCM_TH
	fi.thv = thv;
	fi.thw = thw;
	fi.tcol = color_for(vpos_mv, tcol);
#else
	fi.thv = vec4(abs(position.w), abs(texcoords.w), abs(normal.w), abs(tangent.w));
	fi.thw = vec4(position.w < 0 ? 0.0 : 1.0, texcoords.w < 0 ? 0.0 : 1.0, normal.w < 0 ? 0.0 : 1.0, tangent.w < 0 ? 0.0 : 1.0);
	fi.tcol = vec4(1.0); // TODO: Actual tcol?
#endif
    fi.color = color_for(vpos_mv, color * v_color);
	fi.pos = vpos_mv.xyz;
	gl_Position = proj_matrix * vpos_mv;
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
