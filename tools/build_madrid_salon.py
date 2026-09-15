"""
Builds the complete 3D environment for 'Salón de Madrid' (room_madrid_salon.glb)
Inspired by historic Madrid, private card salons, castilian elegance, and nocturnal atmosphere.
Designed for execution inside Blender 5.2 via MCP or command line.
"""

import bpy
import math
import os

# Create or switch to an isolated scene to strictly avoid polluting user scenes
scene_name = "MadridSalonScene"
if scene_name in bpy.data.scenes:
    bpy.data.scenes.remove(bpy.data.scenes[scene_name])
scene = bpy.data.scenes.new(scene_name)
bpy.context.window.scene = scene

# Deselect and clear scene objects
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

def make_mat(name, base_color, metallic=0.0, roughness=0.5, emission=(0,0,0,1), emission_strength=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    mat.diffuse_color = base_color
    bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = base_color
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if emission_strength > 0:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = emission
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = emission
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat

print("[Blender MCP] Building high-fidelity 'Salón de Madrid' (room_madrid_salon.glb)...")

# ==========================================================
# 1. PBR MATERIALS PALETTE (MADRID CASTILIAN PALETTE)
# ==========================================================
# Floor & Tilework
mat_limestone = make_mat("Madrid_LimestoneFloor", (0.86, 0.82, 0.74, 1.0), metallic=0.02, roughness=0.26)
mat_tile_border = make_mat("Madrid_TileBorder", (0.16, 0.08, 0.05, 1.0), metallic=0.05, roughness=0.35)
mat_talavera = make_mat("Madrid_TalaveraTile", (0.82, 0.78, 0.70, 1.0), metallic=0.08, roughness=0.18)

# Rug Under Table
mat_rug_velvet = make_mat("Madrid_RugVelvet", (0.24, 0.04, 0.07, 1.0), metallic=0.02, roughness=0.88)
mat_rug_gold = make_mat("Madrid_RugGoldBrocade", (0.82, 0.65, 0.22, 1.0), metallic=0.75, roughness=0.35)
mat_rug_garnet = make_mat("Madrid_RugGarnetLattice", (0.16, 0.02, 0.04, 1.0), metallic=0.02, roughness=0.90)

# Woods & Wall Finishes
mat_walnut = make_mat("Madrid_CarvedWalnut", (0.12, 0.06, 0.03, 1.0), metallic=0.0, roughness=0.36)
mat_burgundy_stucco = make_mat("Madrid_BurgundyStucco", (0.22, 0.06, 0.08, 1.0), metallic=0.0, roughness=0.76)
mat_arch_sandstone = make_mat("Madrid_ArchSandstone", (0.78, 0.72, 0.64, 1.0), metallic=0.0, roughness=0.62)
mat_hall_wall = make_mat("Madrid_HallwayWall", (0.35, 0.18, 0.10, 1.0), metallic=0.0, roughness=0.70)

# Metals & Luxury Trims
mat_aged_gold = make_mat("Madrid_AgedGold", (0.85, 0.68, 0.24, 1.0), metallic=0.88, roughness=0.24)
mat_wrought_iron = make_mat("Madrid_WroughtIron", (0.06, 0.06, 0.06, 1.0), metallic=0.78, roughness=0.45)
mat_brass = make_mat("Madrid_PolishedBrass", (0.88, 0.74, 0.30, 1.0), metallic=0.92, roughness=0.20)

# Bar Marble & Glassware
mat_emperador_marble = make_mat("Madrid_EmperadorMarble", (0.10, 0.07, 0.05, 1.0), metallic=0.08, roughness=0.14)
mat_cut_crystal = make_mat("Madrid_CutCrystal", (0.85, 0.92, 0.96, 1.0), metallic=0.15, roughness=0.05)
mat_rioja_wine = make_mat("Madrid_RiojaWineBottle", (0.08, 0.02, 0.03, 1.0), metallic=0.10, roughness=0.12)
mat_sherry = make_mat("Madrid_SherryDecanter", (0.75, 0.42, 0.08, 1.0), metallic=0.05, roughness=0.10)

# Lights & Emissives
mat_sconce_glow = make_mat("Madrid_AmberGlow", (1.0, 0.72, 0.32, 1.0), metallic=0.05, roughness=0.1,
                           emission=(1.0, 0.70, 0.28, 1.0), emission_strength=4.2)
mat_lantern_glow = make_mat("Madrid_LanternGlow", (1.0, 0.60, 0.20, 1.0), metallic=0.05, roughness=0.1,
                            emission=(1.0, 0.58, 0.18, 1.0), emission_strength=3.8)
mat_night_sky = make_mat("Madrid_NightSky", (0.04, 0.07, 0.16, 1.0), metallic=0.0, roughness=0.95)
mat_city_silhouette = make_mat("Madrid_CityRooftops", (0.02, 0.03, 0.08, 1.0), metallic=0.05, roughness=0.85)

# Paintings
mat_art_goya = make_mat("Madrid_ArtGoya", (0.32, 0.20, 0.12, 1.0), metallic=0.05, roughness=0.58)
mat_art_velazquez = make_mat("Madrid_ArtVelazquez", (0.24, 0.16, 0.18, 1.0), metallic=0.05, roughness=0.58)

# ==========================================================
# 2. FLOOR & ROYAL SPANISH RUG
# ==========================================================
# Main limestone floor
bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 1.5, -0.73))
floor = bpy.context.active_object
floor.name = "Madrid_Floor_Limestone"
floor.scale = (28.0, 18.0, 1.0)
floor.data.materials.append(mat_limestone)
bpy.ops.object.transform_apply(scale=True)

# Floor Perimeter Border in Dark Inlaid Walnut & Marble
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 1.5, -0.724))
f_border = bpy.context.active_object
f_border.name = "Madrid_Floor_InlayBorder"
f_border.scale = (25.2, 16.2, 0.012)
f_border.data.materials.append(mat_tile_border)
bpy.ops.object.transform_apply(scale=True)

# Grand Spanish Velvet & Brocade Table Rug
# Outer Burgundy Border
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.0, -0.718))
rug_outer = bpy.context.active_object
rug_outer.name = "Madrid_Rug_Outer"
rug_outer.scale = (14.6, 10.2, 0.016)
rug_outer.data.materials.append(mat_rug_velvet)
bpy.ops.object.transform_apply(scale=True)

# Gold Greek/Moorish Key Border
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.0, -0.714))
rug_key = bpy.context.active_object
rug_key.name = "Madrid_Rug_GoldKey"
rug_key.scale = (13.8, 9.4, 0.018)
rug_key.data.materials.append(mat_rug_gold)
bpy.ops.object.transform_apply(scale=True)

# Inner Garnet Velvet Field
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.0, -0.710))
rug_inner = bpy.context.active_object
rug_inner.name = "Madrid_Rug_GarnetField"
rug_inner.scale = (13.0, 8.6, 0.020)
rug_inner.data.materials.append(mat_rug_garnet)
bpy.ops.object.transform_apply(scale=True)

wall_y = 6.4

# ==========================================================
# 3. HERO BACK WALL: GRAND ARCHWAY & ILLUMINATED CORRIDOR
# ==========================================================
# ==========================================================
# 3. HERO BACK WALL: GRAND OPEN ARCHWAY & ILLUMINATED CORRIDOR
# ==========================================================
# The archway opening is 4.0m wide (from X = -2.0 to X = 2.0).
# Wall sections flanking the archway: Left (X = -8.0, width 12.0) & Right (X = 8.0, width 12.0)
for side, sx in [("Left", -8.0), ("Right", 8.0)]:
    # Baseboard
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sx, wall_y, -0.47))
    bb = bpy.context.active_object
    bb.name = f"Madrid_Baseboard_{side}"
    bb.scale = (12.0, 0.42, 0.52)
    bb.data.materials.append(mat_walnut)
    bpy.ops.object.transform_apply(scale=True)

    # Talavera Wainscoting Base Wall (Height 1.45m)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sx, wall_y, 0.25))
    ws = bpy.context.active_object
    ws.name = f"Madrid_TalaveraWainscoting_{side}"
    ws.scale = (12.0, 0.38, 1.45)
    ws.data.materials.append(mat_talavera)
    bpy.ops.object.transform_apply(scale=True)

    # Carved Walnut Dado Rail
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sx, wall_y - 0.08, 0.98))
    dd = bpy.context.active_object
    dd.name = f"Madrid_DadoRail_{side}"
    dd.scale = (12.0, 0.46, 0.14)
    dd.data.materials.append(mat_walnut)
    bpy.ops.object.transform_apply(scale=True)

    # Upper Back Wall: Burgundy Velvet Stucco
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sx, wall_y, 4.15))
    uw = bpy.context.active_object
    uw.name = f"Madrid_BackWall_Upper_{side}"
    uw.scale = (12.0, 0.35, 6.2)
    uw.data.materials.append(mat_burgundy_stucco)
    bpy.ops.object.transform_apply(scale=True)

# Top wall header directly spanning above the grand arch (from Z = 4.2 to 7.25)
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y, 5.75))
top_header = bpy.context.active_object
top_header.name = "Madrid_BackWall_ArchHeader"
top_header.scale = (4.0, 0.35, 3.0)
top_header.data.materials.append(mat_burgundy_stucco)
bpy.ops.object.transform_apply(scale=True)

# Grand Central Spanish Archway: Sandstone Pilasters flanking the opening
for px in [-2.2, 2.2]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, wall_y - 0.22, 1.85))
    col = bpy.context.active_object
    col.name = f"Madrid_ArchPilaster_{px}"
    col.scale = (0.64, 0.58, 4.65)
    col.data.materials.append(mat_arch_sandstone)
    bpy.ops.object.transform_apply(scale=True)
    # Pilaster Cap & Base Trim
    for cz in [-0.42, 4.15]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, wall_y - 0.26, cz))
        cap = bpy.context.active_object
        cap.name = f"Madrid_PilasterCap_{px}_{cz}"
        cap.scale = (0.76, 0.68, 0.16)
        cap.data.materials.append(mat_aged_gold)
        bpy.ops.object.transform_apply(scale=True)

# Archway Lintel Beam bridging the pilasters
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.22, 4.25))
arch_lintel = bpy.context.active_object
arch_lintel.name = "Madrid_Arch_Lintel"
arch_lintel.scale = (5.0, 0.62, 0.38)
arch_lintel.data.materials.append(mat_arch_sandstone)
bpy.ops.object.transform_apply(scale=True)

# Decorative Carved Arch Molding Ring above lintel
bpy.ops.mesh.primitive_torus_add(major_radius=2.0, minor_radius=0.18, major_segments=32, minor_segments=12, location=(0, wall_y - 0.24, 4.25))
arch_trim = bpy.context.active_object
arch_trim.name = "Madrid_GrandArch_Molding"
arch_trim.rotation_euler = (math.pi / 2, 0, 0)
arch_trim.data.materials.append(mat_arch_sandstone)

# Deep Recessed Corridor Hallway Beyond the Arch (Majestic architectural depth)
# Corridor Floor
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y + 3.0, -0.73))
cfloor = bpy.context.active_object
cfloor.name = "Madrid_Corridor_Floor"
cfloor.scale = (4.0, 6.0, 0.08)
cfloor.data.materials.append(mat_limestone)
bpy.ops.object.transform_apply(scale=True)

# Left Corridor Wall
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-2.0, wall_y + 3.0, 1.85))
cwall_l = bpy.context.active_object
cwall_l.name = "Madrid_Corridor_Wall_L"
cwall_l.scale = (0.2, 6.0, 5.0)
cwall_l.data.materials.append(mat_hall_wall)
bpy.ops.object.transform_apply(scale=True)

# Right Corridor Wall
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(2.0, wall_y + 3.0, 1.85))
cwall_r = bpy.context.active_object
cwall_r.name = "Madrid_Corridor_Wall_R"
cwall_r.scale = (0.2, 6.0, 5.0)
cwall_r.data.materials.append(mat_hall_wall)
bpy.ops.object.transform_apply(scale=True)

# End Corridor Back Wall & Ornate Spanish Arched Door
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y + 6.0, 1.85))
cwall_end = bpy.context.active_object
cwall_end.name = "Madrid_Corridor_Wall_End"
cwall_end.scale = (4.2, 0.2, 5.0)
cwall_end.data.materials.append(mat_hall_wall)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y + 5.92, 1.25))
cdoor = bpy.context.active_object
cdoor.name = "Madrid_Corridor_Door"
cdoor.scale = (1.8, 0.12, 3.2)
cdoor.data.materials.append(mat_walnut)
bpy.ops.object.transform_apply(scale=True)

# Amber Lantern suspended inside the hallway corridor
bpy.ops.mesh.primitive_cylinder_add(radius=0.20, depth=0.55, vertices=8, location=(0, wall_y + 2.8, 3.2))
hall_lantern = bpy.context.active_object
hall_lantern.name = "Madrid_HallwayLantern"
hall_lantern.data.materials.append(mat_lantern_glow)

# Castilian Coat of Arms (Escudo del Club de la Villa) above Arch Keystone
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.38, 5.85))
crest = bpy.context.active_object
crest.name = "Madrid_ClubDeLaVilla_Crest"
crest.scale = (1.4, 0.18, 1.4)
crest.data.materials.append(mat_aged_gold)
bpy.ops.object.transform_apply(scale=True)

# Flanking Ornate Boiserie Frames on Back Wall
for fx in [-5.6, 5.6]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(fx, wall_y - 0.12, 2.75))
    bframe = bpy.context.active_object
    bframe.name = f"Madrid_BackBoiserieFrame_{fx}"
    bframe.scale = (3.2, 0.10, 3.2)
    bframe.data.materials.append(mat_aged_gold)
    bpy.ops.object.transform_apply(scale=True)

    # Inset Masterpiece Paintings
    mat_art = mat_art_goya if fx < 0 else mat_art_velazquez
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(fx, wall_y - 0.18, 2.75))
    painting = bpy.context.active_object
    painting.name = f"Madrid_BackPainting_{fx}"
    painting.scale = (2.9, 2.9, 1.0)
    painting.rotation_euler = (math.pi / 2, 0, 0)
    painting.data.materials.append(mat_art)
    bpy.ops.object.transform_apply(scale=True, rotation=True)

# Sconce Torches on Back Wall (Castilian Wrought-Iron)
for sx in [-7.6, -3.8, 3.8, 7.6]:
    # Bracket
    bpy.ops.mesh.primitive_cylinder_add(radius=0.035, depth=0.42, vertices=12, location=(sx, wall_y - 0.32, 2.95))
    sb = bpy.context.active_object
    sb.name = f"Madrid_SconceBracket_{sx}"
    sb.rotation_euler = (math.pi / 3, 0, 0)
    sb.data.materials.append(mat_wrought_iron)
    # Torch cup & amber flame
    bpy.ops.mesh.primitive_cylinder_add(radius=0.09, depth=0.28, vertices=16, location=(sx, wall_y - 0.48, 3.20))
    st = bpy.context.active_object
    st.name = f"Madrid_SconceCup_{sx}"
    st.data.materials.append(mat_sconce_glow)

# ==========================================================
# 4. LEFT WALL: HISTORIC MADRID BODEGA & BAR COUNTER
# ==========================================================
left_x = -13.6
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(left_x, 1.5, 3.25))
l_wall = bpy.context.active_object
l_wall.name = "Madrid_LeftWall_Boiserie"
l_wall.scale = (0.50, 18.0, 8.0)
l_wall.data.materials.append(mat_walnut)
bpy.ops.object.transform_apply(scale=True)

# Sculpted Walnut Bar Counter with Dark Imperial Marble Top
bar_y = 1.2
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(left_x + 3.2, bar_y, 0.38))
bar_body = bpy.context.active_object
bar_body.name = "Madrid_BarCounter_Body"
bar_body.scale = (1.4, 7.5, 1.15)
bar_body.data.materials.append(mat_walnut)
bpy.ops.object.transform_apply(scale=True)

# Polished Emperador Marble Top
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(left_x + 3.2, bar_y, 0.98))
bar_top = bpy.context.active_object
bar_top.name = "Madrid_BarCounter_MarbleTop"
bar_top.scale = (1.55, 7.7, 0.08)
bar_top.data.materials.append(mat_emperador_marble)
bpy.ops.object.transform_apply(scale=True)

# Brass Footrail
bpy.ops.mesh.primitive_cylinder_add(radius=0.035, depth=7.4, vertices=16, location=(left_x + 4.05, bar_y, -0.55))
footrail = bpy.context.active_object
footrail.name = "Madrid_Bar_Footrail"
footrail.rotation_euler = (math.pi / 2, 0, 0)
footrail.data.materials.append(mat_brass)
bpy.ops.object.transform_apply(rotation=True)

# Back-Bar Shelving with Mirrors & Bottles
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(left_x + 0.65, bar_y, 2.65))
back_bar = bpy.context.active_object
back_bar.name = "Madrid_BackBar_Shelves"
back_bar.scale = (0.60, 7.2, 3.4)
back_bar.data.materials.append(mat_walnut)
bpy.ops.object.transform_apply(scale=True)

# Glassware & Wine Bottles on Back-Bar Shelves
for b_idx, by_pos in enumerate([-1.8, -0.6, 0.6, 1.8]):
    # Rioja Wine Bottle
    bpy.ops.mesh.primitive_cylinder_add(radius=0.07, depth=0.44, vertices=12, location=(left_x + 1.1, bar_y + by_pos - 0.15, 1.85))
    bot = bpy.context.active_object
    bot.name = f"Madrid_WineBottle_{b_idx}"
    bot.data.materials.append(mat_rioja_wine)

    # Sherry Decanter
    bpy.ops.mesh.primitive_cylinder_add(radius=0.09, depth=0.38, vertices=16, location=(left_x + 1.1, bar_y + by_pos + 0.15, 1.82))
    dec = bpy.context.active_object
    dec.name = f"Madrid_Decanter_{b_idx}"
    dec.data.materials.append(mat_sherry)

    # Crystal Goblets on Counter Top
    bpy.ops.mesh.primitive_cylinder_add(radius=0.05, depth=0.22, vertices=12, location=(left_x + 3.1, bar_y + by_pos, 1.12))
    glass = bpy.context.active_object
    glass.name = f"Madrid_Goblet_{b_idx}"
    glass.data.materials.append(mat_cut_crystal)

# ==========================================================
# 5. RIGHT WALL: ARCHED BALCONY WINDOWS & NOCTURNAL SKY
# ==========================================================
right_x = 13.6
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(right_x, 1.5, 3.25))
r_wall = bpy.context.active_object
r_wall.name = "Madrid_RightWall_Boiserie"
r_wall.scale = (0.50, 18.0, 8.0)
r_wall.data.materials.append(mat_burgundy_stucco)
bpy.ops.object.transform_apply(scale=True)

# Two Majestic Spanish Arched Balcony Windows (Rejas & Midnight Sky)
for w_idx, wy in enumerate([-1.8, 3.8]):
    # Outer Stone Frame
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(right_x - 0.22, wy, 2.75))
    w_frame = bpy.context.active_object
    w_frame.name = f"Madrid_WindowFrame_{w_idx}"
    w_frame.scale = (0.45, 3.2, 5.2)
    w_frame.data.materials.append(mat_arch_sandstone)
    bpy.ops.object.transform_apply(scale=True)

    # Deep Midnight Blue Night Sky Backdrop
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(right_x + 1.4, wy, 2.75))
    sky = bpy.context.active_object
    sky.name = f"Madrid_NightSky_{w_idx}"
    sky.scale = (1.0, 3.8, 5.6)
    sky.rotation_euler = (0, -math.pi / 2, 0)
    sky.data.materials.append(mat_night_sky)
    bpy.ops.object.transform_apply(scale=True, rotation=True)

    # City Rooftop Silhouette & Balcony Railing
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(right_x + 0.8, wy, 0.85))
    roof = bpy.context.active_object
    roof.name = f"Madrid_CityRooftops_{w_idx}"
    roof.scale = (0.2, 3.2, 1.4)
    roof.data.materials.append(mat_city_silhouette)
    bpy.ops.object.transform_apply(scale=True)

    # Wrought Iron Grille (Reja Madrileña)
    for rz in [0.8, 1.6, 2.4, 3.2, 4.0]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.022, depth=2.8, vertices=8, location=(right_x - 0.28, wy, rz))
        bar = bpy.context.active_object
        bar.name = f"Madrid_RejaBar_{w_idx}_{rz}"
        bar.rotation_euler = (math.pi / 2, 0, 0)
        bar.data.materials.append(mat_wrought_iron)
        bpy.ops.object.transform_apply(rotation=True)

    # Heavy Burgundy Velvet Draped Curtains
    for c_side, c_sign in [("Left", -1), ("Right", 1)]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.28, depth=5.0, vertices=12, location=(right_x - 0.45, wy + c_sign * 1.45, 2.65))
        curtain = bpy.context.active_object
        curtain.name = f"Madrid_Curtain_{w_idx}_{c_side}"
        curtain.data.materials.append(mat_rug_velvet)
        # Gold Tasseled Tieback
        bpy.ops.mesh.primitive_torus_add(major_radius=0.32, minor_radius=0.04, location=(right_x - 0.45, wy + c_sign * 1.45, 1.6))
        tie = bpy.context.active_object
        tie.name = f"Madrid_CurtainTie_{w_idx}_{c_side}"
        tie.data.materials.append(mat_aged_gold)

# ==========================================================
# 6. COFFERED CEILING (ARTESONADO MADRILEÑO) & CHANDELIER
# ==========================================================
# Ceiling Plane
bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 1.5, 7.35))
ceiling = bpy.context.active_object
ceiling.name = "Madrid_Ceiling_Artesonado"
ceiling.scale = (28.0, 18.0, 1.0)
ceiling.rotation_euler = (math.pi, 0, 0)
ceiling.data.materials.append(mat_walnut)
bpy.ops.object.transform_apply(scale=True, rotation=True)

# Heavy Cross Beams (Artesonado Grid)
for bz_y in [-5.0, -1.8, 1.8, 5.0]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, bz_y, 7.20))
    beam = bpy.context.active_object
    beam.name = f"Madrid_CeilingBeam_Y_{bz_y}"
    beam.scale = (27.6, 0.46, 0.32)
    beam.data.materials.append(mat_walnut)
    bpy.ops.object.transform_apply(scale=True)

for bx in [-9.0, -4.5, 0.0, 4.5, 9.0]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, 1.5, 7.20))
    beam = bpy.context.active_object
    beam.name = f"Madrid_CeilingBeam_X_{bx}"
    beam.scale = (0.46, 17.6, 0.32)
    beam.data.materials.append(mat_walnut)
    bpy.ops.object.transform_apply(scale=True)

    # Gold Carved Rosette at Beam Intersection
    for by in [-5.0, -1.8, 1.8, 5.0]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.18, depth=0.08, vertices=8, location=(bx, by, 7.02))
        rosette = bpy.context.active_object
        rosette.name = f"Madrid_Rosette_{bx}_{by}"
        rosette.data.materials.append(mat_aged_gold)

# Grand Spanish Wrought-Iron Chandelier (Directly above the central card table)
ch_x, ch_y, ch_z = 0.0, 0.0, 5.2

# Suspension Iron Rod & Chain
bpy.ops.mesh.primitive_cylinder_add(radius=0.035, depth=2.2, vertices=12, location=(ch_x, ch_y, ch_z + 1.1))
ch_rod = bpy.context.active_object
ch_rod.name = "Madrid_Chandelier_Rod"
ch_rod.data.materials.append(mat_wrought_iron)

# Circular Wrought-Iron Ring
bpy.ops.mesh.primitive_torus_add(major_radius=1.55, minor_radius=0.08, location=(ch_x, ch_y, ch_z))
ch_ring = bpy.context.active_object
ch_ring.name = "Madrid_Chandelier_Ring"
ch_ring.data.materials.append(mat_wrought_iron)

# 8 Forged Scroll Arms with Amber Candle Flames & Crystal Pendants
for a_idx in range(8):
    angle = a_idx * (2 * math.pi / 8)
    ax = ch_x + math.cos(angle) * 1.55
    ay = ch_y + math.sin(angle) * 1.55

    # Candle Sleeve & Amber Flame
    bpy.ops.mesh.primitive_cylinder_add(radius=0.055, depth=0.32, vertices=16, location=(ax, ay, ch_z + 0.28))
    candle = bpy.context.active_object
    candle.name = f"Madrid_Chandelier_Candle_{a_idx}"
    candle.data.materials.append(mat_sconce_glow)

    # Crystal Pendant Droplet
    bpy.ops.mesh.primitive_cone_add(radius1=0.06, depth=0.22, vertices=8, location=(ax, ay, ch_z - 0.20))
    pendant = bpy.context.active_object
    pendant.name = f"Madrid_Chandelier_Pendant_{a_idx}"
    pendant.rotation_euler = (math.pi, 0, 0)
    pendant.data.materials.append(mat_cut_crystal)

# ==========================================================
# 7. PROPS: COAT RACK, WALL CLOCK & SIDE CONSOLE
# ==========================================================
# Antique Spanish Carved Wall Clock on Left-Center Wall
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-9.8, wall_y - 0.22, 3.8))
clock_body = bpy.context.active_object
clock_body.name = "Madrid_WallClock_Body"
clock_body.scale = (0.75, 0.24, 1.35)
clock_body.data.materials.append(mat_walnut)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cylinder_add(radius=0.26, depth=0.06, vertices=24, location=(-9.8, wall_y - 0.35, 3.8))
clock_dial = bpy.context.active_object
clock_dial.name = "Madrid_WallClock_Dial"
clock_dial.rotation_euler = (math.pi / 2, 0, 0)
clock_dial.data.materials.append(mat_aged_gold)

# Brass Stand & Spanish Fedora Silhouette near entrance/corner
bpy.ops.mesh.primitive_cylinder_add(radius=0.035, depth=1.95, vertices=12, location=(8.8, wall_y - 1.2, 0.25))
coat_rack = bpy.context.active_object
coat_rack.name = "Madrid_CoatRack"
coat_rack.data.materials.append(mat_brass)

# ==========================================================
# 8. EXPORT TO GLB
# ==========================================================
out_dir = os.path.abspath("assets/models/club")
os.makedirs(out_dir, exist_ok=True)
glb_path = os.path.join(out_dir, "room_madrid_salon.glb")

# Ensure all objects in this scene are selected and visible for export
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=glb_path, export_format='GLB', use_selection=True, export_apply=True)
print(f"[Blender MCP] Successfully exported room_madrid_salon.glb ({os.path.getsize(glb_path)} bytes) to {glb_path}!")

# ==========================================================
# 9. LIGHTING & CAMERA FOR VIEWPORT CAPTURE EVIDENCE
# ==========================================================
bpy.ops.object.light_add(type='POINT', location=(0, 0, 4.8))
key_light = bpy.context.active_object
key_light.name = "Madrid_Preview_ChandelierLight"
key_light.data.energy = 1650
key_light.data.color = (1.0, 0.85, 0.52)

bpy.ops.object.light_add(type='POINT', location=(0, wall_y + 2.5, 3.2))
hall_light = bpy.context.active_object
hall_light.name = "Madrid_Preview_HallLight"
hall_light.data.energy = 680
hall_light.data.color = (1.0, 0.65, 0.25)

# Camera looking at the central salon, grand archway and corridor depth
bpy.ops.object.camera_add(location=(0, -5.5, 1.35))
cam = bpy.context.active_object
cam.name = "Madrid_RenderCamera"
cam.rotation_euler = (math.radians(78), 0, 0)
cam.data.lens = 20
bpy.context.scene.camera = cam

for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        for space in area.spaces:
            if space.type == 'VIEW_3D':
                space.shading.type = 'MATERIAL'
                space.overlay.show_overlays = False
                space.region_3d.view_perspective = 'CAMERA'

print("[Blender MCP] 'Salón de Madrid' completed and configured for capture!")
