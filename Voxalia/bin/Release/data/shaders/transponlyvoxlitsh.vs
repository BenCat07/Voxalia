#version 430 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec3 texcoord;
layout (location = 3) in vec4 color;
layout (location = 4) in vec4 tcol;
layout (location = 5) in vec3 tangent;

layout (location = 1) uniform mat4 projection = mat4(1.0);
layout (location = 2) uniform mat4 model_matrix = mat4(1.0);
layout (location = 3) uniform vec4 v_color = vec4(1.0);
// ...

layout (location = 0) out vec4 f_color;
layout (location = 1) out vec3 f_texcoord;
layout (location = 2) out vec3 f_position;
layout (location = 3) out vec4 f_tcol;
layout (location = 4) out mat3 f_tbn;

void main()
{
	f_tcol = tcol;
	f_color = color;
    if (f_color == vec4(0.0, 0.0, 0.0, 1.0))
    {
        f_color = vec4(1.0);
    }
    f_color = f_color * v_color;
	f_texcoord = texcoord;
	vec4 tpos = model_matrix * vec4(position, 1.0);
	f_position = tpos.xyz / tpos.w;
	gl_Position = projection * tpos;
	mat4 mv_mat_simple = model_matrix;
	mv_mat_simple[3][0] = 0.0;
	mv_mat_simple[3][1] = 0.0;
	mv_mat_simple[3][2] = 0.0;
	vec3 tf_normal = (mv_mat_simple * vec4(normal, 0.0)).xyz;
	vec3 tf_tangent = (mv_mat_simple * vec4(tangent, 0.0)).xyz;
	vec3 tf_bitangent = (mv_mat_simple * vec4(cross(tangent, normal), 0.0)).xyz;
	f_tbn = transpose(mat3(tf_tangent, tf_bitangent, tf_normal));
}
