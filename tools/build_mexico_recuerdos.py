"""Blender 5.2: Stylized High-Fidelity 3D Environment - 'La Mesa de los Recuerdos' (Día de Muertos).
Builds the Mexican colonial hacienda courtyard with tiered Altar de Ofrendas, cempasúchil marigold paths,
papel picado garlands, rustic wooden beams, colonial archways, and moonlit pueblo vista.
Exports assets/models/club/room_mexico_recuerdos.glb.
"""
import bpy
import math
import os

print("[Blender MCP] Building 'La Mesa de los Recuerdos' (room_mexico_recuerdos.glb)...")

# 1. Create and isolate temporary scene
scene_name = "MexicoRecuerdosScene"
if scene_name in bpy.data.scenes:
    bpy.data.scenes.remove(bpy.data.scenes[scene_name])
scene = bpy.data.scenes.new(scene_name)
bpy.context.window.scene = scene

# Deselect all
bpy.ops.object.select_all(action='DESELECT')

# PBR Material Helper
def create_pbr_mat(name, base_rgb, roughness=0.75, metallic=0.0, emission_rgb=None, emission_strength=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*base_rgb, 1.0)
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
        if emission_rgb:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = (*emission_rgb, 1.0)
                bsdf.inputs["Emission Strength"].default_value = emission_strength
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = (*emission_rgb, 1.0)
    return mat

# Palette definitions
mat_stone_patio       = create_pbr_mat("Mat_StonePatio", (0.68, 0.54, 0.42), roughness=0.88)
mat_terracotta_brick  = create_pbr_mat("Mat_TerracottaBrick", (0.58, 0.24, 0.14), roughness=0.82)
mat_stucco_terracotta = create_pbr_mat("Mat_StuccoTerracotta", (0.60, 0.28, 0.18), roughness=0.90)
mat_stucco_ochre      = create_pbr_mat("Mat_StuccoOchre", (0.72, 0.48, 0.20), roughness=0.88)
mat_rustic_wood       = create_pbr_mat("Mat_RusticWood", (0.24, 0.14, 0.08), roughness=0.72)
mat_aged_wood         = create_pbr_mat("Mat_AgedWood", (0.18, 0.11, 0.07), roughness=0.76)
mat_cempasuchil       = create_pbr_mat("Mat_Cempasuchil", (0.96, 0.46, 0.06), roughness=0.70)
mat_cempasuchil_gold  = create_pbr_mat("Mat_CempasuchilGold", (0.98, 0.68, 0.08), roughness=0.68)
mat_cempasuchil_leaf  = create_pbr_mat("Mat_CempasuchilLeaf", (0.12, 0.32, 0.14), roughness=0.65)
mat_altar_linen       = create_pbr_mat("Mat_AltarLinen", (0.92, 0.90, 0.84), roughness=0.60)
mat_candle_wax        = create_pbr_mat("Mat_CandleWax", (0.95, 0.92, 0.84), roughness=0.38)
mat_flame_glow        = create_pbr_mat("Mat_FlameGlow", (1.0, 0.65, 0.15), roughness=0.20, emission_rgb=(1.0, 0.62, 0.12), emission_strength=4.8)
mat_papel_magenta     = create_pbr_mat("Mat_PapelMagenta", (0.78, 0.14, 0.40), roughness=0.70)
mat_papel_purple      = create_pbr_mat("Mat_PapelPurple", (0.44, 0.14, 0.52), roughness=0.70)
mat_papel_gold        = create_pbr_mat("Mat_PapelGold", (0.92, 0.62, 0.12), roughness=0.70)
mat_papel_turquoise   = create_pbr_mat("Mat_PapelTurquoise", (0.10, 0.62, 0.60), roughness=0.70)
mat_wrought_iron      = create_pbr_mat("Mat_WroughtIron", (0.12, 0.12, 0.12), roughness=0.45, metallic=0.88)
mat_tejas             = create_pbr_mat("Mat_Tejas", (0.64, 0.26, 0.13), roughness=0.86)
mat_nocturne_sky      = create_pbr_mat("Mat_NocturneSky", (0.05, 0.09, 0.16), roughness=0.95, emission_rgb=(0.04, 0.08, 0.15), emission_strength=0.8)
mat_guitar_wood       = create_pbr_mat("Mat_GuitarWood", (0.52, 0.26, 0.12), roughness=0.35)
mat_talavera_pottery  = create_pbr_mat("Mat_TalaveraPottery", (0.88, 0.85, 0.80), roughness=0.28)
mat_barro_negro       = create_pbr_mat("Mat_BarroNegro", (0.08, 0.08, 0.09), roughness=0.32, metallic=0.25)
mat_pan_muerto        = create_pbr_mat("Mat_PanDeMuerto", (0.68, 0.42, 0.22), roughness=0.80)
mat_serape_rug        = create_pbr_mat("Mat_SerapeRug", (0.52, 0.16, 0.18), roughness=0.85)

# Floor height in Godot is Z = -0.73m
floor_z = -0.73

# ==========================================================
# 1. HACIENDA COURTYARD STONE & BRICK FLOOR
# ==========================================================
# Main Cobbled Stone Floor
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, floor_z - 0.05))
floor_stone = bpy.context.active_object
floor_stone.name = "Mexico_Floor_CobbleStone"
floor_stone.scale = (28.0, 22.0, 0.10)
floor_stone.data.materials.append(mat_stone_patio)
bpy.ops.object.transform_apply(scale=True)

# Terracotta Brick Perimeter Border
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, floor_z - 0.03))
floor_border = bpy.context.active_object
floor_border.name = "Mexico_Floor_TerracottaBorder"
floor_border.scale = (18.5, 15.5, 0.08)
floor_border.data.materials.append(mat_terracotta_brick)
bpy.ops.object.transform_apply(scale=True)

# Inner Courtyard Terrace
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, floor_z - 0.01))
floor_terrace = bpy.context.active_object
floor_terrace.name = "Mexico_Floor_InnerTerrace"
floor_terrace.scale = (17.5, 14.5, 0.06)
floor_terrace.data.materials.append(mat_stone_patio)
bpy.ops.object.transform_apply(scale=True)

# Traditional Artisanal Woven Serape Rug under the table
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, floor_z + 0.012))
rug_serape = bpy.context.active_object
rug_serape.name = "Mexico_Rug_Serape"
rug_serape.scale = (6.4, 5.2, 0.024)
rug_serape.data.materials.append(mat_serape_rug)
bpy.ops.object.transform_apply(scale=True)

# Rug decorative geometric border strips
for sy in [-2.45, 2.45]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, sy, floor_z + 0.025))
    fringe = bpy.context.active_object
    fringe.name = f"Mexico_Rug_Fringe_{sy}"
    fringe.scale = (6.2, 0.18, 0.01)
    fringe.data.materials.append(mat_cempasuchil_gold)
    bpy.ops.object.transform_apply(scale=True)

# ==========================================================
# 2. CAMINO DE CEMPASÚCHIL (FLOWER PETAL PATHWAY)
# ==========================================================
# Main petal path leading from south entrance toward the table, and table to altar
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 2.9, floor_z + 0.018))
petal_path_north = bpy.context.active_object
petal_path_north.name = "Mexico_PetalPath_North"
petal_path_north.scale = (1.9, 3.4, 0.012)
petal_path_north.data.materials.append(mat_cempasuchil)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -3.2, floor_z + 0.018))
petal_path_south = bpy.context.active_object
petal_path_south.name = "Mexico_PetalPath_South"
petal_path_south.scale = (1.6, 2.8, 0.012)
petal_path_south.data.materials.append(mat_cempasuchil)
bpy.ops.object.transform_apply(scale=True)

# Scattered flower patches framing the path and table corners
for cx, cy in [(-2.8, 2.2), (2.8, 2.2), (-2.8, -2.2), (2.8, -2.2), (0.0, 4.8)]:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.65, depth=0.015, vertices=16, location=(cx, cy, floor_z + 0.02))
    petals = bpy.context.active_object
    petals.name = f"Mexico_PetalPatch_{cx}_{cy}"
    petals.data.materials.append(mat_cempasuchil_gold)

# ==========================================================
# 3. HERO ASSET: GRAND TIERED ALTAR DE OFRENDAS
# ==========================================================
altar_y = 6.2

# Tier 1 (Base Platform): 6.8m wide, 1.4m deep, 0.55m high
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, altar_y, floor_z + 0.28))
altar_t1 = bpy.context.active_object
altar_t1.name = "Mexico_Altar_Tier1"
altar_t1.scale = (6.8, 1.4, 0.56)
altar_t1.data.materials.append(mat_rustic_wood)
bpy.ops.object.transform_apply(scale=True)

# Tier 1 White Lace Tablecloth
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, altar_y - 0.05, floor_z + 0.57))
linen_t1 = bpy.context.active_object
linen_t1.name = "Mexico_Altar_Linen1"
linen_t1.scale = (6.9, 1.35, 0.04)
linen_t1.data.materials.append(mat_altar_linen)
bpy.ops.object.transform_apply(scale=True)

# Tier 2 (Middle Platform): 4.8m wide, 1.1m deep, 0.55m high
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, altar_y + 0.25, floor_z + 0.84))
altar_t2 = bpy.context.active_object
altar_t2.name = "Mexico_Altar_Tier2"
altar_t2.scale = (4.8, 1.0, 0.56)
altar_t2.data.materials.append(mat_rustic_wood)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, altar_y + 0.22, floor_z + 1.13))
linen_t2 = bpy.context.active_object
linen_t2.name = "Mexico_Altar_Linen2"
linen_t2.scale = (4.9, 0.95, 0.04)
linen_t2.data.materials.append(mat_altar_linen)
bpy.ops.object.transform_apply(scale=True)

# Tier 3 (Top Platform): 3.2m wide, 0.8m deep, 0.55m high
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, altar_y + 0.45, floor_z + 1.40))
altar_t3 = bpy.context.active_object
altar_t3.name = "Mexico_Altar_Tier3"
altar_t3.scale = (3.2, 0.75, 0.56)
altar_t3.data.materials.append(mat_rustic_wood)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, altar_y + 0.42, floor_z + 1.69))
linen_t3 = bpy.context.active_object
linen_t3.name = "Mexico_Altar_Linen3"
linen_t3.scale = (3.3, 0.70, 0.04)
linen_t3.data.materials.append(mat_altar_linen)
bpy.ops.object.transform_apply(scale=True)

# Grand Floral Arch above the Altar (Arch of Cempasúchil)
bpy.ops.mesh.primitive_torus_add(major_radius=1.85, minor_radius=0.22, major_segments=32, minor_segments=12, location=(0, altar_y + 0.65, floor_z + 2.5))
arch_flowers = bpy.context.active_object
arch_flowers.name = "Mexico_Altar_FlowerArch"
arch_flowers.rotation_euler = (math.pi / 2, 0, 0)
arch_flowers.data.materials.append(mat_cempasuchil)

# Golden Floral Rosette at Keystone of Arch
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.35, location=(0, altar_y + 0.60, floor_z + 4.35))
keystone_flower = bpy.context.active_object
keystone_flower.name = "Mexico_Altar_KeystoneFlower"
keystone_flower.data.materials.append(mat_cempasuchil_gold)

# Altar Candles in Degradé of Heights across the tiers
candle_positions = [
    # Tier 1 Front edge
    (-3.0, altar_y - 0.45, floor_z + 0.60, 0.35),
    (-2.4, altar_y - 0.50, floor_z + 0.60, 0.45),
    (-1.8, altar_y - 0.45, floor_z + 0.60, 0.28),
    (-1.2, altar_y - 0.50, floor_z + 0.60, 0.50),
    (-0.6, altar_y - 0.45, floor_z + 0.60, 0.32),
    (0.6, altar_y - 0.45, floor_z + 0.60, 0.32),
    (1.2, altar_y - 0.50, floor_z + 0.60, 0.50),
    (1.8, altar_y - 0.45, floor_z + 0.60, 0.28),
    (2.4, altar_y - 0.50, floor_z + 0.60, 0.45),
    (3.0, altar_y - 0.45, floor_z + 0.60, 0.35),
    # Tier 2 Middle
    (-2.1, altar_y + 0.15, floor_z + 1.15, 0.40),
    (-1.4, altar_y + 0.10, floor_z + 1.15, 0.55),
    (-0.8, altar_y + 0.15, floor_z + 1.15, 0.30),
    (0.8, altar_y + 0.15, floor_z + 1.15, 0.30),
    (1.4, altar_y + 0.10, floor_z + 1.15, 0.55),
    (2.1, altar_y + 0.15, floor_z + 1.15, 0.40),
    # Tier 3 Top
    (-1.2, altar_y + 0.40, floor_z + 1.70, 0.48),
    (-0.5, altar_y + 0.35, floor_z + 1.70, 0.60),
    (0.0, altar_y + 0.40, floor_z + 1.70, 0.35),
    (0.5, altar_y + 0.35, floor_z + 1.70, 0.60),
    (1.2, altar_y + 0.40, floor_z + 1.70, 0.48),
]

for idx, (vx, vy, vz, vh) in enumerate(candle_positions):
    # Candle Wax Body
    bpy.ops.mesh.primitive_cylinder_add(radius=0.045, depth=vh, vertices=12, location=(vx, vy, vz + vh / 2))
    wax = bpy.context.active_object
    wax.name = f"Mexico_Altar_CandleWax_{idx}"
    wax.data.materials.append(mat_candle_wax)

    # Teardrop Flame (Emissive)
    bpy.ops.mesh.primitive_cone_add(radius1=0.022, radius2=0.002, depth=0.065, vertices=8, location=(vx, vy, vz + vh + 0.035))
    flame = bpy.context.active_object
    flame.name = f"Mexico_Altar_CandleFlame_{idx}"
    flame.data.materials.append(mat_flame_glow)

# Memorial Frames (Fictional Stylized Portraits of Card Club Ancestors)
frame_data = [
    # (x, y, z, width, height, angle)
    (-1.6, altar_y + 0.20, floor_z + 1.40, 0.38, 0.48, -0.08),
    (1.6, altar_y + 0.20, floor_z + 1.40, 0.38, 0.48, 0.08),
    (-0.7, altar_y + 0.48, floor_z + 2.05, 0.45, 0.60, -0.04),
    (0.7, altar_y + 0.48, floor_z + 2.05, 0.45, 0.60, 0.04),
    (0.0, altar_y + 0.52, floor_z + 2.15, 0.54, 0.72, 0.0), # Central Hero Portrait
]

for f_idx, (fx, fy, fz, fw, fh, fang) in enumerate(frame_data):
    # Wooden Frame
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(fx, fy, fz))
    mframe = bpy.context.active_object
    mframe.name = f"Mexico_Altar_Frame_{f_idx}"
    mframe.scale = (fw, 0.06, fh)
    mframe.rotation_euler = (0, 0, fang)
    mframe.data.materials.append(mat_rustic_wood)
    bpy.ops.object.transform_apply(scale=True, rotation=True)

    # Inner Canvas / Inset
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(fx, fy - 0.035, fz))
    portrait = bpy.context.active_object
    portrait.name = f"Mexico_Altar_PortraitCanvas_{f_idx}"
    portrait.scale = (fw * 0.82, fh * 0.84, 1.0)
    portrait.rotation_euler = (math.pi / 2, 0, fang)
    portrait.data.materials.append(mat_altar_linen)

# Barro Negro Pottery & Offerings (Pan de Muerto, Copal Burner)
# Copal Burner (Sahumador) in Center Tier 1
bpy.ops.mesh.primitive_cylinder_add(radius=0.18, depth=0.22, vertices=16, location=(0, altar_y - 0.35, floor_z + 0.70))
sahumador = bpy.context.active_object
sahumador.name = "Mexico_Altar_Sahumador"
sahumador.data.materials.append(mat_barro_negro)

# Pan de Muerto on Clay Plates
for px in [-1.5, 1.5]:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.22, depth=0.03, vertices=16, location=(px, altar_y - 0.25, floor_z + 0.61))
    plate = bpy.context.active_object
    plate.name = f"Mexico_Altar_Plate_{px}"
    plate.data.materials.append(mat_barro_negro)

    # Pan de muerto bread bun with cross bones
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.14, location=(px, altar_y - 0.25, floor_z + 0.72))
    pan = bpy.context.active_object
    pan.name = f"Mexico_Altar_PanDeMuerto_{px}"
    pan.scale = (1.0, 1.0, 0.65)
    pan.data.materials.append(mat_pan_muerto)
    bpy.ops.object.transform_apply(scale=True)

# Talavera Vases with Cempasúchil Boquets flanking altar
for vx in [-3.2, 3.2]:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.24, depth=0.55, vertices=16, location=(vx, altar_y - 0.25, floor_z + 0.85))
    vase = bpy.context.active_object
    vase.name = f"Mexico_Altar_TalaveraVase_{vx}"
    vase.data.materials.append(mat_talavera_pottery)

    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.36, location=(vx, altar_y - 0.25, floor_z + 1.25))
    flowers = bpy.context.active_object
    flowers.name = f"Mexico_Altar_VaseFlowers_{vx}"
    flowers.data.materials.append(mat_cempasuchil)

# ==========================================================
# 4. HACIENDA BACK WALL & COLONIAL ARCADES
# ==========================================================
back_wall_y = 7.4

# Left Back Wall (behind altar and left passage)
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-7.5, back_wall_y, 2.2))
wall_b_left = bpy.context.active_object
wall_b_left.name = "Mexico_BackWall_Left"
wall_b_left.scale = (11.0, 0.45, 6.2)
wall_b_left.data.materials.append(mat_stucco_terracotta)
bpy.ops.object.transform_apply(scale=True)

# Center Wall (direct background behind altar)
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, back_wall_y + 0.35, 2.6))
wall_b_center = bpy.context.active_object
wall_b_center.name = "Mexico_BackWall_Center"
wall_b_center.scale = (8.0, 0.45, 7.0)
wall_b_center.data.materials.append(mat_stucco_ochre)
bpy.ops.object.transform_apply(scale=True)

# Right Back Wall (with moonlit garden archway)
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(7.5, back_wall_y, 2.2))
wall_b_right = bpy.context.active_object
wall_b_right.name = "Mexico_BackWall_Right"
wall_b_right.scale = (11.0, 0.45, 6.2)
wall_b_right.data.materials.append(mat_stucco_terracotta)
bpy.ops.object.transform_apply(scale=True)

# Rustic Wooden Ceiling Beams (Vigas de Pino Rústico)
for bx in [-8.0, -4.5, 0.0, 4.5, 8.0]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx, 0.0, 4.85))
    viga = bpy.context.active_object
    viga.name = f"Mexico_Viga_{bx}"
    viga.scale = (0.32, 18.0, 0.42)
    viga.data.materials.append(mat_rustic_wood)
    bpy.ops.object.transform_apply(scale=True)

# Transverse Corbel Supports
for by in [-4.0, 4.0]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, by, 5.05))
    beam_t = bpy.context.active_object
    beam_t.name = f"Mexico_TransverseBeam_{by}"
    beam_t.scale = (22.0, 0.36, 0.45)
    beam_t.data.materials.append(mat_rustic_wood)
    bpy.ops.object.transform_apply(scale=True)

# Terracotta Roof Tile Eaves (Tejas) overhang along top of back wall
for tx in range(-6, 7):
    bpy.ops.mesh.primitive_cylinder_add(radius=0.18, depth=1.8, vertices=12, location=(tx * 1.2, back_wall_y - 0.45, 5.45))
    teja = bpy.context.active_object
    teja.name = f"Mexico_Teja_{tx}"
    teja.rotation_euler = (math.radians(18), 0, 0)
    teja.scale = (1.0, 1.0, 0.6)
    teja.data.materials.append(mat_tejas)

# ==========================================================
# 5. PAPEL PICADO GARLANDS STRUNG ACROSS COURTYARD
# ==========================================================
papel_colors = [mat_papel_magenta, mat_papel_gold, mat_papel_purple, mat_papel_turquoise]

# String Lines (Thin rustic wire)
for string_idx, (start_pt, end_pt) in enumerate([
    ((-7.0, -4.5, 4.4), (7.0, -2.5, 4.4)),
    ((-6.5, 1.5, 4.5), (6.5, 3.5, 4.5)),
    ((-7.5, 3.8, 4.6), (7.5, 3.8, 4.6)),
]):
    bpy.ops.mesh.primitive_cylinder_add(radius=0.008, depth=14.5, vertices=6, location=((start_pt[0]+end_pt[0])/2, (start_pt[1]+end_pt[1])/2, 4.5))
    wire = bpy.context.active_object
    wire.name = f"Mexico_PapelString_{string_idx}"
    wire.rotation_euler = (0, math.pi / 2, math.atan2(end_pt[1]-start_pt[1], end_pt[0]-start_pt[0]))
    wire.data.materials.append(mat_aged_wood)

    # Individual Papel Picado Flags
    flag_count = 11
    for fi in range(flag_count):
        t = (fi + 0.5) / flag_count
        fx = start_pt[0] + t * (end_pt[0] - start_pt[0])
        fy = start_pt[1] + t * (end_pt[1] - start_pt[1])
        fz = 4.45 - 0.12 * math.sin(t * math.pi) # Gentle natural catenary sag
        
        c_mat = papel_colors[(string_idx + fi) % len(papel_colors)]
        
        # Papel picado flag sheet
        bpy.ops.mesh.primitive_plane_add(size=1.0, location=(fx, fy, fz - 0.28))
        flag = bpy.context.active_object
        flag.name = f"Mexico_PapelFlag_{string_idx}_{fi}"
        flag.scale = (0.28, 0.42, 1.0)
        # Slight festive tilt
        tilt = math.sin(fi * 1.5 + string_idx) * 0.12
        flag.rotation_euler = (math.pi / 2 + tilt, 0, 0)
        flag.data.materials.append(c_mat)

# ==========================================================
# 6. LEFT WALL: RUSTIC PORCH & ACOUSTIC GUITAR
# ==========================================================
left_wall_x = -12.5

# Left Hacienda Wall
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(left_wall_x, 0, 2.2))
wall_l = bpy.context.active_object
wall_l.name = "Mexico_LeftWall"
wall_l.scale = (0.45, 20.0, 6.2)
wall_l.data.materials.append(mat_stucco_terracotta)
bpy.ops.object.transform_apply(scale=True)

# Porch Pillars (Rustic Wooden Posts on Stone Bases)
for pz_idx, py in enumerate([-4.5, 0.0, 4.5]):
    # Stone pedestal
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(left_wall_x + 3.2, py, floor_z + 0.35))
    pedestal = bpy.context.active_object
    pedestal.name = f"Mexico_Pillar_Pedestal_{pz_idx}"
    pedestal.scale = (0.65, 0.65, 0.70)
    pedestal.data.materials.append(mat_stone_patio)
    bpy.ops.object.transform_apply(scale=True)

    # Wood pillar shaft
    bpy.ops.mesh.primitive_cylinder_add(radius=0.18, depth=4.2, vertices=16, location=(left_wall_x + 3.2, py, floor_z + 2.8))
    pillar = bpy.context.active_object
    pillar.name = f"Mexico_Pillar_Shaft_{pz_idx}"
    pillar.data.materials.append(mat_rustic_wood)

    # Carved Capital beam support
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(left_wall_x + 3.2, py, floor_z + 4.95))
    pcap = bpy.context.active_object
    pcap.name = f"Mexico_Pillar_Cap_{pz_idx}"
    pcap.scale = (0.85, 0.55, 0.25)
    pcap.data.materials.append(mat_rustic_wood)
    bpy.ops.object.transform_apply(scale=True)

# Porch Beam spanning the pillars
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(left_wall_x + 3.2, 0, floor_z + 5.15))
pbeam = bpy.context.active_object
pbeam.name = "Mexico_Porch_LintelBeam"
pbeam.scale = (0.45, 12.0, 0.35)
pbeam.data.materials.append(mat_rustic_wood)
bpy.ops.object.transform_apply(scale=True)

# Mexican Classical Acoustic Guitar resting against the wall (Narrative of song and memory)
guitar_x = left_wall_x + 1.2
guitar_y = -1.2
guitar_z = floor_z + 0.65

# Guitar Body (Figure-8 body shape using two intersecting rounded meshes)
bpy.ops.mesh.primitive_cylinder_add(radius=0.28, depth=0.14, vertices=24, location=(guitar_x, guitar_y, guitar_z))
g_lower = bpy.context.active_object
g_lower.name = "Mexico_Guitar_LowerBody"
g_lower.rotation_euler = (math.radians(14), math.radians(12), math.radians(45))
g_lower.data.materials.append(mat_guitar_wood)

bpy.ops.mesh.primitive_cylinder_add(radius=0.21, depth=0.13, vertices=24, location=(guitar_x + 0.08, guitar_y, guitar_z + 0.38))
g_upper = bpy.context.active_object
g_upper.name = "Mexico_Guitar_UpperBody"
g_upper.rotation_euler = (math.radians(14), math.radians(12), math.radians(45))
g_upper.data.materials.append(mat_guitar_wood)

# Guitar Soundhole
bpy.ops.mesh.primitive_cylinder_add(radius=0.075, depth=0.15, vertices=16, location=(guitar_x + 0.06, guitar_y - 0.02, guitar_z + 0.28))
g_hole = bpy.context.active_object
g_hole.name = "Mexico_Guitar_Soundhole"
g_hole.rotation_euler = (math.radians(14), math.radians(12), math.radians(45))
g_hole.data.materials.append(mat_aged_wood)

# Guitar Neck and Headstock
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(guitar_x + 0.16, guitar_y + 0.04, guitar_z + 0.85))
g_neck = bpy.context.active_object
g_neck.name = "Mexico_Guitar_Neck"
g_neck.scale = (0.055, 0.05, 0.65)
g_neck.rotation_euler = (math.radians(14), math.radians(12), math.radians(45))
g_neck.data.materials.append(mat_aged_wood)
bpy.ops.object.transform_apply(scale=True)

# Talavera pottery jug on side shelf
bpy.ops.mesh.primitive_cylinder_add(radius=0.25, depth=0.55, vertices=16, location=(left_wall_x + 0.6, -3.2, floor_z + 0.75))
porch_jug = bpy.context.active_object
porch_jug.name = "Mexico_Porch_Jug"
porch_jug.data.materials.append(mat_talavera_pottery)

# ==========================================================
# 7. RIGHT WALL: MOONLIT COLONIAL ARCADES & PUEBLO VISTA
# ==========================================================
right_wall_x = 12.5

# Right Hacienda Wall with two grand archway openings overlooking the moonlit night
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(right_wall_x, -4.5, 2.2))
wall_r_south = bpy.context.active_object
wall_r_south.name = "Mexico_RightWall_South"
wall_r_south.scale = (0.45, 6.0, 6.2)
wall_r_south.data.materials.append(mat_stucco_terracotta)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(right_wall_x, 4.5, 2.2))
wall_r_north = bpy.context.active_object
wall_r_north.name = "Mexico_RightWall_North"
wall_r_north.scale = (0.45, 6.0, 6.2)
wall_r_north.data.materials.append(mat_stucco_terracotta)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(right_wall_x, 0.0, 4.5))
wall_r_top = bpy.context.active_object
wall_r_top.name = "Mexico_RightWall_ArchHeader"
wall_r_top.scale = (0.45, 4.0, 1.8)
wall_r_top.data.materials.append(mat_stucco_terracotta)
bpy.ops.object.transform_apply(scale=True)

# Central Arch Opening molding (Terracotta bricks)
bpy.ops.mesh.primitive_torus_add(major_radius=1.85, minor_radius=0.15, major_segments=32, minor_segments=12, location=(right_wall_x, 0.0, 3.6))
arch_r_molding = bpy.context.active_object
arch_r_molding.name = "Mexico_RightArch_Molding"
arch_r_molding.rotation_euler = (0, math.pi / 2, 0)
arch_r_molding.data.materials.append(mat_terracotta_brick)

# Moonlit Night Sky Backdrop plane beyond the right opening
bpy.ops.mesh.primitive_plane_add(size=1.0, location=(right_wall_x + 5.0, 0.0, 3.2))
sky_backdrop = bpy.context.active_object
sky_backdrop.name = "Mexico_NightSky_Backdrop"
sky_backdrop.scale = (1.0, 18.0, 10.0)
sky_backdrop.rotation_euler = (0, -math.pi / 2, 0)
sky_backdrop.data.materials.append(mat_nocturne_sky)

# Full Moon Disk in the night sky (Subtle pale silver emission)
mat_full_moon = create_pbr_mat("Mat_FullMoon", (0.92, 0.95, 1.0), roughness=0.15, emission_rgb=(0.88, 0.94, 1.0), emission_strength=2.2)
bpy.ops.mesh.primitive_cylinder_add(radius=1.2, depth=0.08, vertices=32, location=(right_wall_x + 4.8, 1.8, 6.5))
moon_disk = bpy.context.active_object
moon_disk.name = "Mexico_MoonDisk"
moon_disk.rotation_euler = (0, -math.pi / 2, 0)
moon_disk.data.materials.append(mat_full_moon)

# Distant Pueblo Roofline Silhouettes
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(right_wall_x + 3.8, -1.2, floor_z + 1.2))
pueblo_silh1 = bpy.context.active_object
pueblo_silh1.name = "Mexico_PuebloSilhouette_1"
pueblo_silh1.scale = (0.25, 3.5, 2.4)
pueblo_silh1.data.materials.append(mat_stucco_ochre)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cone_add(radius1=1.8, radius2=0, depth=1.2, vertices=4, location=(right_wall_x + 3.8, -1.2, floor_z + 3.0))
pueblo_roof = bpy.context.active_object
pueblo_roof.name = "Mexico_PuebloRoof_1"
pueblo_roof.rotation_euler = (0, 0, math.pi / 4)
pueblo_roof.data.materials.append(mat_tejas)

# ==========================================================
# 8. HERO CHANDELIER & RUSTIC WROUGHT-IRON LANTERNS
# ==========================================================
# Grand Central Wrought-Iron 6-Sided Lantern hanging directly over the playing table
bpy.ops.mesh.primitive_cylinder_add(radius=0.52, depth=0.85, vertices=6, location=(0, 0, 4.2))
lantern_body = bpy.context.active_object
lantern_body.name = "Mexico_CentralLantern_Frame"
lantern_body.data.materials.append(mat_wrought_iron)

# Amber Glass Panels inside lantern
bpy.ops.mesh.primitive_cylinder_add(radius=0.46, depth=0.75, vertices=6, location=(0, 0, 4.2))
lantern_glass = bpy.context.active_object
lantern_glass.name = "Mexico_CentralLantern_Glass"
lantern_glass.data.materials.append(mat_flame_glow)

# Suspension Chain from central beam
bpy.ops.mesh.primitive_cylinder_add(radius=0.035, depth=1.2, vertices=8, location=(0, 0, 4.9))
lantern_chain = bpy.context.active_object
lantern_chain.name = "Mexico_CentralLantern_Chain"
lantern_chain.data.materials.append(mat_wrought_iron)

# Wrought-iron Wall Sconces on Left & Right Walls
for sx, sy in [(-12.0, -3.5), (-12.0, 3.5), (12.0, -4.5), (12.0, 4.5)]:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=0.35, vertices=8, location=(sx * 0.98, sy, 2.5))
    sconce = bpy.context.active_object
    sconce.name = f"Mexico_WallSconce_{sx}_{sy}"
    sconce.data.materials.append(mat_wrought_iron)

    # Torch / Candle Flame
    bpy.ops.mesh.primitive_cone_add(radius1=0.04, radius2=0.005, depth=0.14, vertices=8, location=(sx * 0.98, sy, 2.75))
    sflame = bpy.context.active_object
    sflame.name = f"Mexico_SconceFlame_{sx}_{sy}"
    sflame.data.materials.append(mat_flame_glow)

# ==========================================================
# 9. EXPORT GLB WITH ZERO OBJECT LEAKS
# ==========================================================
out_dir = os.path.abspath("assets/models/club")
os.makedirs(out_dir, exist_ok=True)
glb_path = os.path.join(out_dir, "room_mexico_recuerdos.glb")

bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=glb_path, export_format='GLB', use_selection=True, export_apply=True)
print(f"[Blender MCP] Successfully exported room_mexico_recuerdos.glb ({os.path.getsize(glb_path)} bytes) to {glb_path}!")

# ==========================================================
# 10. PREVIEW CAMERAS & LIGHTING FOR EVIDENCE
# ==========================================================
# Key Light: Warm amber light from central chandelier
bpy.ops.object.light_add(type='POINT', location=(0, 0, 4.2))
key_l = bpy.context.active_object
key_l.name = "Mexico_Preview_ChandelierLight"
key_l.data.energy = 1750
key_l.data.color = (1.0, 0.72, 0.35)

# Altar Light: Warm golden glow from altar candles
bpy.ops.object.light_add(type='POINT', location=(0, altar_y, floor_z + 1.6))
altar_l = bpy.context.active_object
altar_l.name = "Mexico_Preview_AltarLight"
altar_l.data.energy = 1100
altar_l.data.color = (1.0, 0.58, 0.18)

# Moonlight Fill: Cool pale blue moonlight from right courtyard opening
bpy.ops.object.light_add(type='SUN', location=(10.0, 2.0, 8.0))
moon_l = bpy.context.active_object
moon_l.name = "Mexico_Preview_Moonlight"
moon_l.data.energy = 2.5
moon_l.data.color = (0.42, 0.62, 0.95)
moon_l.rotation_euler = (math.radians(45), math.radians(-30), math.radians(65))

# Camera looking at Central Table, Flower Path and Altar de Ofrendas
bpy.ops.object.camera_add(location=(0, -5.5, 1.45))
cam = bpy.context.active_object
cam.name = "Mexico_RenderCamera"
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

print("[Blender MCP] 'La Mesa de los Recuerdos' completed and configured for capture!")
