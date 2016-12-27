#version 430 core

layout (binding = 0) uniform sampler2DArray s;

in struct vox_out
{
	vec3 texcoord;
	vec3 tcol;
} f;

layout (location = 0) out vec4 color;

void main()
{
	color = vec4(texture(s, f.texcoord).xyz * f.tcol, 1.0);
}
