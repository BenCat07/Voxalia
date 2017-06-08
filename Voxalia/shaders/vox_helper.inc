
// Helper for Voxels START

// Requires glnoise.inc
float snoise(in vec3 v);
float snoise2(in vec3 v);

const vec4 tcolors_val[] = vec4[](
	vec4(1.0, 1.0, 1.0, 1.0), // WHITE = 0
	vec4(0.02, 0.02, 0.02, 1.0), // BLACK = 1
	vec4(0.0, 1.0, 0.0, 1.0), // GREEN = 2
	vec4(0.0, 0.0, 1.0, 1.0), // BLUE = 3
	vec4(1.0, 0.0, 0.0, 1.0), // RED = 4
	vec4(1.0, 0.0, 1.0, 1.0), // MAGENTA = 5
	vec4(1.0, 1.0, 0.0, 1.0), // YELLOW = 6
	vec4(0.0, 1.0, 1.0, 1.0), // CYAN = 7
	vec4(0.0, 0.5, 0.0, 1.0), // DARK_GREEN = 8
	vec4(0.0, 0.0, 0.5, 1.0), // DARK_BLUE = 9
	vec4(0.5, 0.0, 0.0, 1.0), // DARK_RED = 10
	vec4(0.5, 1.0, 0.5, 1.0), // LIGHT_GREEN = 11
	vec4(0.5, 0.5, 1.0, 1.0), // LIGHT_BLUE = 12
	vec4(1.0, 0.5, 0.5, 1.0), // LIGHT_RED = 13
	vec4(0.5, 0.5, 0.5, 1.0), // GRAY = 14
	vec4(0.75, 0.75, 0.75, 1.0), // LIGHT_GRAY = 15
	vec4(0.25, 0.25, 0.25, 1.0), // DARK_GRAY = 16
	vec4(0.5, 0.0, 0.5, 1.0), // DARK_MAGENTA = 17
	vec4(0.5, 0.5, 0.0, 1.0), // DARK_YELLOW = 18
	vec4(0.0, 0.5, 0.5, 1.0), // DARK_CYAN = 19
	vec4(1.0, 0.5, 1.0, 1.0), // LIGHT_MAGENTA = 20
	vec4(1.0, 1.0, 0.5, 1.0), // LIGHT_YELLOW = 21
	vec4(0.5, 1.0, 1.0, 1.0), // LIGHT_CYAN = 22
	vec4(1.0, 0.5, 0.0, 1.0), // ORANGE = 23
	vec4(0.5, 0.25, 0.0, 1.0), // BROWN = 24
	vec4(0.5, 0.0, 1.0, 1.0), // PURPLE = 25
	vec4(1.0, 0.5, 1.0, 1.0), // PINK = 26
	vec4(0.5, 1.0, 0.0, 1.0), // LIME = 27
	vec4(0.0, 0.5, 1.0, 1.0), // SKY_BLUE = 28
	vec4(0.125, 0.125, 0.125, 1.0), // VERY_DARK_GRAY = 29
	vec4(1.25, 1.25, 1.25, 1.0), // SLIGHTLY_BRIGHT = 30
	vec4(1.5, 1.5, 1.5, 1.0), // BRIGHT = 31
	vec4(2.0, 2.0, 2.0, 1.0), // VERY_BRIGHT = 32
	vec4(0.0, 1.0, 0.0, 0.5), // TRANSPARENT_GREEN = 33
	vec4(0.0, 0.0, 1.0, 0.5), // TRANSPARENT_BLUE = 34
	vec4(1.0, 0.0, 0.0, 0.5), // TRANSPARENT_RED = 35
	vec4(1.0, 0.0, 1.0, 0.5), // TRANSPARENT_MAGENTA = 36
	vec4(1.0, 1.0, 0.0, 0.5), // TRANSPARENT_YELLOW = 37
	vec4(0.0, 1.0, 1.0, 0.5), // TRANSPARENT_CYAN = 38
	vec4(1.0, 1.0, 1.0, 0.75), // SLIGHTLY_TRANSPARENT = 39
	vec4(1.0, 1.0, 1.0, 0.5), // TRANSPARENT = 40
	vec4(1.0, 1.0, 1.0, 0.25), // VERY_TRANSPARENT = 41
	vec4(0.0, 1.0, 0.0, 0.01), // LIGHT_STROBE_GREEN = 42
	vec4(0.0, 0.0, 1.0, 0.01), // LIGHT_STROBE_BLUE = 43
	vec4(1.0, 0.0, 0.0, 0.01), // LIGHT_STROBE_RED = 44
	vec4(1.0, 0.0, 1.0, 0.01), // LIGHT_STROBE_MAGENTA = 45
	vec4(1.0, 1.0, 0.0, 0.01), // LIGHT_STROBE_YELLOW = 46
	vec4(0.0, 1.0, 1.0, 0.01), // LIGHT_STROBE_CYAN = 47
	vec4(1.0, 1.0, 1.0, 0.0), // STROBE_WHITE = 48
	vec4(0.0, 1.0, 0.0, 0.0), // STROBE_GREEN = 49
	vec4(0.0, 0.0, 1.0, 0.0), // STROBE_BLUE = 50
	vec4(1.0, 0.0, 0.0, 0.0), // STROBE_RED = 51
	vec4(1.0, 0.0, 1.0, 0.0), // STROBE_MAGENTA = 52
	vec4(1.0, 1.0, 0.0, 0.0), // STROBE_YELLOW = 53
	vec4(0.0, 1.0, 1.0, 0.0), // STROBE_CYAN = 54
	vec4(0.0, 0.0, 0.0, 0.0), // MAGIC = 55
	vec4(0.5, 0.0, 0.0, 0.0), // OLD_MAGIC = 56
	vec4(0.5, 0.0, 0.5, 0.0), // RAINBOW = 57
	vec4(0.0, 0.5, 0.0, 0.0), // BLUR = 58
	vec4(0.4, 0.5, 0.5, 0.0), // CRACKS = 59
	vec4(0.31, 0.5, 0.5, 0.0), // CRACKS_LIGHT = 60
	vec4(0.5, 0.5, 0.5, 0.0), // CRACKS_DARK = 61
	vec4(0.5, 0.5, (145.0 / 255.0), 0.0), // INVERT = 62
	vec4((145.0 / 255.0), 0.5, 0.5, 0.0) // SHINE = 63
);

uint blockPaint(in uint bpnt)
{
	return bpnt & 63;
}

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

bool rendersAtAll(in uint mat)
{
	return imageLoad(texture_ids, ivec2(blockMat(mat), 6 * 7)).x > 0.5;
}

bool matHasAnyOpaque(in uint mat)
{
	return imageLoad(texture_ids, ivec2(blockMat(mat), 6 * 7 + 2)).x > 0.5;
}

bool matIsOpaque(in uint mat)
{
	// TODO: Transparent painted blocks are NOT opaque!
	return imageLoad(texture_ids, ivec2(blockMat(mat), 6 * 7 + 1)).x > 0.5;
}

#if MCM_LIGHTS
#else
bool shapeCovers(in uint shapeRel, in uint shapeC, in uint sideC)
{
	uint t_c = uint(uintBitsToFloat(f_shapes[shapeC * 12 + sideC]));
	uint t_rel = uint(uintBitsToFloat(f_shapes[shapeRel * 12 + 6 + (sideC % 2 == 0 ? sideC + 1 : sideC - 1)]));
	if (t_c == 3)
	{
		return 0 == 1;
	}
	if (t_rel == 0)
	{
		return 0 == 1;
	}
	if (t_rel == 2)
	{
		return t_c == 2;
	}
	return t_rel == 1;
}

bool isOpaque(in uvec4 blk, in uvec4 curBLK, in uint sideC)
{
	uint mat = blockMat(blk.x);
	// TODO: paint color check!
	return blockDamage(blk.x) == 0 && (matIsOpaque(mat) || mat == blockMat(curBLK.x))
			&& shapeCovers(blk.z, curBLK.z, sideC);
}

uint shapeIndex(in uvec4 curBLK, in uvec3 pos)
{
	uvec4 cbXP = pos.x + 1 == gl_WorkGroupSize.x ? posses[blkSize() * 2 + blockID(uvec3(0, pos.y, pos.z))] : posses[blockID(uvec3(pos.x + 1, pos.y, pos.z))];
	uvec4 cbXM = pos.x == 0 ? posses[blkSize() * 1 + blockID(uvec3(gl_WorkGroupSize.x - 1, pos.y, pos.z))] : posses[blockID(uvec3(pos.x - 1, pos.y, pos.z))];
	uvec4 cbYP = pos.y + 1 == gl_NumWorkGroups.y ? posses[blkSize() * 4 + blockID(uvec3(pos.x, 0, pos.z))] : posses[blockID(uvec3(pos.x, pos.y + 1, pos.z))];
	uvec4 cbYM = pos.y == 0 ? posses[blkSize() * 3 + blockID(uvec3(pos.x, gl_NumWorkGroups.y - 1, pos.z))] : posses[blockID(uvec3(pos.x, pos.y - 1, pos.z))];
	uvec4 cbZP = pos.z + 1 == gl_WorkGroupSize.z ? posses[blkSize() * 6 + blockID(uvec3(pos.x, pos.y, 0))] : posses[blockID(uvec3(pos.x, pos.y, pos.z + 1))];
	uvec4 cbZM = pos.z == 0 ? posses[blkSize() * 5 + blockID(uvec3(pos.x, pos.y, gl_WorkGroupSize.z - 1))] : posses[blockID(uvec3(pos.x, pos.y, pos.z - 1))];
	uint sh_subdat = (isOpaque(cbXP, curBLK, 0) ? 1 : 0) | (isOpaque(cbXM, curBLK, 1) ? 2 : 0)
				   | (isOpaque(cbYP, curBLK, 2) ? 4 : 0) | (isOpaque(cbYM, curBLK, 3) ? 8 : 0)
				   | (isOpaque(cbZP, curBLK, 4) ? 16 : 0)| (isOpaque(cbZM, curBLK, 5) ? 32 : 0);
	uint tid = (256 * 12) + curBLK.z * (64 * 4) + sh_subdat * 4 + blockDamage(curBLK.x);
	return f_shapes[tid];
}
#endif

#if MCM_LIGHTS
vec4 getLightFor(in uint mat)
{
	mat = blockMat(mat);
	return vec4(imageLoad(texture_ids, ivec2(mat, 6 * 7 + 3)).x, imageLoad(texture_ids, ivec2(mat, 6 * 7 + 4)).x,
			imageLoad(texture_ids, ivec2(mat, 6 * 7 + 5)).x, imageLoad(texture_ids, ivec2(mat, 6 * 7 + 6)).x);
}
#else

int count_visible_verts(in uvec3 pos)
{
	uint id = blockID(pos);
	uvec4 curBLK = posses[id];
	if (!rendersAtAll(curBLK.x))
	{
		return 0;
	}
#if MCM_SOLIDONLY
	if (!matHasAnyOpaque(curBLK.x))
	{
		return 0;
	}
#else
#if MCM_TRANSP
	if (matIsOpaque(curBLK.x))
	{
		return 0;
	}
#endif
#endif
	uint shInd = shapeIndex(curBLK, pos);
	return int(f_shapes[shInd]);
}
#endif

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

uvec4 getAtRel(in uvec3 pos)
{
	uvec3 dup = pos;
	int mult = 0;
	// TODO: Account for literal corner cases! (ZPXP, etc.)
	if (pos.x >= gl_WorkGroupSize.x)
	{
		mult = 2;
		dup.x -= gl_WorkGroupSize.x;
	}
	if (pos.y >= gl_NumWorkGroups.y)
	{
		mult = 4;
		dup.y -= gl_NumWorkGroups.y;
	}
	if (pos.z >= gl_WorkGroupSize.z)
	{
		mult = 6;
		dup.z -= gl_WorkGroupSize.z;
	}
	if (pos.x < 0)
	{
		mult = 1;
		dup.x += gl_WorkGroupSize.x;
	}
	if (pos.y < 0)
	{
		mult = 3;
		dup.y += gl_NumWorkGroups.y;
	}
	if (pos.z < 0)
	{
		mult = 5;
		dup.z += gl_WorkGroupSize.z;
	}
	return posses[blkSize() * mult + blockID(dup)];
}

#if MODE_TWO
vec3 lightsFromBlocksFor(in vec3 fpos, in vec3 fnorm)
{
	uint maxV = f_lightinfos[0];
	vec3 absPos = fpos;// + vec3(MCM_VOX_COUNT, MCM_VOX_COUNT, MCM_VOX_COUNT);
	vec3 colRel = vec3(0.0);
	for (uint i = 0; i < maxV; i++)
	{
		uint idT = i * 7 + 1;
		uvec3 posAbs = uvec3(f_lightinfos[idT + 0], f_lightinfos[idT + 1], f_lightinfos[idT + 2]);
		float dist = uintBitsToFloat(f_lightinfos[idT + 6]);
		vec3 relDist = absPos - vec3(posAbs);
		float distSq = dot(relDist, relDist);
		if (dist * dist >= distSq)
		{
			vec3 col = vec3(uintBitsToFloat(f_lightinfos[idT + 3]), uintBitsToFloat(f_lightinfos[idT + 4]), uintBitsToFloat(f_lightinfos[idT + 5]));
			float resDist = max(sqrt(distSq), 0.1);
			vec3 relNorm = relDist * (1.0 / resDist);
			float norm_lit = distSq < 4.0 ? 1.0 : max(dot(fnorm, -relNorm), 0.05);
			// TODO: Modify by blocks-in-the-way!
			colRel += col * (1.0 - (resDist / dist)) * norm_lit;
		}
	}
	return colRel;
}
#endif

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
		int inder = shInd + 1 + vertCount * 0 + o_i;
		vec3 v1 = (fpos + vec3(uintBitsToFloat(f_shapes[inder]), uintBitsToFloat(f_shapes[inder + 1]), uintBitsToFloat(f_shapes[inder + 2]))) * (30.0 / MCM_VOX_COUNT);
		vec3 v2 = (fpos + vec3(uintBitsToFloat(f_shapes[inder + 3]), uintBitsToFloat(f_shapes[inder + 4]), uintBitsToFloat(f_shapes[inder + 5]))) * (30.0 / MCM_VOX_COUNT);
		vec3 v3 = (fpos + vec3(uintBitsToFloat(f_shapes[inder + 6]), uintBitsToFloat(f_shapes[inder + 7]), uintBitsToFloat(f_shapes[inder + 8]))) * (30.0 / MCM_VOX_COUNT);
		inder = shInd + 1 + vertCount * 3 + o_i;
		vec3 n1 = vec3(uintBitsToFloat(f_shapes[inder]), uintBitsToFloat(f_shapes[inder + 1]), uintBitsToFloat(f_shapes[inder + 2]));
		vec3 n2 = vec3(uintBitsToFloat(f_shapes[inder + 3]), uintBitsToFloat(f_shapes[inder + 4]), uintBitsToFloat(f_shapes[inder + 5]));
		vec3 n3 = vec3(uintBitsToFloat(f_shapes[inder + 6]), uintBitsToFloat(f_shapes[inder + 7]), uintBitsToFloat(f_shapes[inder + 8]));
#if MODE_ONE
		inder = shInd + 1 + vertCount * 6 + o_i;
		vec3 t1 = vec3(uintBitsToFloat(f_shapes[inder]), uintBitsToFloat(f_shapes[inder + 1]), uintBitsToFloat(f_shapes[inder + 2]));
		vec3 t2 = vec3(uintBitsToFloat(f_shapes[inder + 3]), uintBitsToFloat(f_shapes[inder + 4]), uintBitsToFloat(f_shapes[inder + 5]));
		vec3 t3 = vec3(uintBitsToFloat(f_shapes[inder + 6]), uintBitsToFloat(f_shapes[inder + 7]), uintBitsToFloat(f_shapes[inder + 8]));
		f_position[start + it3 + 0] = vec4(v1, 0.0);
		f_position[start + it3 + 1] = vec4(v2, 0.0);
		f_position[start + it3 + 2] = vec4(v3, 0.0);
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
		uvec4 p_c = n1.z < 0.0 ? curBLK : (n1.x > 0.6 ? getAtRel(uvec3(pos.x + 1, pos.y, pos.z + 1)) :
				(n1.x < -0.6 ? getAtRel(uvec3(pos.x - 1, pos.y, pos.z + 1)) :
				(n1.y > 0.6 ? getAtRel(uvec3(pos.x, pos.y + 1, pos.z + 1)) :
				(n1.y < -0.6 ? getAtRel(uvec3(pos.x, pos.y - 1, pos.z + 1)) :
				getAtRel(uvec3(pos.x, pos.y, pos.z + 1))))));
		f_color[start + it3 + 0] = vec4(vec3(float(p_c.y) / 255.0) + lightsFromBlocksFor(v1, n1), 1.0);
		p_c = n2.z < 0.0 ? curBLK : (n2.x > 0.6 ? getAtRel(uvec3(pos.x + 1, pos.y, pos.z + 1)) :
				(n2.x < -0.6 ? getAtRel(uvec3(pos.x - 1, pos.y, pos.z + 1)) :
				(n2.y > 0.6 ? getAtRel(uvec3(pos.x, pos.y + 1, pos.z + 1)) :
				(n2.y < -0.6 ? getAtRel(uvec3(pos.x, pos.y - 1, pos.z + 1)) :
				getAtRel(uvec3(pos.x, pos.y, pos.z + 1))))));
		f_color[start + it3 + 1] = vec4(vec3(float(p_c.y) / 255.0) + lightsFromBlocksFor(v2, n2), 1.0);
		p_c = n3.z < 0.0 ? curBLK : (n3.x > 0.6 ? getAtRel(uvec3(pos.x + 1, pos.y, pos.z + 1)) :
				(n3.x < -0.6 ? getAtRel(uvec3(pos.x - 1, pos.y, pos.z + 1)) :
				(n3.y > 0.6 ? getAtRel(uvec3(pos.x, pos.y + 1, pos.z + 1)) :
				(n3.y < -0.6 ? getAtRel(uvec3(pos.x, pos.y - 1, pos.z + 1)) :
				getAtRel(uvec3(pos.x, pos.y, pos.z + 1))))));
		f_color[start + it3 + 2] = vec4(vec3(float(p_c.y) / 255.0) + lightsFromBlocksFor(v3, n3), 1.0);
		vec4 r_tcol = tcolors_val[blockPaint(curBLK.w)];
		f_indices[start + it3] = start + it3;
		f_tcol[start + it3] = r_tcol;
		f_indices[start + it3 + 1] = start + it3 + 1;
		f_tcol[start + it3 + 1] = r_tcol;
		f_indices[start + it3 + 2] = start + it3 + 2;
		f_tcol[start + it3 + 2] = r_tcol;
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
