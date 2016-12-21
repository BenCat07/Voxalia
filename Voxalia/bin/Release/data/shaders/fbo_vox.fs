#version 430 core

#define MCM_REFRACT 0

layout (binding = 0) uniform sampler2DArray s;
layout (binding = 1) uniform sampler2DArray htex;
layout (binding = 2) uniform sampler2DArray normal_tex;

layout (location = 3) uniform vec4 v_color = vec4(1.0);
// ...
layout (location = 5) uniform float minimum_light = 0.0;
// ...
layout (location = 8) uniform vec2 light_clamp = vec2(0.0, 1.0);

in struct vox_out
{
	vec4 position;
	vec3 texcoord;
	vec4 color;
	vec4 tcol;
	mat3 tbn;
	vec4 thv;
	vec4 thw;
} f;

layout (location = 0) out vec4 color;
layout (location = 1) out vec4 position;
layout (location = 2) out vec4 normal;
layout (location = 3) out vec4 renderhint;
layout (location = 4) out vec4 renderhint2;

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
	vec4 col = texture(s, f.texcoord);
	vec4 thval = vec4(0.0);
	float thstr = 0.0;
	vec4 th_weight = min(max((f.thw - (vec4(1.0 - f.texcoord.x, f.texcoord.x, 1.0 - f.texcoord.y, f.texcoord.y) * 4.0)), vec4(0.0)), vec4(1.0));
	thstr += th_weight.x;
	thval += texture(s, vec3(vec2(1.0 - f.texcoord.x, f.texcoord.y), f.thv.x)) * th_weight.x;
	thstr += th_weight.y;
	thval += texture(s, vec3(vec2(1.0 - f.texcoord.x, f.texcoord.y), f.thv.y)) * th_weight.y;
	thstr += th_weight.z;
	thval += texture(s, vec3(vec2(f.texcoord.x, 1.0 - f.texcoord.y), f.thv.z)) * th_weight.z;
	thstr += th_weight.w;
	thval += texture(s, vec3(vec2(f.texcoord.x, 1.0 - f.texcoord.y), f.thv.w)) * th_weight.w;
	float tw = col.w;
	float trel = min(1.0 - thstr, 1.0);
	thval += col * trel;
	thstr += trel;
	col = thval / thstr;
	col.w = tw;
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
		else
		{
			col *= texture(s, vec3(f.texcoord.xy, 0));
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
	vec3 norms = texture(normal_tex, f.texcoord).xyz * 2.0 - vec3(1.0);
	color = col * v_color;
	position = vec4(f.position.xyz, 1.0);
	normal = vec4(normalize(f.tbn * norms), 1.0);
	float light_min = clamp(minimum_light + dets.a, 0.0, 1.0);
	color = vec4(color.xyz * lightcol, color.w);
	renderhint = vec4(spec, rhBlur, light_min, 1.0);
	renderhint2 = vec4(0.0, refl, 0.0, 1.0);
}
