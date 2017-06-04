
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

bool rendersAtAll(in uvec4 blk)
{
	return blk.x != 0;
}

bool isOpaque(in uvec4 blk)
{
	return blk.x != 0;
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
	uvec4 cbXP = pos.x + 1 == gl_WorkGroupSize.x ? posses[blkSize() * 2 + blockID(uvec3(0, pos.y, pos.z))] : posses[blockID(uvec3(pos.x + 1, pos.y, pos.z))];
	uvec4 cbXM = pos.x == 0 ? posses[blkSize() * 1 + blockID(uvec3(gl_WorkGroupSize.x - 1, pos.y, pos.z))] : posses[blockID(uvec3(pos.x - 1, pos.y, pos.z))];
	uvec4 cbYP = pos.y + 1 == gl_NumWorkGroups.y ? posses[blkSize() * 4 + blockID(uvec3(pos.x, 0, pos.z))] : posses[blockID(uvec3(pos.x, pos.y + 1, pos.z))];
	uvec4 cbYM = pos.y == 0 ? posses[blkSize() * 3 + blockID(uvec3(pos.x, gl_NumWorkGroups.y - 1, pos.z))] : posses[blockID(uvec3(pos.x, pos.y - 1, pos.z))];
	uvec4 cbZP = pos.z + 1 == gl_WorkGroupSize.z ? posses[blkSize() * 6 + blockID(uvec3(pos.x, pos.y, 0))] : posses[blockID(uvec3(pos.x, pos.y, pos.z + 1))];
	uvec4 cbZM = pos.z == 0 ? posses[blkSize() * 5 + blockID(uvec3(pos.x, pos.y, gl_WorkGroupSize.z - 1))] : posses[blockID(uvec3(pos.x, pos.y, pos.z - 1))];
	int res = 0;
	if (!isOpaque(cbXP))
	{
		res += 6;
	}
	if (!isOpaque(cbXM))
	{
		res += 6;
	}
	if (!isOpaque(cbYP))
	{
		res += 6;
	}
	if (!isOpaque(cbYM))
	{
		res += 6;
	}
	if (!isOpaque(cbZP))
	{
		res += 6;
	}
	if (!isOpaque(cbZM))
	{
		res += 6;
	}
	return res;
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

void crunch_verts(in uvec3 pos, in uint start)
{
	// TODO: Shapes support?
	uint id = blockID(pos);
	uvec4 curBLK = posses[id];
	// Guaranteed to render (by indexmark != 0), don't check RendersAtAll.
	uvec4 cbXP = pos.x + 1 == gl_WorkGroupSize.x ? posses[blkSize() * 2 + blockID(uvec3(0, pos.y, pos.z))] : posses[blockID(uvec3(pos.x + 1, pos.y, pos.z))];
	uvec4 cbXM = pos.x == 0 ? posses[blkSize() * 1 + blockID(uvec3(gl_WorkGroupSize.x - 1, pos.y, pos.z))] : posses[blockID(uvec3(pos.x - 1, pos.y, pos.z))];
	uvec4 cbYP = pos.y + 1 == gl_NumWorkGroups.y ? posses[blkSize() * 4 + blockID(uvec3(pos.x, 0, pos.z))] : posses[blockID(uvec3(pos.x, pos.y + 1, pos.z))];
	uvec4 cbYM = pos.y == 0 ? posses[blkSize() * 3 + blockID(uvec3(pos.x, gl_NumWorkGroups.y - 1, pos.z))] : posses[blockID(uvec3(pos.x, pos.y - 1, pos.z))];
	uvec4 cbZP = pos.z + 1 == gl_WorkGroupSize.z ? posses[blkSize() * 6 + blockID(uvec3(pos.x, pos.y, 0))] : posses[blockID(uvec3(pos.x, pos.y, pos.z + 1))];
	uvec4 cbZM = pos.z == 0 ? posses[blkSize() * 5 + blockID(uvec3(pos.x, pos.y, gl_WorkGroupSize.z - 1))] : posses[blockID(uvec3(pos.x, pos.y, pos.z - 1))];
	vec3 fpos = vec3(pos);
	if (!isOpaque(cbXP))
	{
		// TODO: This section!
#if MODE_ONE
		f_position[start + 0] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 1] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 2] = vec4(fpos.x + 1.0, fpos.y, fpos.z, 0.0);
		f_position[start + 3] = vec4(fpos.x + 1.0, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 4] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 5] = vec4(fpos.x + 1.0, fpos.y, fpos.z, 0.0);
#endif
		for (uint i = 0; i < 6; i++)
		{
#if MODE_ONE
			f_normal[start + i] =  vec4(1.0, 0.0, 0.0, 0.0);
			f_tangent[start + i] = vec4(0.0, 1.0, 0.0, 0.0);
#endif
#if MODE_TWO
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
#endif
#if MODE_THREE
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
#endif
		}
#if MODE_ONE
		float texID = tex_for(curBLK.x, fpos, 2);
		f_texcoord[start + 0] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(0.0, 1.0, texID, 0.0);
#endif
		start += 6;
	}
	if (!isOpaque(cbXM))
	{
#if MODE_ONE
		f_position[start + 0] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		f_position[start + 1] = vec4(fpos.x, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 2] = vec4(fpos.x, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 3] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		f_position[start + 4] = vec4(fpos.x, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 5] = vec4(fpos.x, fpos.y, fpos.z + 1.0, 0.0);
#endif
		for (uint i = 0; i < 6; i++)
		{
#if MODE_ONE
			f_normal[start + i] =  vec4(-1.0, 0.0, 0.0, 0.0);
			f_tangent[start + i] = vec4(0.0, 1.0, 0.0, 0.0);
#endif
#if MODE_TWO
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
#endif
#if MODE_THREE
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
#endif
		}
#if MODE_ONE
		float texID = tex_for(curBLK.x, fpos, 4);
		f_texcoord[start + 0] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(1.0, 0.0, texID, 0.0);
#endif
		start += 6;
	}
	if (!isOpaque(cbYP))
	{
#if MODE_ONE
		f_position[start + 0] = vec4(fpos.x, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 1] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 2] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 3] = vec4(fpos.x, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 4] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 5] = vec4(fpos.x, fpos.y + 1.0, fpos.z + 1.0, 0.0);
#endif
		for (uint i = 0; i < 6; i++)
		{
#if MODE_ONE
			f_normal[start + i] =  vec4(0.0, 1.0, 0.0, 0.0);
			f_tangent[start + i] = vec4(1.0, 0.0, 0.0, 0.0);
#endif
#if MODE_TWO
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
#endif
#if MODE_THREE
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
#endif
		}
#if MODE_ONE
		float texID = tex_for(curBLK.x, fpos, 3);
		f_texcoord[start + 0] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(1.0, 0.0, texID, 0.0);
#endif
		start += 6;
	}
	if (!isOpaque(cbYM))
	{
#if MODE_ONE
		f_position[start + 0] = vec4(fpos.x + 1.0, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 1] = vec4(fpos.x + 1.0, fpos.y, fpos.z, 0.0);
		f_position[start + 2] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		f_position[start + 3] = vec4(fpos.x, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 4] = vec4(fpos.x + 1.0, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 5] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
#endif
		for (uint i = 0; i < 6; i++)
		{
#if MODE_ONE
			f_normal[start + i] =  vec4(0.0, -1.0, 0.0, 0.0);
			f_tangent[start + i] = vec4(1.0, 0.0, 0.0, 0.0);
#endif
#if MODE_TWO
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
#endif
#if MODE_THREE
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
#endif
		}
#if MODE_ONE
		float texID = tex_for(curBLK.x, fpos, 5);
		f_texcoord[start + 0] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(0.0, 1.0, texID, 0.0);
#endif
		start += 6;
	}
	if (!isOpaque(cbZP))
	{
#if MODE_ONE
		f_position[start + 0] = vec4(fpos.x, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 1] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 2] = vec4(fpos.x, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 3] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 4] = vec4(fpos.x + 1.0, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 5] = vec4(fpos.x, fpos.y, fpos.z + 1.0, 0.0);
#endif
		for (uint i = 0; i < 6; i++)
		{
#if MODE_ONE
			f_normal[start + i] =  vec4(0.0, 0.0, 1.0, 0.0);
			f_tangent[start + i] = vec4(1.0, 0.0, 0.0, 0.0);
#endif
#if MODE_TWO
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
#endif
#if MODE_THREE
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
#endif
		}
#if MODE_ONE
		float texID = tex_for(curBLK.x, fpos, 0);
		f_texcoord[start + 0] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(0.0, 0.0, texID, 0.0);
#endif
		start += 6;
	}
	if (!isOpaque(cbZM))
	{
#if MODE_ONE
		f_position[start + 0] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		f_position[start + 1] = vec4(fpos.x + 1.0, fpos.y, fpos.z, 0.0);
		f_position[start + 2] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 3] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		f_position[start + 4] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 5] = vec4(fpos.x, fpos.y + 1.0, fpos.z, 0.0);
#endif
		for (uint i = 0; i < 6; i++)
		{
#if MODE_ONE
			f_normal[start + i] =  vec4(0.0, 0.0, -1.0, 0.0);
			f_tangent[start + i] = vec4(1.0, 0.0, 0.0, 0.0);
#endif
#if MODE_TWO
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
#endif
#if MODE_THREE
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
#endif
		}
#if MODE_ONE
		float texID = tex_for(curBLK.x, fpos, 1);
		f_texcoord[start + 0] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(1.0, 1.0, texID, 0.0);
#endif
		start += 6;
	}
}
#endif

// Helper for Voxels END
