//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

#version 430 core

#define MCM_PRETTY 0
#define MCM_FADE_DEPTH 0

layout (points) in;
layout (triangle_strip, max_vertices = 4) out;

layout (location = 1) uniform mat4 proj_matrix = mat4(1.0);

in struct vox_out
{
#if MCM_PRETTY
	vec4 position;
	vec2 texcoord;
	vec4 color;
	mat3 tbn;
#else
	vec3 norm;
	vec2 texcoord;
	vec4 color;
#endif
} f[1];

out struct vox_fout
{
#if MCM_PRETTY
	vec4 position;
	vec3 texcoord;
	vec4 color;
	mat3 tbn;
	vec2 scrpos;
	float z;
#else
	vec3 norm;
	vec3 texcoord;
	vec4 color;
#endif
#if MCM_FADE_DEPTH
	float size;
#endif
} fi;

vec4 qfix(in vec4 pos, in vec3 right, in vec3 pos_norm)
{
#if MCM_PRETTY
	fi.position = pos;
	vec4 npos = proj_matrix * pos;
	fi.scrpos = npos.xy / npos.w * 0.5 + vec2(0.5);
	fi.z = npos.z;
	fi.tbn = transpose(mat3(right, cross(right, pos_norm), pos_norm)); // TODO: Neccessity of transpose()?
#else
	fi.norm = pos_norm;
#endif
	return pos;
}

void main()
{
	vec3 pos = gl_in[0].gl_Position.xyz;
	 // TODO: Configurable particles render range cap!
	/*if (dot(pos, pos) > (50.0 * 50.0))
	{
		return;
	}*/
	vec3 up = vec3(0.0, 0.0, 1.0);
	vec3 pos_norm = normalize(pos.xyz);
	if (abs(pos_norm.x) < 0.01 && abs(pos_norm.y) < 0.01)
	{
		up = vec3(0.0, 1.0, 0.0);
	}
	float scale = f[0].texcoord.x * 0.5;
	float tid = f[0].texcoord.y;
	vec3 right = cross(up, pos_norm);
	fi.color = f[0].color;
#if MCM_FADE_DEPTH
	fi.size = 1.0 / scale;
#endif
	// First Vertex
	gl_Position = proj_matrix * qfix(vec4(pos - (right) * scale, 1.0), right, pos_norm);
	fi.texcoord = vec3(0.0, 1.0, tid);
	EmitVertex();
	// Second Vertex
	gl_Position = proj_matrix * qfix(vec4(pos + (right) * scale, 1.0), right, pos_norm);
	fi.texcoord = vec3(1.0, 1.0, tid);
	EmitVertex();
	// Third Vertex
	gl_Position = proj_matrix * qfix(vec4(pos - (right - up * 2.0) * scale, 1.0), right, pos_norm);
	fi.texcoord = vec3(0.0, 0.0, tid);
	EmitVertex();
	// Forth Vertex
	gl_Position = proj_matrix * qfix(vec4(pos + (right + up * 2.0) * scale, 1.0), right, pos_norm);
	fi.texcoord = vec3(1.0, 0.0, tid);
	EmitVertex();
	EndPrimitive();
}
