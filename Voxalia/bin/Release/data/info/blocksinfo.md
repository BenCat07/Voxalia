# Blocks Info

- TODO: Information here!
- TODO: This needs to be replacedby something more sane, seriously.
- Generally do not edit the debug block all - its exact texture values are expected to some degree by the code.

### Textures

- Voxalia texture list - MAT=texture OR MAT=t1,t2,...%rate - MAT can be name OR # (A number) OR m# (total minus the number)
- Optionally add to the end $normal_texture OR $nt1,nt2,...%rate
- Optionally add to the end &specularity_texture*reflectivity_texture!glowing_texture@refraction_eta_texture OR &st1,st2,...*rt1,rt1,...!gt1,gt2,...@ret1,ret2,...%rate
- Note that specular/reflectivity/glowing/refraction_eta share an internal position, and if you have an uneven numbers of textures for the three, the remainder will be filled in black.
- Note that the the primary texture animation and the normal texture animation and the help texture animation can desynchronize if all three are not set to exactly the same rate.
