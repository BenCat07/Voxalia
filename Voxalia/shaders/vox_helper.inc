
// Helper for Voxels START

// Requires glnoise.inc
float snoise(in vec3 v);
float snoise2(in vec3 v);

uint blkSize()
{
	return gl_WorkGroupSize.x * gl_NumWorkGroups.y * gl_WorkGroupSize.z;
}

uint blockID(in uvec3 pos)
{
	return pos.z * (gl_WorkGroupSize.x * gl_NumWorkGroups.y) + pos.y * gl_WorkGroupSize.x + pos.x;
}

uint blockMat(in uint matID)
{
	return matID & (16384 - 1);
}

uint blockDamage(in uint matID)
{
		return (matID & (16384 | (16384 * 2))) / (16384);
}

bool rendersAtAll(in uvec4 blk)
{
	return blockMat(blk.x) != 0;
}

bool isOpaque(in uvec4 blk)
{
	return blockMat(blk.x) != 0;
}

uint shapeIndex(in uvec4 curBLK, in uvec3 pos)
{
	uvec4 cbXP = pos.x + 1 == gl_WorkGroupSize.x ? posses[blkSize() * 2 + blockID(uvec3(0, pos.y, pos.z))] : posses[blockID(uvec3(pos.x + 1, pos.y, pos.z))];
	uvec4 cbXM = pos.x == 0 ? posses[blkSize() * 1 + blockID(uvec3(gl_WorkGroupSize.x - 1, pos.y, pos.z))] : posses[blockID(uvec3(pos.x - 1, pos.y, pos.z))];
	uvec4 cbYP = pos.y + 1 == gl_NumWorkGroups.y ? posses[blkSize() * 4 + blockID(uvec3(pos.x, 0, pos.z))] : posses[blockID(uvec3(pos.x, pos.y + 1, pos.z))];
	uvec4 cbYM = pos.y == 0 ? posses[blkSize() * 3 + blockID(uvec3(pos.x, gl_NumWorkGroups.y - 1, pos.z))] : posses[blockID(uvec3(pos.x, pos.y - 1, pos.z))];
	uvec4 cbZP = pos.z + 1 == gl_WorkGroupSize.z ? posses[blkSize() * 6 + blockID(uvec3(pos.x, pos.y, 0))] : posses[blockID(uvec3(pos.x, pos.y, pos.z + 1))];
	uvec4 cbZM = pos.z == 0 ? posses[blkSize() * 5 + blockID(uvec3(pos.x, pos.y, gl_WorkGroupSize.z - 1))] : posses[blockID(uvec3(pos.x, pos.y, pos.z - 1))];
	uint sh_subdat = (isOpaque(cbXP) ? 1 : 0) | (isOpaque(cbXM) ? 2 : 0)
				   | (isOpaque(cbYP) ? 4 : 0)| (isOpaque(cbYM) ? 8 : 0)
				   | (isOpaque(cbZP) ? 16 : 0)| (isOpaque(cbZM) ? 32 : 0);
	uint tid = curBLK.z * (64 * 4) + sh_subdat * 4 + blockDamage(curBLK.x);
	return f_shapes[tid];
}

int count_visible_verts(in uvec3 pos)
{
	// TODO: Shapes support?
	uint id = blockID(pos);
	uvec4 curBLK = posses[id];
	if (!rendersAtAll(curBLK))
	{
		return 0;
	}
	uint shInd = shapeIndex(curBLK, pos);
	return int(f_shapes[shInd]);
}

#if CRUNCHABLE
float tex_for(in uint mat, in vec3 fpos, in int tp)
{
	float count = imageLoad(texture_ids, ivec2(mat, tp * 7)).x;
	if (count < 1.5)
	{
		return imageLoad(texture_ids, ivec2(mat, 1 + tp * 7)).x;
	}
	int id = int(snoise2(fpos) * count);
	return imageLoad(texture_ids, ivec2(mat, 1 + id + tp * 7)).x;
}

float swap_tex(in uint mat, in vec3 fpos, in float zer)
{
	if (zer == tex_for(5, fpos, 0))
	{
		return tex_for(mat, fpos, 0);
	}
	if (zer == tex_for(5, fpos, 1))
	{
		return tex_for(mat, fpos, 1);
	}
	if (zer == tex_for(5, fpos, 2))
	{
		return tex_for(mat, fpos, 2);
	}
	if (zer == tex_for(5, fpos, 3))
	{
		return tex_for(mat, fpos, 3);
	}
	if (zer == tex_for(5, fpos, 4))
	{
		return tex_for(mat, fpos, 4);
	}
	return tex_for(mat, fpos, 5);
}

void crunch_verts(in uvec3 pos, in uint start)
{
	// TODO: Shapes support?
	uint id = blockID(pos);
	uvec4 curBLK = posses[id];
	// Guaranteed to render (by indexmark != 0), don't check RendersAtAll.
	vec3 fpos = vec3(pos);
	int shInd = int(shapeIndex(curBLK, pos));
	int vertCount = int(f_shapes[shInd]);
	uint mat = blockMat(curBLK.x);
	for (int i = 0; i < vertCount; i += 3)
	{
		int it3 = i;
		int o_i = i * 3;
#if MODE_ONE
		int inder = shInd + 1 + vertCount * 0 + o_i;
		vec3 v1 = vec3(uintBitsToFloat(f_shapes[inder]), uintBitsToFloat(f_shapes[inder + 1]), uintBitsToFloat(f_shapes[inder + 2]));
		vec3 v2 = vec3(uintBitsToFloat(f_shapes[inder + 3]), uintBitsToFloat(f_shapes[inder + 4]), uintBitsToFloat(f_shapes[inder + 5]));
		vec3 v3 = vec3(uintBitsToFloat(f_shapes[inder + 6]), uintBitsToFloat(f_shapes[inder + 7]), uintBitsToFloat(f_shapes[inder + 8]));
		inder = shInd + 1 + vertCount * 3 + o_i;
		vec3 n1 = vec3(uintBitsToFloat(f_shapes[inder]), uintBitsToFloat(f_shapes[inder + 1]), uintBitsToFloat(f_shapes[inder + 2]));
		vec3 n2 = vec3(uintBitsToFloat(f_shapes[inder + 3]), uintBitsToFloat(f_shapes[inder + 4]), uintBitsToFloat(f_shapes[inder + 5]));
		vec3 n3 = vec3(uintBitsToFloat(f_shapes[inder + 6]), uintBitsToFloat(f_shapes[inder + 7]), uintBitsToFloat(f_shapes[inder + 8]));
		inder = shInd + 1 + vertCount * 6 + o_i;
		vec3 t1 = vec3(uintBitsToFloat(f_shapes[inder]), uintBitsToFloat(f_shapes[inder + 1]), uintBitsToFloat(f_shapes[inder + 2]));
		vec3 t2 = vec3(uintBitsToFloat(f_shapes[inder + 3]), uintBitsToFloat(f_shapes[inder + 4]), uintBitsToFloat(f_shapes[inder + 5]));
		vec3 t3 = vec3(uintBitsToFloat(f_shapes[inder + 6]), uintBitsToFloat(f_shapes[inder + 7]), uintBitsToFloat(f_shapes[inder + 8]));
		f_position[start + it3 + 0] = vec4(fpos + v1, 0.0);
		f_position[start + it3 + 1] = vec4(fpos + v2, 0.0);
		f_position[start + it3 + 2] = vec4(fpos + v3, 0.0);
		f_normal[start + it3 + 0] =  vec4(n1, 0.0);
		f_normal[start + it3 + 1] =  vec4(n2, 0.0);
		f_normal[start + it3 + 2] =  vec4(n3, 0.0);
		// TODO: Correct TextureIDs
		f_texcoord[start + it3 + 0] = vec4(t1.xy, swap_tex(mat, fpos, t1.z), 0.0);
		f_texcoord[start + it3 + 1] = vec4(t2.xy, swap_tex(mat, fpos, t2.z), 0.0);
		f_texcoord[start + it3 + 2] = vec4(t3.xy, swap_tex(mat, fpos, t3.z), 0.0);
		vec3 dv1 = v2 - v1;
		vec3 dv2 = v3 - v1;
		vec3 dt1 = t2 - t1;
		vec3 dt2 = t3 - t1;
		vec3 tng = (dv1 * dt2.y - dv2 * dt1.y) / (dt1.x * dt2.y - dt1.y * dt2.x);
		f_tangent[start + it3 + 0] = vec4(tng, 0.0);
		f_tangent[start + it3 + 1] = vec4(tng, 0.0);
		f_tangent[start + it3 + 2] = vec4(tng, 0.0);
#endif
#if MODE_TWO
		f_indices[start + it3] = start + it3;
		f_color[start + it3] = vec4(1.0);
		f_tcol[start + it3] = vec4(1.0);
		f_indices[start + it3 + 1] = start + it3 + 1;
		f_color[start + it3 + 1] = vec4(1.0);
		f_tcol[start + it3 + 1] = vec4(1.0);
		f_indices[start + it3 + 2] = start + it3 + 2;
		f_color[start + it3 + 2] = vec4(1.0);
		f_tcol[start + it3 + 2] = vec4(1.0);
#endif
#if MODE_THREE
		// TODO: Actual values!
		f_thv[start + it3] = vec4(0.0);
		f_thw[start + it3] = vec4(0.0);
		f_thv[start + it3 + 1] = vec4(0.0);
		f_thw[start + it3 + 1] = vec4(0.0);
		f_thv[start + it3 + 2] = vec4(0.0);
		f_thw[start + it3 + 2] = vec4(0.0);
#endif
	}
}
#endif

// Helper for Voxels END
