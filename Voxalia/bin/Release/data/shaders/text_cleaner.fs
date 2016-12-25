#version 430 core

layout (binding = 0) uniform sampler2DArray tex;

in struct text_fout
{
	vec4 color;
	vec3 texcoord;
} fi;

out vec4 color;

void main()
{
	vec4 tcolor = texture(tex, fi.texcoord);
	color = vec4(fi.color.xyz, ((tcolor.x + tcolor.y + tcolor.z) / 3) * fi.color.w);
}
