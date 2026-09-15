import bpy
import math
import os

# Clean scene objects safely without resetting Blender preferences/addons
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()
for mesh in list(bpy.data.meshes):
    if mesh.users == 0:
        bpy.data.meshes.remove(mesh)
for mat in list(bpy.data.materials):
    if mat.users == 0:
        bpy.data.materials.remove(mat)

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

print("[Blender MCP] Building complete high-fidelity room_classic_club...")

# --- 1. CORE MATERIALS ---
mat_floor = make_mat("classic_club_Floor", (0.15, 0.09, 0.05, 1.0), metallic=0.02, roughness=0.22)
mat_rug_main = make_mat("classic_club_RugMain", (0.04, 0.16, 0.10, 1.0), metallic=0.02, roughness=0.88)
mat_rug_border = make_mat("classic_club_RugBorder", (0.14, 0.10, 0.05, 1.0), metallic=0.15, roughness=0.65)
mat_trim = make_mat("classic_club_Trim", (0.86, 0.72, 0.26, 1.0), metallic=0.88, roughness=0.22)
mat_wood = make_mat("classic_club_Boiserie", (0.18, 0.10, 0.06, 1.0), metallic=0.0, roughness=0.35)
mat_wall = make_mat("classic_club_Wall", (0.07, 0.22, 0.14, 1.0), metallic=0.0, roughness=0.72)
mat_sconce = make_mat("classic_club_Sconce", (1.0, 0.90, 0.60, 1.0), metallic=0.1, roughness=0.1,
                      emission=(1.0, 0.90, 0.60, 1.0), emission_strength=3.2)
mat_brick = make_mat("ClassicBrick", (0.38, 0.14, 0.08, 1.0), roughness=0.88)
mat_fire_glow = make_mat("ClassicFireGlow", (1.0, 0.45, 0.05, 1.0), emission=(1.0, 0.40, 0.05, 1.0), emission_strength=5.5)
mat_charcoal = make_mat("ClassicCharcoal", (0.05, 0.04, 0.04, 1.0), roughness=0.95)

mat_gold = mat_trim
mat_marble = make_mat("BlackMarble", (0.05, 0.05, 0.06, 1.0), metallic=0.1, roughness=0.15)
mat_glass = make_mat("CutCrystal", (0.82, 0.90, 0.95, 1.0), metallic=0.1, roughness=0.06)
mat_bourbon = make_mat("BourbonLiquid", (0.85, 0.38, 0.06, 1.0), metallic=0.0, roughness=0.15)

mat_book_red = make_mat("BookRed", (0.55, 0.08, 0.10, 1.0), roughness=0.55)
mat_book_navy = make_mat("BookNavy", (0.07, 0.14, 0.42, 1.0), roughness=0.55)
mat_book_green = make_mat("BookGreen", (0.05, 0.32, 0.14, 1.0), roughness=0.55)
mat_book_ochre = make_mat("BookOchre", (0.70, 0.48, 0.16, 1.0), roughness=0.55)
mat_book_spine = make_mat("GoldSpineTooling", (0.92, 0.78, 0.28, 1.0), metallic=0.88, roughness=0.25)

mat_oil_painting_1 = make_mat("OilPaintingAce", (0.28, 0.18, 0.10, 1.0), metallic=0.05, roughness=0.6)
mat_oil_painting_2 = make_mat("OilPaintingKings", (0.16, 0.20, 0.28, 1.0), metallic=0.05, roughness=0.6)

# --- 2. ARCHITECTURAL ENVELOPE ---
# Floor
bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 1.5, -0.73))
floor = bpy.context.active_object
floor.name = "Floor"
floor.scale = (28.0, 18.0, 1.0)
floor.data.materials.append(mat_floor)
bpy.ops.object.transform_apply(scale=True)

# Rug under table
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.0, -0.718))
rug_outer = bpy.context.active_object
rug_outer.name = "ClubRug_Outer"
rug_outer.scale = (14.2, 9.8, 0.016)
rug_outer.data.materials.append(mat_rug_border)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.0, -0.714))
rug_stripe = bpy.context.active_object
rug_stripe.name = "ClubRug_Stripe"
rug_stripe.scale = (13.6, 9.2, 0.018)
rug_stripe.data.materials.append(mat_trim)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.0, -0.710))
rug_inner = bpy.context.active_object
rug_inner.name = "ClubRug_Inner"
rug_inner.scale = (13.2, 8.8, 0.020)
rug_inner.data.materials.append(mat_rug_main)
bpy.ops.object.transform_apply(scale=True)

wall_y = 6.4

# Baseboard
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y, -0.47))
baseboard = bpy.context.active_object
baseboard.name = "Baseboard"
baseboard.scale = (28.0, 0.42, 0.50)
baseboard.data.materials.append(mat_wood)
bpy.ops.object.transform_apply(scale=True)

# Back Wall Main Surface
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y, 3.4))
b_wall = bpy.context.active_object
b_wall.name = "BackWall_Classic"
b_wall.scale = (28.0, 0.35, 7.5)
b_wall.data.materials.append(mat_wall)
bpy.ops.object.transform_apply(scale=True)

# Side Walls with Boiserie & Sconces
for side, sign, wx in [("Left", -1, -13.6), ("Right", 1, 13.6)]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx, 1.5, 3.25))
    s_wall = bpy.context.active_object
    s_wall.name = f"SideWall_{side}"
    s_wall.scale = (0.50, 18.0, 8.0)
    s_wall.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)

    for spy in [3.5, -1.5, -5.5]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx - sign * 0.28, spy, 0.94))
        s_frame = bpy.context.active_object
        s_frame.name = f"SideBoiserieFrame_{side}_{spy}"
        s_frame.scale = (0.06, 3.2, 1.70)
        s_frame.data.materials.append(mat_trim)
        bpy.ops.object.transform_apply(scale=True)

        bpy.ops.mesh.primitive_cylinder_add(radius=0.055, depth=0.20, vertices=16, location=(wx - sign * 0.45, spy, 4.05))
        s_lamp = bpy.context.active_object
        s_lamp.name = f"SideSconceLamp_{side}_{spy}"
        s_lamp.data.materials.append(mat_sconce)

# Coffered Ceiling
bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 1.5, 7.35))
ceiling = bpy.context.active_object
ceiling.name = "CeilingPlane"
ceiling.scale = (28.0, 18.0, 1.0)
ceiling.rotation_euler = (math.pi, 0, 0)
ceiling.data.materials.append(mat_wood)
bpy.ops.object.transform_apply(scale=True, rotation=True)
for bz_y in [-5.0, -1.8, 1.8, 5.0]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, bz_y, 7.22))
    beam = bpy.context.active_object
    beam.name = f"CeilingBeam_{bz_y}"
    beam.scale = (27.6, 0.40, 0.26)
    beam.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)

# --- 3. GRAND BRICK & MAHOGANY FIREPLACE ---
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.45, -0.60))
hearth = bpy.context.active_object
hearth.name = "FireplaceHearth"
hearth.scale = (3.8, 1.2, 0.24)
hearth.data.materials.append(mat_brick)
bpy.ops.object.transform_apply(scale=True)

for px in [-1.65, 1.65]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, wall_y - 0.35, 0.95))
    fp_col = bpy.context.active_object
    fp_col.name = f"FireplaceCol_{px}"
    fp_col.scale = (0.50, 0.85, 2.80)
    fp_col.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.42, 2.38))
mantel = bpy.context.active_object
mantel.name = "FireplaceMantel"
mantel.scale = (4.0, 1.0, 0.18)
mantel.data.materials.append(mat_wood)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.10, 0.65))
cavity = bpy.context.active_object
cavity.name = "FireplaceCavity"
cavity.scale = (2.6, 0.5, 2.1)
cavity.data.materials.append(mat_charcoal)
bpy.ops.object.transform_apply(scale=True)

for lx, ly, lz, rot in [(-0.35, wall_y - 0.25, -0.40, 0.3), (0.30, wall_y - 0.28, -0.38, -0.25), (0.0, wall_y - 0.22, -0.32, 0.05)]:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=0.90, vertices=12, location=(lx, ly, lz))
    log = bpy.context.active_object
    log.name = f"FireLog_{lx}"
    log.rotation_euler = (0, 1.57, rot)
    log.data.materials.append(mat_fire_glow)
    bpy.ops.object.transform_apply(rotation=True)

# Brass Fireplace Irons
bpy.ops.mesh.primitive_cylinder_add(radius=0.10, depth=0.04, vertices=16, location=(1.95, wall_y - 0.50, -0.66))
irons_base = bpy.context.active_object
irons_base.data.materials.append(mat_gold)
bpy.ops.mesh.primitive_cylinder_add(radius=0.015, depth=0.90, vertices=12, location=(1.95, wall_y - 0.50, -0.22))
irons_rod = bpy.context.active_object
irons_rod.data.materials.append(mat_gold)

# --- 4. ORNATE BAROQUE FRAMED OIL PAINTINGS (FLANKING FIREPLACE) ---
paintings = [
    ("Painting_L", -2.6, mat_oil_painting_1),
    ("Painting_R",  2.6, mat_oil_painting_2)
]
for pname, px, pmat in paintings:
    # Outer Gilded Baroque Frame
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, wall_y - 0.15, 3.40))
    p_frame = bpy.context.active_object
    p_frame.name = f"{pname}_Frame"
    p_frame.scale = (1.20, 0.08, 1.60)
    p_frame.data.materials.append(mat_gold)
    bpy.ops.object.transform_apply(scale=True)
    
    # Inner Canvas Canvas
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, wall_y - 0.20, 3.40))
    canvas = bpy.context.active_object
    canvas.name = f"{pname}_Canvas"
    canvas.scale = (0.98, 0.04, 1.38)
    canvas.data.materials.append(pmat)
    bpy.ops.object.transform_apply(scale=True)
    
    # Brass Picture Light above frame
    bpy.ops.mesh.primitive_cylinder_add(radius=0.025, depth=0.60, vertices=16, location=(px, wall_y - 0.35, 4.30))
    pic_light = bpy.context.active_object
    pic_light.rotation_euler = (0, math.pi/2, 0)
    pic_light.data.materials.append(mat_sconce)
    bpy.ops.object.transform_apply(rotation=True)

# --- 5. TWO FLANKING GRAND HERITAGE BOOKCASES WITH TROPHIES & BOOKS ---
book_mats = [mat_book_red, mat_book_navy, mat_book_green, mat_book_ochre]

for bx_pos in [-5.0, 5.0]:
    # Upright Posts
    for side_idx, px_off in [(-1, -1.35), (1, 1.35)]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos + px_off, wall_y - 0.28, 2.30))
        col = bpy.context.active_object
        col.name = f"BookcaseCol_{bx_pos}_{side_idx}"
        col.scale = (0.16, 0.58, 4.80)
        col.data.materials.append(mat_wood)
        bpy.ops.object.transform_apply(scale=True)
        
        # Fluted Pilaster Front Trim
        bpy.ops.mesh.primitive_cylinder_add(radius=0.06, depth=4.6, vertices=16, location=(bx_pos + px_off, wall_y - 0.58, 2.30))
        pil = bpy.context.active_object
        pil.data.materials.append(mat_wood)
        
        # Brass Capitals and Base Collars
        for cz in [0.15, 4.55]:
            bpy.ops.mesh.primitive_cylinder_add(radius=0.085, depth=0.12, vertices=16, location=(bx_pos + px_off, wall_y - 0.58, cz))
            cap = bpy.context.active_object
            cap.data.materials.append(mat_gold)

    # Base Plinth
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos, wall_y - 0.30, -0.05))
    bs_base = bpy.context.active_object
    bs_base.name = f"BookcaseBase_{bx_pos}"
    bs_base.scale = (2.85, 0.65, 0.30)
    bs_base.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)

    # Cornice Header
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos, wall_y - 0.30, 4.75))
    bs_top = bpy.context.active_object
    bs_top.name = f"BookcaseTop_{bx_pos}"
    bs_top.scale = (2.85, 0.65, 0.22)
    bs_top.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)

    # Arched Pediment Top
    bpy.ops.mesh.primitive_cylinder_add(radius=1.35, depth=0.62, vertices=24, location=(bx_pos, wall_y - 0.29, 4.88))
    arch_top = bpy.context.active_object
    arch_top.rotation_euler = (math.pi / 2, 0, 0)
    arch_top.scale = (1.0, 0.50, 1.0)
    arch_top.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True, rotation=True)

    # Brass Club Ace Medallion on Pediment
    bpy.ops.mesh.primitive_cylinder_add(radius=0.28, depth=0.08, vertices=24, location=(bx_pos, wall_y - 0.62, 5.08))
    medallion = bpy.context.active_object
    medallion.rotation_euler = (math.pi/2, 0, 0)
    medallion.data.materials.append(mat_gold)
    bpy.ops.object.transform_apply(rotation=True)

    # 4 Shelves with Brass Lip
    for s_idx, sz_val in enumerate([0.25, 1.35, 2.45, 3.55]):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos, wall_y - 0.26, sz_val))
        shelf = bpy.context.active_object
        shelf.name = f"BookcaseShelf_{bx_pos}_{s_idx}"
        shelf.scale = (2.55, 0.54, 0.05)
        shelf.data.materials.append(mat_wood)
        bpy.ops.object.transform_apply(scale=True)
        
        # Brass Lip
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos, wall_y - 0.53, sz_val))
        lip = bpy.context.active_object
        lip.scale = (2.55, 0.03, 0.06)
        lip.data.materials.append(mat_gold)
        bpy.ops.object.transform_apply(scale=True)

    # Shelf 0 (Bottom): Full row of antique books with gold spine ribs
    for bk in range(11):
        bx_off = -1.05 + bk * 0.21
        bh = 0.52 + (bk % 3) * 0.07
        bw = 0.17
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos + bx_off, wall_y - 0.26, 0.25 + bh/2 + 0.03))
        bobj = bpy.context.active_object
        bobj.scale = (bw, 0.38, bh)
        bobj.data.materials.append(book_mats[(bk + int(bx_pos)) % 4])
        bpy.ops.object.transform_apply(scale=True)
        # Gold spine ribs
        for rz in [0.25, 0.5, 0.75]:
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos + bx_off, wall_y - 0.46, 0.25 + bh*rz))
            rib = bpy.context.active_object
            rib.scale = (bw * 0.85, 0.015, 0.02)
            rib.data.materials.append(mat_book_spine)
            bpy.ops.object.transform_apply(scale=True)

    # Shelf 1: Crystal Decanter and Tumblers (Left) & Books (Right)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos - 0.70, wall_y - 0.28, 1.62))
    dec_body = bpy.context.active_object
    dec_body.scale = (0.26, 0.26, 0.40)
    dec_body.data.materials.append(mat_glass)
    bpy.ops.object.transform_apply(scale=True)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos - 0.70, wall_y - 0.28, 1.53))
    dec_liq = bpy.context.active_object
    dec_liq.scale = (0.22, 0.22, 0.22)
    dec_liq.data.materials.append(mat_bourbon)
    bpy.ops.object.transform_apply(scale=True)

    bpy.ops.mesh.primitive_cylinder_add(radius=0.055, depth=0.16, vertices=16, location=(bx_pos - 0.70, wall_y - 0.28, 1.86))
    dec_neck = bpy.context.active_object
    dec_neck.data.materials.append(mat_glass)

    bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=0.09, vertices=16, location=(bx_pos - 0.70, wall_y - 0.28, 1.98))
    dec_stop = bpy.context.active_object
    dec_stop.data.materials.append(mat_glass)

    for tx in [-0.35, -0.15]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.07, depth=0.14, vertices=16, location=(bx_pos + tx, wall_y - 0.35, 1.45))
        tumbler = bpy.context.active_object
        tumbler.data.materials.append(mat_glass)

    for bk in range(5):
        bx_off = 0.35 + bk * 0.18
        bh = 0.50 + (bk % 2) * 0.06
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos + bx_off, wall_y - 0.26, 1.35 + bh/2 + 0.03))
        bobj = bpy.context.active_object
        bobj.scale = (0.15, 0.36, bh)
        bobj.data.materials.append(book_mats[(bk + 1) % 4])
        bpy.ops.object.transform_apply(scale=True)

    # Shelf 2: GRAND TRUCO CHAMPIONSHIP TROPHY
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos, wall_y - 0.26, 2.56))
    trop_base = bpy.context.active_object
    trop_base.scale = (0.42, 0.42, 0.16)
    trop_base.data.materials.append(mat_marble)
    bpy.ops.object.transform_apply(scale=True)

    bpy.ops.mesh.primitive_cylinder_add(radius=0.14, depth=0.06, vertices=20, location=(bx_pos, wall_y - 0.26, 2.67))
    bpy.context.active_object.data.materials.append(mat_gold)

    bpy.ops.mesh.primitive_cylinder_add(radius=0.065, depth=0.24, vertices=16, location=(bx_pos, wall_y - 0.26, 2.80))
    bpy.context.active_object.data.materials.append(mat_gold)

    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=0.26, radius2=0.10, depth=0.38, location=(bx_pos, wall_y - 0.26, 3.04))
    cup = bpy.context.active_object
    cup.data.materials.append(mat_gold)

    bpy.ops.mesh.primitive_torus_add(major_radius=0.26, minor_radius=0.025, location=(bx_pos, wall_y - 0.26, 3.23))
    bpy.context.active_object.data.materials.append(mat_gold)

    for sign in [-1, 1]:
        bpy.ops.mesh.primitive_torus_add(major_radius=0.14, minor_radius=0.022, location=(bx_pos + sign * 0.29, wall_y - 0.26, 3.08))
        handle = bpy.context.active_object
        handle.rotation_euler = (0, math.pi/2, 0)
        handle.scale = (1.2, 0.7, 1.0)
        handle.data.materials.append(mat_gold)
        bpy.ops.object.transform_apply(rotation=True, scale=True)

    for ca, crot in [(-0.06, -0.2), (0.0, 0.0), (0.06, 0.2)]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos + ca, wall_y - 0.26, 3.32))
        card = bpy.context.active_object
        card.rotation_euler = (0, crot, 0)
        card.scale = (0.09, 0.01, 0.15)
        card.data.materials.append(mat_gold)
        bpy.ops.object.transform_apply(rotation=True, scale=True)

    # Shelf 3: Upper Volumes Row
    for bk in range(12):
        bx_off = -1.10 + bk * 0.20
        bh = 0.46 + (bk % 3) * 0.06
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos + bx_off, wall_y - 0.26, 3.55 + bh/2 + 0.03))
        bobj = bpy.context.active_object
        bobj.scale = (0.16, 0.36, bh)
        bobj.data.materials.append(book_mats[(bk + 2) % 4])
        bpy.ops.object.transform_apply(scale=True)

# --- 6. GRANDFATHER CLOCK (RELÓGIO DE PÊNDULO) ---
clk_x, clk_y = -8.5, wall_y - 0.20
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(clk_x, clk_y, -0.15))
c_base = bpy.context.active_object
c_base.name = "GrandfatherClockBase"
c_base.scale = (1.1, 0.65, 1.1)
c_base.data.materials.append(mat_wood)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(clk_x, clk_y, 1.45))
c_waist = bpy.context.active_object
c_waist.name = "GrandfatherClockWaist"
c_waist.scale = (0.90, 0.55, 2.10)
c_waist.data.materials.append(mat_wood)
bpy.ops.object.transform_apply(scale=True)

bpy.ops.mesh.primitive_cylinder_add(radius=0.14, depth=0.03, vertices=20, location=(clk_x, clk_y - 0.20, 1.15))
pendulum = bpy.context.active_object
pendulum.name = "ClockPendulum"
pendulum.rotation_euler = (math.pi / 2, 0, 0)
pendulum.data.materials.append(mat_gold)
bpy.ops.object.transform_apply(rotation=True)

bpy.ops.mesh.primitive_cube_add(size=1.0, location=(clk_x, clk_y, 2.95))
c_hood = bpy.context.active_object
c_hood.name = "GrandfatherClockHood"
c_hood.scale = (1.05, 0.65, 0.90)
c_hood.data.materials.append(mat_wood)
bpy.ops.object.transform_apply(scale=True)

mat_clock_dial = make_mat("ClockDial", (0.95, 0.93, 0.85, 1.0), roughness=0.3)
bpy.ops.mesh.primitive_cylinder_add(radius=0.32, depth=0.04, vertices=24, location=(clk_x, clk_y - 0.32, 2.95))
dial = bpy.context.active_object
dial.name = "ClockDial"
dial.rotation_euler = (math.pi / 2, 0, 0)
dial.data.materials.append(mat_clock_dial)
bpy.ops.object.transform_apply(rotation=True)

# --- 7. VICTORIAN DRINKS BAR CART (LOCATED AT LEFT SIDE WALL) ---
cart_x, cart_y, cart_z = -8.2, 1.6, -0.71
cx_half, cy_half = 0.50, 0.28
c_top_z, c_bot_z = cart_z + 0.86, cart_z + 0.25

mat_cart_brass = make_mat("CartBrass", (0.90, 0.74, 0.25, 1.0), metallic=0.92, roughness=0.20)
mat_chrome = make_mat("CartChrome", (0.92, 0.94, 0.96, 1.0), metallic=0.96, roughness=0.12)
mat_smoke_glass = make_mat("SmokeGlass", (0.12, 0.12, 0.15, 0.7), metallic=0.1, roughness=0.08)
mat_bottle_green = make_mat("BottleEmerald", (0.04, 0.38, 0.16, 0.8), metallic=0.1, roughness=0.12)
mat_bottle_amber = make_mat("BottleAmber", (0.80, 0.40, 0.05, 0.8), metallic=0.1, roughness=0.12)
mat_bottle_ruby = make_mat("BottleRuby", (0.50, 0.05, 0.10, 0.8), metallic=0.1, roughness=0.12)

# Cart Uprights
for sx in [-1, 1]:
    for sy in [-1, 1]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.02, depth=0.82, vertices=14,
                                            location=(cart_x + sx * cx_half, cart_y + sy * cy_half, (c_top_z + c_bot_z)/2))
        bpy.context.active_object.data.materials.append(mat_cart_brass)

# Trays
for tz in [c_top_z, c_bot_z]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cart_x, cart_y, tz))
    tr = bpy.context.active_object
    tr.scale = (cx_half * 2 + 0.06, cy_half * 2 + 0.06, 0.02)
    tr.data.materials.append(mat_smoke_glass)
    bpy.ops.object.transform_apply(scale=True)
    
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cart_x, cart_y, tz + 0.03))
    rail = bpy.context.active_object
    rail.scale = (cx_half * 2 + 0.08, cy_half * 2 + 0.08, 0.012)
    rail.data.materials.append(mat_cart_brass)
    bpy.ops.object.transform_apply(scale=True)

# Cart Wheels
for wy in [-cy_half - 0.04, cy_half + 0.04]:
    bpy.ops.mesh.primitive_torus_add(major_radius=0.18, minor_radius=0.016, location=(cart_x + cx_half, cart_y + wy, cart_z + 0.18))
    w_rim = bpy.context.active_object
    w_rim.rotation_euler = (math.pi/2, 0, 0)
    w_rim.data.materials.append(mat_cart_brass)
    bpy.ops.object.transform_apply(rotation=True)

# Bar Shaker & Bottles
bpy.ops.mesh.primitive_cylinder_add(radius=0.065, depth=0.20, vertices=16, location=(cart_x - 0.15, cart_y + 0.08, c_top_z + 0.12))
bpy.context.active_object.data.materials.append(mat_chrome)

bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=0.16, vertices=16, location=(cart_x - 0.15, cart_y - 0.10, c_top_z + 0.10))
bpy.context.active_object.data.materials.append(mat_chrome)

cart_bottles = [
    (cart_x + 0.15, cart_y - 0.08, mat_bottle_green),
    (cart_x + 0.15, cart_y + 0.08, mat_bottle_amber),
    (cart_x - 0.10, cart_y, mat_bottle_ruby),
]
for bx, by, bmat in cart_bottles:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.05, depth=0.24, vertices=14, location=(bx, by, c_bot_z + 0.14))
    bpy.context.active_object.data.materials.append(bmat)

# --- 8. GRAND CHANDELIER ---
ch_x, ch_y, ch_z = 0.0, 0.0, 5.25
bpy.ops.mesh.primitive_cylinder_add(radius=0.45, depth=0.12, vertices=24, location=(ch_x, ch_y, 7.28))
canopy = bpy.context.active_object
canopy.data.materials.append(mat_gold)

bpy.ops.mesh.primitive_cylinder_add(radius=0.038, depth=1.80, vertices=16, location=(ch_x, ch_y, 6.35))
ch_rod = bpy.context.active_object
ch_rod.data.materials.append(mat_gold)

bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=0.32, location=(ch_x, ch_y, ch_z))
ch_body = bpy.context.active_object
ch_body.data.materials.append(mat_gold)

for a_idx in range(8):
    angle = a_idx * (2 * math.pi / 8)
    ax = ch_x + math.cos(angle) * 1.35
    ay = ch_y + math.sin(angle) * 1.35
    bpy.ops.mesh.primitive_cylinder_add(radius=0.055, depth=0.22, vertices=16, location=(ax, ay, ch_z + 0.30))
    c_lamp = bpy.context.active_object
    c_lamp.data.materials.append(mat_sconce)

# --- 9. EXPORT GLB ---
out_dir = os.path.abspath("assets/models/club")
os.makedirs(out_dir, exist_ok=True)
file_path = os.path.join(out_dir, "room_classic_club.glb")
bpy.ops.export_scene.gltf(filepath=file_path, export_format='GLB', use_selection=False, export_apply=True)
print(f"[Blender MCP] Successfully exported upgraded room_classic_club.glb ({os.path.getsize(file_path)} bytes)!")

# --- 10. LIGHTING & CAMERA FOR ROOM BEAUTY SCREENSHOT ---
bpy.ops.object.light_add(type='AREA', radius=5.0, location=(0, -2.0, 5.5))
room_key = bpy.context.active_object
room_key.name = "RoomKeyLight"
room_key.data.energy = 1500
room_key.data.color = (1.0, 0.94, 0.84)

bpy.ops.object.light_add(type='POINT', location=(0, wall_y - 0.5, 0.8))
fire_omni = bpy.context.active_object
fire_omni.name = "FireplaceLight"
fire_omni.data.energy = 650
fire_omni.data.color = (1.0, 0.50, 0.10)

# Camera positioned in the card hall viewing the fireplace, bookcases, and bar cart
bpy.ops.object.camera_add(location=(0, -4.8, 2.1))
cam = bpy.context.active_object
cam.name = "RenderCamera"
cam.rotation_euler = (math.radians(82), 0, 0)
cam.data.lens = 22
bpy.context.scene.camera = cam

for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        for space in area.spaces:
            if space.type == 'VIEW_3D':
                space.shading.type = 'MATERIAL'
                space.overlay.show_overlays = False
                space.region_3d.view_perspective = 'CAMERA'

print("[Blender MCP] Scene rendered and configured for viewport capture!")
