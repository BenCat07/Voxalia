
// Helper for Voxels START

uint blockID(in uvec3 pos)
{
	return pos.z * gl_NumWorkGroups.x * gl_NumWorkGroups.y + pos.y * gl_NumWorkGroups.x + pos.x;
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
	uvec4 cbXP = pos.x + 1 == gl_NumWorkGroups.x ? uvec4(0) : posses[blockID(uvec3(pos.x + 1, pos.y, pos.z))];
	uvec4 cbXM = pos.x == 0 ? uvec4(0) : posses[blockID(uvec3(pos.x - 1, pos.y, pos.z))];
	uvec4 cbYP = pos.y + 1 == gl_NumWorkGroups.y ? uvec4(0) : posses[blockID(uvec3(pos.x, pos.y + 1, pos.z))];
	uvec4 cbYM = pos.y == 0 ? uvec4(0) : posses[blockID(uvec3(pos.x, pos.y - 1, pos.z))];
	uvec4 cbZP = pos.z + 1 == gl_NumWorkGroups.z ? uvec4(0) : posses[blockID(uvec3(pos.x, pos.y, pos.z + 1))];
	uvec4 cbZM = pos.z == 0 ? uvec4(0) : posses[blockID(uvec3(pos.x , pos.y, pos.z - 1))];
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
void crunch_verts(in uvec3 pos, in uint start)
{
	// TODO: Shapes support?
	uint id = blockID(pos);
	uvec4 curBLK = posses[id];
	if (!rendersAtAll(curBLK))
	{
		return;
	}
	uvec4 cbXP = pos.x + 1 == gl_NumWorkGroups.x ? uvec4(0) : posses[blockID(uvec3(pos.x + 1, pos.y, pos.z))];
	uvec4 cbXM = pos.x == 0 ? uvec4(0) : posses[blockID(uvec3(pos.x - 1, pos.y, pos.z))];
	uvec4 cbYP = pos.y + 1 == gl_NumWorkGroups.y ? uvec4(0) : posses[blockID(uvec3(pos.x, pos.y + 1, pos.z))];
	uvec4 cbYM = pos.y == 0 ? uvec4(0) : posses[blockID(uvec3(pos.x, pos.y - 1, pos.z))];
	uvec4 cbZP = pos.z + 1 == gl_NumWorkGroups.z ? uvec4(0) : posses[blockID(uvec3(pos.x, pos.y, pos.z + 1))];
	uvec4 cbZM = pos.z == 0 ? uvec4(0) : posses[blockID(uvec3(pos.x , pos.y, pos.z - 1))];
	int res = 0;
	vec3 fpos = vec3(pos);
	if (!isOpaque(cbXP))
	{
		// TODO: This section!
		f_position[start + 0] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 1] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 2] = vec4(fpos.x + 1.0, fpos.y, fpos.z, 0.0);
		f_position[start + 3] = vec4(fpos.x + 1.0, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 4] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 5] = vec4(fpos.x + 1.0, fpos.y, fpos.z, 0.0);
		for (uint i = 0; i < 6; i++)
		{
			f_normal[start + i] =  vec4(1.0, 0.0, 0.0, 0.0);
			f_tangent[start + i] = vec4(0.0, 1.0, 0.0, 0.0);
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
		}
		float texID = imageLoad(texture_ids, ivec2(curBLK.x, 2)).x;
		f_texcoord[start + 0] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(0.0, 1.0, texID, 0.0);
		start += 6;
	}
	if (!isOpaque(cbXM))
	{
		// TODO: This section!
		f_position[start + 0] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		f_position[start + 1] = vec4(fpos.x, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 2] = vec4(fpos.x, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 3] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		f_position[start + 4] = vec4(fpos.x, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 5] = vec4(fpos.x, fpos.y, fpos.z + 1.0, 0.0);
		for (uint i = 0; i < 6; i++)
		{
			f_normal[start + i] =  vec4(-1.0, 0.0, 0.0, 0.0);
			f_tangent[start + i] = vec4(0.0, 1.0, 0.0, 0.0);
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
		}
		float texID = imageLoad(texture_ids, ivec2(curBLK.x, 4)).x;
		f_texcoord[start + 0] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(1.0, 0.0, texID, 0.0);
		start += 6;
	}
	if (!isOpaque(cbYP))
	{
		// TODO: This section!
		f_position[start + 0] = vec4(fpos.x, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 1] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 2] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 3] = vec4(fpos.x, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 4] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 5] = vec4(fpos.x, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		for (uint i = 0; i < 6; i++)
		{
			f_normal[start + i] =  vec4(0.0, 1.0, 0.0, 0.0);
			f_tangent[start + i] = vec4(1.0, 0.0, 0.0, 0.0);
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
		}
		float texID = imageLoad(texture_ids, ivec2(curBLK.x, 3)).x;
		f_texcoord[start + 0] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(1.0, 0.0, texID, 0.0);
		start += 6;
	}
	if (!isOpaque(cbYM))
	{
		// TODO: This section!
		f_position[start + 0] = vec4(fpos.x + 1.0, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 1] = vec4(fpos.x + 1.0, fpos.y, fpos.z, 0.0);
		f_position[start + 2] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		f_position[start + 3] = vec4(fpos.x, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 4] = vec4(fpos.x + 1.0, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 5] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		for (uint i = 0; i < 6; i++)
		{
			f_normal[start + i] =  vec4(0.0, -1.0, 0.0, 0.0);
			f_tangent[start + i] = vec4(1.0, 0.0, 0.0, 0.0);
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
		}
		float texID = imageLoad(texture_ids, ivec2(curBLK.x, 5)).x;
		f_texcoord[start + 0] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(0.0, 1.0, texID, 0.0);
		start += 6;
	}
	if (!isOpaque(cbZP))
	{
		f_position[start + 0] = vec4(fpos.x, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 1] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 2] = vec4(fpos.x, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 3] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z + 1.0, 0.0);
		f_position[start + 4] = vec4(fpos.x + 1.0, fpos.y, fpos.z + 1.0, 0.0);
		f_position[start + 5] = vec4(fpos.x, fpos.y, fpos.z + 1.0, 0.0);
		for (uint i = 0; i < 6; i++)
		{
			f_normal[start + i] =  vec4(0.0, 0.0, 1.0, 0.0);
			f_tangent[start + i] = vec4(1.0, 0.0, 0.0, 0.0);
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
		}
		float texID = imageLoad(texture_ids, ivec2(curBLK.x, 0)).x;
		f_texcoord[start + 0] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(0.0, 0.0, texID, 0.0);
		start += 6;
	}
	if (!isOpaque(cbZM))
	{
		f_position[start + 0] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		f_position[start + 1] = vec4(fpos.x + 1.0, fpos.y, fpos.z, 0.0);
		f_position[start + 2] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 3] = vec4(fpos.x, fpos.y, fpos.z, 0.0);
		f_position[start + 4] = vec4(fpos.x + 1.0, fpos.y + 1.0, fpos.z, 0.0);
		f_position[start + 5] = vec4(fpos.x, fpos.y + 1.0, fpos.z, 0.0);
		for (uint i = 0; i < 6; i++)
		{
			f_normal[start + i] =  vec4(0.0, 0.0, -1.0, 0.0);
			f_tangent[start + i] = vec4(1.0, 0.0, 0.0, 0.0);
			f_indices[start + i] = start + i;
			f_color[start + i] = vec4(1.0);
			f_tcol[start + i] = vec4(1.0);
			f_thv[start + i] = vec4(0.0);
			f_thw[start + i] = vec4(0.0);
		}
		float texID = imageLoad(texture_ids, ivec2(curBLK.x, 1)).x;
		f_texcoord[start + 0] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 1] = vec4(1.0, 1.0, texID, 0.0);
		f_texcoord[start + 2] = vec4(0.0, 1.0, texID, 0.0);
		f_texcoord[start + 3] = vec4(0.0, 0.0, texID, 0.0);
		f_texcoord[start + 4] = vec4(1.0, 0.0, texID, 0.0);
		f_texcoord[start + 5] = vec4(1.0, 1.0, texID, 0.0);
		start += 6;
	}
}
#endif

// Helper for Voxels END
