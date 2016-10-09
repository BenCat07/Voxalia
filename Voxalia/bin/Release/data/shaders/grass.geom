#version 430 core

#define MCM_PRETTY 0

layout (points) in;
layout (triangle_strip, max_vertices = 6) out;

layout (location = 1) uniform mat4 proj_matrix = mat4(1.0);
// ...
layout (location = 7) uniform vec3 wind = vec3(0.0);
// ...
layout (location = 6) uniform float time = 0.0;

in struct vox_out
{
#if MCM_PRETTY
	vec4 position;
	vec2 texcoord;
	vec4 color;
	mat3 tbn;
#else
	vec2 texcoord;
	vec4 color;
#endif
} f[1];

out struct vox_out
{
#if MCM_PRETTY
	vec4 position;
	vec2 texcoord;
	vec4 color;
	mat3 tbn;
#else
	vec2 texcoord;
	vec4 color;
#endif
} fi;

float snoise(in vec3 v);
float snoise2(in vec3 v);

vec4 qfix(in vec4 pos, in vec3 right, in vec3 pos_norm)
{
#if MCM_PRETTY
	fi.position = pos;
	vec3 tf_norm = pos_norm;
	fi.tbn = transpose(mat3(right, cross(right, tf_norm), tf_norm)); // TODO: Neccessity of transpose()?
#endif
	return pos;
}

void main()
{
	vec3 pos = gl_in[0].gl_Position.xyz;
	if (dot(pos, pos) > (50.0 * 50.0)) // TODO: Configurable grass render range cap!
	{
		return;
	}
	float snz = snoise((pos + vec3(time, time, 0.0)) * 0.2);
	vec3 wnd = wind * snz;
	vec3 up = vec3(0.0, 0.0, 1.0);
	vec3 right = cross(up, normalize(vec3(pos.x, pos.y, 0.0))) * 0.3;
	vec3 nr = right * (1.0 / 0.3);
	vec3 pos_norm = normalize(pos.xyz + wnd);
	fi.color = vec4(f[0].color.xyz * dot(pos_norm, vec3(0.0, 0.0, -1.0)) * 0.5 + 0.5, 1.0) * f[0].color;
	// First Vertex
	gl_Position = proj_matrix * qfix(vec4(pos - (right) * 0.5, 1.0), nr, pos_norm);
	fi.texcoord = vec2(0.0, 1.0);
	EmitVertex();
	// Second Vertex
	gl_Position = proj_matrix * qfix(vec4(pos + (right) * 0.5, 1.0), nr, pos_norm);
	fi.texcoord = vec2(1.0, 1.0);
	EmitVertex();
	// Third Vertex
	gl_Position = proj_matrix * qfix(vec4(pos - (right - up * 2.0) * 0.5 + wnd, 1.0), nr, pos_norm);
	fi.texcoord = vec2(0.0, 0.5);
	EmitVertex();
	// Forth Vertex
	gl_Position = proj_matrix * qfix(vec4(pos + (right + up * 2.0) * 0.5 + wnd, 1.0), nr, pos_norm);
	fi.texcoord = vec2(1.0, 0.5);
	EmitVertex();
	// Fifth Vertex
	gl_Position = proj_matrix * qfix(vec4(pos - (right - up * 4.0) * 0.5 + wnd * 2.0, 1.0), nr, pos_norm);
	fi.texcoord = vec2(0.0, 0.0);
	EmitVertex();
	// Sixth Vertex
	gl_Position = proj_matrix * qfix(vec4(pos + (right + up * 4.0) * 0.5 + wnd * 2.0, 1.0), nr, pos_norm);
	fi.texcoord = vec2(1.0, 0.0);
	EmitVertex();
	EndPrimitive();
}



////////////////////////////// END SHADER //////////////////////////////
////////////////////////////////////////////////////////////////////////
///////////////////////////// BEGIN GLNOISE ////////////////////////////

// MONKEY: Cleaning + updating!

//
// Description : Array and textureless GLSL 2D/3D/4D simplex 
//							 noise functions.
//			Author : Ian McEwan, Ashima Arts.
//	Maintainer : ijm
//		 Lastmod : 20110822 (ijm)
//		 License : Copyright (C) 2011 Ashima Arts. All rights reserved.
//							 Distributed under the MIT License. See LICENSE file.
//							 https://github.com/ashima/webgl-noise
// 

vec3 mod289(in vec3 x) {
	return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 mod289(in vec4 x) {
	return x - floor(x * (1.0 / 289.0)) * 289.0;
}

vec4 permute(in vec4 x) {
		 return mod289(((x * 34.0) + 1.0) * x);
}

vec4 taylorInvSqrt(vec4 r)
{
	return 1.79284291400159 - 0.85373472095314 * r;
}

float snoise(in vec3 v)
{ 
	const vec2	C = vec2(1.0/6.0, 1.0/3.0);
	const vec4	D = vec4(0.0, 0.5, 1.0, 2.0);

	vec3 i = floor(v + dot(v, C.yyy));
	vec3 x0 = v - i + dot(i, C.xxx);

	vec3 g = step(x0.yzx, x0.xyz);
	vec3 l = 1.0 - g;
	vec3 i1 = min(g.xyz, l.zxy);
	vec3 i2 = max(g.xyz, l.zxy);

	vec3 x1 = x0 - i1 + C.xxx;
	vec3 x2 = x0 - i2 + C.yyy;
	vec3 x3 = x0 - D.yyy;

	i = mod289(i); 
	vec4 p = permute(permute(permute(i.z + vec4(0.0, i1.z, i2.z, 1.0)) + i.y + vec4(0.0, i1.y, i2.y, 1.0)) + i.x + vec4(0.0, i1.x, i2.x, 1.0));

	float n_ = 0.142857142857;
	vec3 ns = n_ * D.wyz - D.xzx;

	vec4 j = p - 49.0 * floor(p * ns.z * ns.z);

	vec4 x_ = floor(j * ns.z);
	vec4 y_ = floor(j - 7.0 * x_);

	vec4 x = x_ *ns.x + ns.yyyy;
	vec4 y = y_ *ns.x + ns.yyyy;
	vec4 h = 1.0 - abs(x) - abs(y);

	vec4 b0 = vec4(x.xy, y.xy);
	vec4 b1 = vec4(x.zw, y.zw);

	vec4 s0 = floor(b0) * 2.0 + 1.0;
	vec4 s1 = floor(b1) * 2.0 + 1.0;
	vec4 sh = -step(h, vec4(0.0));

	vec4 a0 = b0.xzyw + s0.xzyw*sh.xxyy;
	vec4 a1 = b1.xzyw + s1.xzyw*sh.zzww;

	vec3 p0 = vec3(a0.xy,h.x);
	vec3 p1 = vec3(a0.zw,h.y);
	vec3 p2 = vec3(a1.xy,h.z);
	vec3 p3 = vec3(a1.zw,h.w);

	vec4 norm = taylorInvSqrt(vec4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
	p0 *= norm.x;
	p1 *= norm.y;
	p2 *= norm.z;
	p3 *= norm.w;

	vec4 m = max(0.6 - vec4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
	m = m * m;
	return 42.0 * dot(m * m, vec4( dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
}

float snoise2(in vec3 v) // MONKEY: snoise2 (Entire function)
{
	return (snoise(v) + 1.0) * 0.5;
}