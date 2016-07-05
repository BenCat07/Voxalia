#version 430 core
// transponlyvox.fs

#define MCM_GOOD_GRAPHICS 0
#define MCM_LIT 0
#define MCM_SHADOWS 0
#define MCM_LL 0

layout (binding = 0) uniform sampler2DArray tex;
layout (binding = 1) uniform sampler2DArray htex;
layout (binding = 2) uniform sampler2DArray normal_tex;
layout (binding = 3) uniform sampler2DShadow shadowtex;
#if MCM_LL
layout(size1x32, binding = 5) coherent uniform uimage2D ui_page;
layout(size1x32, binding = 6) coherent uniform uimage2D ui_frag;
layout(size1x32, binding = 7) coherent uniform uimage2D ui_sema;
layout(size4x32, binding = 8) coherent uniform imageBuffer uib_spage;
layout(size1x32, binding = 9) coherent uniform uimageBuffer uib_llist;
layout(size1x32, binding = 10) coherent uniform uimageBuffer uib_cspage;
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

layout (location = 4) uniform float desaturationAmount = 1.0;
layout (location = 5) uniform float minimum_light;
layout (location = 6) uniform mat4 shadow_matrix;
layout (location = 7) uniform vec3 light_color = vec3(1.0, 1.0, 1.0);
layout (location = 8) uniform mat4 light_details;
layout (location = 9) uniform mat4 light_details2;

#if MCM_LL
#else
out vec4 color;
#endif

vec3 desaturate(vec3 c)
{
	return mix(c, vec3(0.95, 0.77, 0.55) * dot(c, vec3(1.0)), desaturationAmount);
}

void main()
{
#if MCM_LL
	vec4 color;
#endif
	vec4 tcolor = texture(tex, f.texcoord);
	if (f.tcol.w == 0.0 && f.tcol.x == 0.0 && f.tcol.z == 0.0 && f.tcol.y > 0.3 && f.tcol.y < 0.7)
	{
		// float rhBlur = (f.tcol.y - 0.31) * ((1.0 / 0.38) * (3.14159 * 2.0));
	}
	else
	{
		tcolor *= f.tcol;
	}
	if (tcolor.w * f.color.w >= 0.99)
	{
		discard;
	}
    if (tcolor.w * f.color.w < 0.01)
    {
        discard;
    }
	color = tcolor;
#if MCM_LIT
	vec4 dets = texture(htex, f.texcoord);
	vec3 norms = texture(normal_tex, f.texcoord).xyz * 2.0 - 1.0;
    float spec = dets.r; // TODO: Refract / reflect?
	float light_radius = light_details[0][0];
	vec3 diffuse_albedo = vec3(light_details[0][1], light_details[0][2], light_details[0][3]);
	vec3 specular_albedo = vec3(light_details[1][0], light_details[1][1], light_details[1][2]);
	float light_type = light_details[1][3];
	float should_sqrt = light_details[2][0];
	float tex_size = light_details[2][1];
	float depth_jump = light_details[2][2];
	float lightc = light_details[2][3];
	float light_min = clamp(minimum_light + dets.a, 0.0, 1.0);
	float blighting = max((f.color.x + f.color.y + f.color.z) / 3.0, light_min); // TODO: Clamp f.color.xyz, match fbo_vox;
	color = vec4(color.xyz * blighting, color.w);
	vec4 bambient = (vec4(light_details[3][0], light_details[3][1], light_details[3][2], 1.0) + vec4(light_min, light_min, light_min, 0.0)) / lightc;
	vec3 eye_pos = vec3(light_details2[0][0], light_details2[0][1], light_details2[0][2]);
	vec3 light_pos = vec3(light_details2[1][0], light_details2[1][1], light_details2[1][2]);
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
#if MCM_SHADOWS
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
		color = vec4(0.0, 0.0, 0.0, color.w);
		return;
	}
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
			depth += textureProj(shadowtex, fs + vec4(x * jump, y * jump, offz, 0.0));
			depth_count++;
		}
	}
	depth = depth / depth_count;
#else
	float depth = textureProj(shadowtex, fs - vec4(0.0, 0.0, 0.0001, 0.0));
#endif
	vec3 L = light_path / light_length;
	vec4 diffuse = vec4(max(dot(N, -L), 0.0) * diffuse_albedo, 1.0);
	vec3 specular = vec3(pow(max(dot(reflect(L, N), normalize(f.position.xyz - eye_pos)), 0.0), 128.0) * specular_albedo * spec);
	color = vec4((bambient * color + (vec4(depth, depth, depth, 1.0) * atten * (diffuse * vec4(light_color, 1.0)) * color) +
		(vec4(min(specular, 1.0), 0.0) * vec4(light_color, 1.0) * atten * depth)).xyz, color.w);
#else
	vec4 fs = x_spos / x_spos.w / 2.0 + vec4(0.5, 0.5, 0.5, 0.0);
	fs.w = 1.0;
	if (fs.x < 0.0 || fs.x > 1.0
		|| fs.y < 0.0 || fs.y > 1.0
		|| fs.z < 0.0 || fs.z > 1.0)
	{
		color = vec4(0.0, 0.0, 0.0, color.w);
		return;
	}
	vec3 L = light_path / light_length;
	vec4 diffuse = vec4(max(dot(N, -L), 0.0) * diffuse_albedo, 1.0);
	vec3 specular = vec3(pow(max(dot(reflect(L, N), normalize(f.position.xyz - eye_pos)), 0.0), 128.0) * specular_albedo * spec);
	color = vec4((bambient * color + (vec4(1.0) * atten * (diffuse * vec4(light_color, 1.0)) * color) +
		(vec4(min(specular, 1.0), 0.0) * vec4(light_color, 1.0) * atten)).xyz, color.w);
#endif
#endif
#if MCM_GOOD_GRAPHICS
    color = vec4(desaturate(color.xyz), color.w); // TODO: Make available to all, not just good graphics only! Or a separate CVar!
#endif
#if MCM_LL
	uint page = 0;
	uint frag = 0;
	uint frag_mod = 0;
	ivec2 scrpos = ivec2(f.scrpos * u_screensize);
	int i = 0;
	while (imageAtomicExchange(ui_sema, scrpos, 1U) != 0U && i < 1000) // TODO: 1000 -> uniform var?!
	{
		memoryBarrier();
		i++;
	}
	/*if (i == 1000)
	{
		return;
	}*/
	page = imageLoad(ui_page, scrpos).x;
	frag = imageLoad(ui_frag, scrpos).x;
	frag_mod = frag % P_SIZE;
	if (frag_mod == 0)
	{
		uint npage = imageAtomicAdd(uib_cspage, 0, P_SIZE);
		if (npage < ab_shared_pool_size)
		{
			imageStore(uib_llist, int(npage / P_SIZE), uvec4(page, 0U, 0U, 0U));
			imageStore(ui_page, scrpos, uvec4(npage, 0U, 0U, 0U));
			page = npage;
		}
		else
		{
			page = 0;
		}
	}
	if (page > 0)
	{
		imageStore(ui_frag, scrpos, uvec4(frag + 1, 0U, 0U, 0U));
	}
	frag = frag_mod;
	memoryBarrier();
	imageAtomicExchange(ui_sema, scrpos, 0U);
	vec4 abv = color;
	abv.z = (abv.x * 255) + (abv.w * 255 * 255);
	abv.w = f.z;
	imageStore(uib_spage, int(page + frag), abv);
#endif
}
