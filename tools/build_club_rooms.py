import bpy
import math
import os

bpy.ops.wm.read_factory_settings(use_empty=True)

out_dir = os.path.abspath("assets/models/club")
os.makedirs(out_dir, exist_ok=True)

def make_mat(name, base_color, metallic=0.0, roughness=0.5, emission=(0,0,0,1), emission_strength=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = base_color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if emission_strength > 0:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = emission
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = emission
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

# ==========================================================
# 1. BUILD LUXURY CLUB CHAIR (club_chair.glb)
# ==========================================================
def build_club_chair():
    clear_scene()
    print("[Blender] Building luxury club armchair...")
    
    mat_velvet = make_mat("ChairVelvet", (0.07, 0.22, 0.15, 1.0), metallic=0.04, roughness=0.55)
    mat_wood = make_mat("ChairWood", (0.16, 0.08, 0.04, 1.0), metallic=0.0, roughness=0.32)
    mat_brass = make_mat("ChairBrass", (0.84, 0.70, 0.25, 1.0), metallic=0.88, roughness=0.25)
    
    # Rounded Seat Cushion
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.04, 0.42))
    cushion = bpy.context.active_object
    cushion.name = "SeatCushion"
    cushion.scale = (0.70, 0.66, 0.14)
    cushion.data.materials.append(mat_velvet)
    bpy.ops.object.transform_apply(scale=True)
    bev_c = cushion.modifiers.new("Bevel", "BEVEL")
    bev_c.width = 0.045
    bev_c.segments = 4

    # Wood Base Trim
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.04, 0.33))
    base = bpy.context.active_object
    base.name = "SeatBase"
    base.scale = (0.72, 0.68, 0.06)
    base.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)
    bev_b = base.modifiers.new("Bevel", "BEVEL")
    bev_b.width = 0.02
    bev_b.segments = 2

    # Curved Wraparound Backrest
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.28, 0.72))
    back = bpy.context.active_object
    back.name = "Backrest_Main"
    back.scale = (0.74, 0.14, 0.52)
    back.data.materials.append(mat_velvet)
    bpy.ops.object.transform_apply(scale=True)
    bev_bk = back.modifiers.new("Bevel", "BEVEL")
    bev_bk.width = 0.05
    bev_bk.segments = 4

    # Left and Right Armrests
    for sign, arm_name in [(-1, "Armrest_L"), (1, "Armrest_R")]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sign * 0.38, 0.04, 0.60))
        arm = bpy.context.active_object
        arm.name = arm_name
        arm.scale = (0.12, 0.62, 0.28)
        arm.data.materials.append(mat_velvet)
        bpy.ops.object.transform_apply(scale=True)
        bev_a = arm.modifiers.new("Bevel", "BEVEL")
        bev_a.width = 0.04
        bev_a.segments = 3

    # Polished Wood Legs with Brass Ferrules
    leg_coords = [
        ("Leg_FL", -0.31, -0.31, 0.15, -0.06, 0.06),
        ("Leg_FR",  0.31, -0.31, 0.15, -0.06, -0.06),
        ("Leg_BL", -0.31,  0.26, 0.15,  0.08, 0.06),
        ("Leg_BR",  0.31,  0.26, 0.15,  0.08, -0.06)
    ]
    for name, x, y, z, rot_x, rot_y in leg_coords:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.032, depth=0.30, vertices=16, location=(x, y, z))
        leg = bpy.context.active_object
        leg.name = name
        leg.rotation_euler = (rot_x, rot_y, 0)
        leg.data.materials.append(mat_wood)
        bpy.ops.object.transform_apply(rotation=True)

        bpy.ops.mesh.primitive_cylinder_add(radius=0.035, depth=0.06, vertices=16, location=(x, y, 0.03))
        ferrule = bpy.context.active_object
        ferrule.name = f"Ferrule_{name}"
        ferrule.data.materials.append(mat_brass)

    chair_path = os.path.join(out_dir, "club_chair.glb")
    bpy.ops.export_scene.gltf(filepath=chair_path, export_format='GLB', use_selection=False, export_apply=True)
    print(f"[Blender] Saved club_chair.glb ({os.path.getsize(chair_path)} bytes)")

# ==========================================================
# 2. COMMON ROOM BASE BUILDER
# ==========================================================
def build_base_room_shell(room_id, floor_color, floor_rough, floor_metal,
                          rug_main, rug_border, boiserie_color, wall_color,
                          accent_color, drapery_color, sconce_emission, sconce_strength):
    clear_scene()
    mat_floor = make_mat(f"{room_id}_Floor", floor_color, metallic=floor_metal, roughness=floor_rough)
    mat_rug_main = make_mat(f"{room_id}_RugMain", rug_main, metallic=0.02, roughness=0.88)
    mat_rug_border = make_mat(f"{room_id}_RugBorder", rug_border, metallic=0.15, roughness=0.65)
    mat_boiserie = make_mat(f"{room_id}_Boiserie", boiserie_color, metallic=0.0, roughness=0.35)
    mat_boiserie_panel = make_mat(f"{room_id}_Panel", (boiserie_color[0]*0.82, boiserie_color[1]*0.82, boiserie_color[2]*0.82, 1.0), metallic=0.0, roughness=0.42)
    mat_wall = make_mat(f"{room_id}_Wall", wall_color, metallic=0.0, roughness=0.72)
    mat_trim = make_mat(f"{room_id}_Trim", accent_color, metallic=0.85, roughness=0.22)
    mat_drapery = make_mat(f"{room_id}_Drapery", drapery_color, metallic=0.04, roughness=0.85)
    mat_sconce = make_mat(f"{room_id}_Sconce", sconce_emission, metallic=0.1, roughness=0.1,
                          emission=sconce_emission, emission_strength=sconce_strength)

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
    rug_outer.data.materials.append(rug_border if isinstance(rug_border, bpy.types.Material) else mat_rug_border)
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
    rug_inner.data.materials.append(rug_main if isinstance(rug_main, bpy.types.Material) else mat_rug_main)
    bpy.ops.object.transform_apply(scale=True)

    wall_y = 6.4

    # Plinth Baseboard
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y, -0.47))
    baseboard = bpy.context.active_object
    baseboard.name = "Baseboard"
    baseboard.scale = (28.0, 0.42, 0.50)
    baseboard.data.materials.append(mat_boiserie)
    bpy.ops.object.transform_apply(scale=True)

    # Side Walls
    for side, sign, wx in [("Left", -1, -13.6), ("Right", 1, 13.6)]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx, 1.5, 3.25))
        s_wall = bpy.context.active_object
        s_wall.name = f"SideWall_{side}"
        s_wall.scale = (0.50, 18.0, 8.0)
        s_wall.data.materials.append(mat_boiserie)
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

    return {
        "wall_y": wall_y, "mat_floor": mat_floor, "mat_boiserie": mat_boiserie,
        "mat_boiserie_panel": mat_boiserie_panel, "mat_wall": mat_wall,
        "mat_trim": mat_trim, "mat_drapery": mat_drapery, "mat_sconce": mat_sconce
    }

# ==========================================================
# 3. SALÃO CLÁSSICO (classic_club) - LAREIRA, ESTANTES & PÊNDULO
# ==========================================================
def build_classic_club():
    print("[Blender] Building unique 3D room: classic_club (Lareira de tijolos, estantes de mogno e pêndulo)...")
    mats = build_base_room_shell(
        room_id="classic_club",
        floor_color=(0.15, 0.09, 0.05, 1.0), floor_rough=0.22, floor_metal=0.02,
        rug_main=(0.04, 0.16, 0.10, 1.0), rug_border=(0.14, 0.10, 0.05, 1.0),
        boiserie_color=(0.18, 0.10, 0.06, 1.0), wall_color=(0.07, 0.22, 0.14, 1.0),
        accent_color=(0.86, 0.72, 0.26, 1.0), drapery_color=(0.05, 0.18, 0.11, 1.0),
        sconce_emission=(1.0, 0.90, 0.60, 1.0), sconce_strength=3.2
    )
    wall_y = mats["wall_y"]
    mat_wood = mats["mat_boiserie"]
    mat_gold = mats["mat_trim"]
    mat_wall = mats["mat_wall"]
    mat_sconce = mats["mat_sconce"]

    # Back Wall Main Surface
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y, 3.4))
    b_wall = bpy.context.active_object
    b_wall.name = "BackWall_Classic"
    b_wall.scale = (28.0, 0.35, 7.5)
    b_wall.data.materials.append(mat_wall)
    bpy.ops.object.transform_apply(scale=True)

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
        beam.scale = (27.6, 0.40, 0.26)
        beam.data.materials.append(mat_wood)
        bpy.ops.object.transform_apply(scale=True)

    # --- ARCHITECTURAL UNIQUE PIECE 1: GRAND BRICK & MAHOGANY FIREPLACE ---
    mat_brick = make_mat("ClassicBrick", (0.38, 0.14, 0.08, 1.0), roughness=0.88)
    mat_fire_glow = make_mat("ClassicFireGlow", (1.0, 0.45, 0.05, 1.0), emission=(1.0, 0.40, 0.05, 1.0), emission_strength=5.5)
    mat_charcoal = make_mat("ClassicCharcoal", (0.05, 0.04, 0.04, 1.0), roughness=0.95)

    # Fireplace Hearth Plinth
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.45, -0.60))
    hearth = bpy.context.active_object
    hearth.name = "FireplaceHearth"
    hearth.scale = (3.8, 1.2, 0.24)
    hearth.data.materials.append(mat_brick)
    bpy.ops.object.transform_apply(scale=True)

    # Fireplace Outer Pillars
    for px in [-1.65, 1.65]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, wall_y - 0.35, 0.95))
        fp_col = bpy.context.active_object
        fp_col.name = f"FireplaceCol_{px}"
        fp_col.scale = (0.50, 0.85, 2.80)
        fp_col.data.materials.append(mat_wood)
        bpy.ops.object.transform_apply(scale=True)

    # Fireplace Over-Mantel Shelf
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.42, 2.38))
    mantel = bpy.context.active_object
    mantel.name = "FireplaceMantel"
    mantel.scale = (4.0, 1.0, 0.18)
    mantel.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)

    # Fireplace Cavity Back
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.10, 0.65))
    cavity = bpy.context.active_object
    cavity.name = "FireplaceCavity"
    cavity.scale = (2.6, 0.5, 2.1)
    cavity.data.materials.append(mat_charcoal)
    bpy.ops.object.transform_apply(scale=True)

    # Burning Embers / Glowing Fire Logs
    for lx, ly, lz, rot in [(-0.35, wall_y - 0.25, -0.40, 0.3), (0.30, wall_y - 0.28, -0.38, -0.25), (0.0, wall_y - 0.22, -0.32, 0.05)]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=0.90, vertices=12, location=(lx, ly, lz))
        log = bpy.context.active_object
        log.name = f"FireLog_{lx}"
        log.rotation_euler = (0, 1.57, rot)
        log.data.materials.append(mat_fire_glow)
        bpy.ops.object.transform_apply(rotation=True)

    # Brass Fireplace Poker & Tongs Stand
    bpy.ops.mesh.primitive_cylinder_add(radius=0.10, depth=0.04, vertices=16, location=(1.95, wall_y - 0.50, -0.66))
    irons_base = bpy.context.active_object
    irons_base.data.materials.append(mat_gold)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.015, depth=0.90, vertices=12, location=(1.95, wall_y - 0.50, -0.22))
    irons_rod = bpy.context.active_object
    irons_rod.data.materials.append(mat_gold)

    # --- ARCHITECTURAL UNIQUE PIECE 2: FLANKING ARCHED MAHOGANY BOOKSHELVES ---
    mat_book_red = make_mat("BookRed", (0.55, 0.08, 0.10, 1.0), roughness=0.6)
    mat_book_blue = make_mat("BookBlue", (0.08, 0.18, 0.45, 1.0), roughness=0.6)
    mat_book_green = make_mat("BookGreen", (0.06, 0.35, 0.16, 1.0), roughness=0.6)
    mat_book_gold = make_mat("BookGold", (0.75, 0.60, 0.20, 1.0), roughness=0.4)

    for bx_pos in [-4.8, 4.8]:
        # Bookshelf Cabinet Frame
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos, wall_y - 0.15, 2.20))
        bs_frame = bpy.context.active_object
        bs_frame.name = f"Bookcase_{bx_pos}"
        bs_frame.scale = (2.6, 0.55, 5.2)
        bs_frame.data.materials.append(mat_wood)
        bpy.ops.object.transform_apply(scale=True)

        # Arched Top Molding
        bpy.ops.mesh.primitive_cylinder_add(radius=1.3, depth=0.55, vertices=24, location=(bx_pos, wall_y - 0.15, 4.80))
        arch_top = bpy.context.active_object
        arch_top.rotation_euler = (math.pi / 2, 0, 0)
        arch_top.scale = (1.0, 0.6, 1.0)
        arch_top.data.materials.append(mat_wood)
        bpy.ops.object.transform_apply(scale=True, rotation=True)

        # 4 Shelf Levels with Colorful 3D Books
        for s_idx, sz_val in enumerate([0.2, 1.3, 2.4, 3.5]):
            # Shelf Board
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos, wall_y - 0.25, sz_val))
            shelf = bpy.context.active_object
            shelf.scale = (2.4, 0.45, 0.06)
            shelf.data.materials.append(mat_gold)
            bpy.ops.object.transform_apply(scale=True)

            # Rows of Books
            book_mats = [mat_book_red, mat_book_blue, mat_book_green, mat_book_gold]
            for bk in range(7):
                bx_off = (bk - 3) * 0.30
                bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bx_pos + bx_off, wall_y - 0.25, sz_val + 0.30))
                book = bpy.context.active_object
                book.scale = (0.24, 0.35, 0.52 + (bk % 3) * 0.06)
                book.data.materials.append(book_mats[(bk + s_idx) % 4])
                bpy.ops.object.transform_apply(scale=True)

    # --- ARCHITECTURAL UNIQUE PIECE 3: GRANDFATHER CLOCK (RELÓGIO DE PÊNDULO) ---
    clk_x, clk_y = -8.5, wall_y - 0.20
    # Clock Plinth Base
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(clk_x, clk_y, -0.15))
    c_base = bpy.context.active_object
    c_base.name = "GrandfatherClockBase"
    c_base.scale = (1.1, 0.65, 1.1)
    c_base.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)

    # Clock Middle Waist with Glass Window
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(clk_x, clk_y, 1.45))
    c_waist = bpy.context.active_object
    c_waist.name = "GrandfatherClockWaist"
    c_waist.scale = (0.90, 0.55, 2.10)
    c_waist.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)

    # Brass Pendulum
    bpy.ops.mesh.primitive_cylinder_add(radius=0.14, depth=0.03, vertices=20, location=(clk_x, clk_y - 0.20, 1.15))
    pendulum = bpy.context.active_object
    pendulum.name = "ClockPendulum"
    pendulum.rotation_euler = (math.pi / 2, 0, 0)
    pendulum.data.materials.append(mat_gold)
    bpy.ops.object.transform_apply(rotation=True)

    # Clock Hood Top
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(clk_x, clk_y, 2.95))
    c_hood = bpy.context.active_object
    c_hood.name = "GrandfatherClockHood"
    c_hood.scale = (1.05, 0.65, 0.90)
    c_hood.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)

    # Round Clock Dial
    mat_clock_dial = make_mat("ClockDial", (0.95, 0.93, 0.85, 1.0), roughness=0.3)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.32, depth=0.04, vertices=24, location=(clk_x, clk_y - 0.32, 2.95))
    dial = bpy.context.active_object
    dial.name = "ClockDial"
    dial.rotation_euler = (math.pi / 2, 0, 0)
    dial.data.materials.append(mat_clock_dial)
    bpy.ops.object.transform_apply(rotation=True)

    # Grand Chandelier
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

    file_path = os.path.join(out_dir, "room_classic_club.glb")
    bpy.ops.export_scene.gltf(filepath=file_path, export_format='GLB', use_selection=False, export_apply=True)
    print(f"[Blender] Exported room_classic_club.glb ({os.path.getsize(file_path)} bytes)")

# ==========================================================
# 4. LOUNGE DO BARÃO (barao_lounge) - ARCOS GÓTICOS, CORUJAS & LUAR
# ==========================================================
def build_barao_lounge():
    print("[Blender] Building unique 3D room: barao_lounge (Arcos góticos ogivais, vitrais noturnos, corujas e púrpura)...")
    mats = build_base_room_shell(
        room_id="barao_lounge",
        floor_color=(0.07, 0.06, 0.10, 1.0), floor_rough=0.16, floor_metal=0.12,
        rug_main=(0.06, 0.05, 0.14, 1.0), rug_border=(0.18, 0.14, 0.08, 1.0),
        boiserie_color=(0.11, 0.08, 0.15, 1.0), wall_color=(0.12, 0.08, 0.20, 1.0),
        accent_color=(0.90, 0.75, 0.28, 1.0), drapery_color=(0.10, 0.06, 0.16, 1.0),
        sconce_emission=(0.98, 0.84, 0.52, 1.0), sconce_strength=3.4
    )
    wall_y = mats["wall_y"]
    mat_stone = make_mat("BaraoGothicStone", (0.12, 0.10, 0.14, 1.0), metallic=0.05, roughness=0.75)
    mat_dark_iron = make_mat("BaraoWroughtIron", (0.05, 0.05, 0.06, 1.0), metallic=0.75, roughness=0.45)
    mat_violet_flame = make_mat("BaraoVioletFlame", (0.75, 0.25, 0.95, 1.0), emission=(0.85, 0.35, 1.0, 1.0), emission_strength=4.5)
    mat_moon = make_mat("BaraoMoonNight", (0.04, 0.05, 0.12, 1.0), roughness=0.9)
    mat_moon_disc = make_mat("BaraoMoonDisc", (0.95, 0.95, 1.0, 1.0), emission=(0.95, 0.95, 1.0, 1.0), emission_strength=3.0)
    mat_gold = mats["mat_trim"]
    mat_wood = mats["mat_boiserie"]

    # Gothic Vaulted Ceiling
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 1.5, 7.35))
    ceiling = bpy.context.active_object
    ceiling.scale = (28.0, 18.0, 1.0)
    ceiling.rotation_euler = (math.pi, 0, 0)
    ceiling.data.materials.append(mat_stone)
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    # Pointed cross ribs
    for rib_x in [-7.5, -2.5, 2.5, 7.5]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(rib_x, 1.5, 7.15))
        rib = bpy.context.active_object
        rib.scale = (0.50, 17.6, 0.40)
        rib.data.materials.append(mat_dark_iron)
        bpy.ops.object.transform_apply(scale=True)

    # --- ARCHITECTURAL UNIQUE PIECE 1: TALL POINTED GOTHIC LANCET WINDOWS & MOON ---
    # Dark Midnight Sky Backdrop plane behind windows
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, wall_y + 0.35, 3.8))
    sky = bpy.context.active_object
    sky.name = "GothicSkyBackdrop"
    sky.scale = (26.0, 1.0, 8.0)
    sky.rotation_euler = (math.pi / 2, 0, 0)
    sky.data.materials.append(mat_moon)
    bpy.ops.object.transform_apply(scale=True, rotation=True)

    # Glowing Full Moon disc visible through the left window
    bpy.ops.mesh.primitive_cylinder_add(radius=1.2, depth=0.04, vertices=32, location=(-4.8, wall_y + 0.28, 4.8))
    moon = bpy.context.active_object
    moon.name = "MoonDisc"
    moon.rotation_euler = (math.pi / 2, 0, 0)
    moon.data.materials.append(mat_moon_disc)
    bpy.ops.object.transform_apply(rotation=True)

    # Two Tall Lancet Window Openings with Stone Tracery
    for wx_pos in [-4.8, 4.8]:
        # Window Frame Pillars
        for side_sign in [-1, 1]:
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx_pos + side_sign * 1.35, wall_y - 0.10, 3.4))
            col = bpy.context.active_object
            col.scale = (0.35, 0.40, 5.6)
            col.data.materials.append(mat_stone)
            bpy.ops.object.transform_apply(scale=True)

        # Pointed Gothic Arch Peak
        bpy.ops.mesh.primitive_cone_add(vertices=16, radius1=1.45, radius2=0.0, depth=2.2, location=(wx_pos, wall_y - 0.10, 6.2))
        arch_peak = bpy.context.active_object
        arch_peak.scale = (1.0, 0.35, 1.0)
        arch_peak.data.materials.append(mat_stone)
        bpy.ops.object.transform_apply(scale=True)

        # Stone Tracery Trefoil (Rose Windowette in arch)
        bpy.ops.mesh.primitive_torus_add(major_radius=0.42, minor_radius=0.06, location=(wx_pos, wall_y - 0.12, 5.3))
        trefoil = bpy.context.active_object
        trefoil.rotation_euler = (math.pi / 2, 0, 0)
        trefoil.data.materials.append(mat_dark_iron)
        bpy.ops.object.transform_apply(rotation=True)

    # --- ARCHITECTURAL UNIQUE PIECE 2: GOTHIC STONE FIREPLACE & OWL HERALDRY ---
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.25, 1.0))
    g_fp = bpy.context.active_object
    g_fp.name = "GothicFireplace"
    g_fp.scale = (3.4, 0.85, 3.2)
    g_fp.data.materials.append(mat_stone)
    bpy.ops.object.transform_apply(scale=True)

    # Carved Heraldic Owl Shield above fireplace
    bpy.ops.mesh.primitive_cylinder_add(radius=0.75, depth=0.12, vertices=6, location=(0, wall_y - 0.70, 3.1))
    shield = bpy.context.active_object
    shield.name = "BaronHeraldicShield"
    shield.rotation_euler = (math.pi / 2, 0, 0)
    shield.data.materials.append(mat_gold)
    bpy.ops.object.transform_apply(rotation=True)

    # Violet Embers in the fireplace
    bpy.ops.mesh.primitive_cylinder_add(radius=0.45, depth=0.15, vertices=16, location=(0, wall_y - 0.40, -0.45))
    embers = bpy.context.active_object
    embers.data.materials.append(mat_violet_flame)

    # --- ARCHITECTURAL UNIQUE PIECE 3: SCULPTED STONE OWL GARGOYLES ON PEDESTALS ---
    for ox in [-7.8, 7.8]:
        # High Stone Pedestal
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(ox, wall_y - 0.35, 0.40))
        ped = bpy.context.active_object
        ped.scale = (0.90, 0.90, 2.2)
        ped.data.materials.append(mat_stone)
        bpy.ops.object.transform_apply(scale=True)

        # Sculpted Horned Owl Statue Body
        bpy.ops.mesh.primitive_cylinder_add(radius=0.32, depth=0.90, vertices=16, location=(ox, wall_y - 0.35, 1.85))
        owl_body = bpy.context.active_object
        owl_body.data.materials.append(mat_stone)

        # Owl Head & Horn Tufts
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.28, location=(ox, wall_y - 0.35, 2.45))
        owl_head = bpy.context.active_object
        owl_head.data.materials.append(mat_stone)
        for h_sign in [-1, 1]:
            bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=0.08, depth=0.22, location=(ox + h_sign * 0.16, wall_y - 0.35, 2.75))
            tuft = bpy.context.active_object
            tuft.data.materials.append(mat_stone)

    # --- ARCHITECTURAL UNIQUE PIECE 4: WROUGHT-IRON MULTI-BRANCH CANDELABRAS ---
    for cx_pos in [-2.4, 2.4]:
        # Iron Stand
        bpy.ops.mesh.primitive_cylinder_add(radius=0.045, depth=2.4, vertices=12, location=(cx_pos, wall_y - 0.85, 0.50))
        cand_shaft = bpy.context.active_object
        cand_shaft.data.materials.append(mat_dark_iron)
        for ca_idx in range(5):
            c_ang = ca_idx * (math.pi / 4)
            cax = cx_pos + math.cos(c_ang) * 0.35
            cay = wall_y - 0.85 + math.sin(c_ang) * 0.35
            bpy.ops.mesh.primitive_cylinder_add(radius=0.035, depth=0.22, vertices=12, location=(cax, cay, 1.75))
            candle = bpy.context.active_object
            candle.data.materials.append(mat_violet_flame)

    # Heavy Wrought-Iron Gothic Chandelier
    ch_x, ch_y, ch_z = 0.0, 0.0, 5.25
    bpy.ops.mesh.primitive_torus_add(major_radius=1.5, minor_radius=0.08, location=(ch_x, ch_y, ch_z))
    iron_ring = bpy.context.active_object
    iron_ring.data.materials.append(mat_dark_iron)
    for ca_idx in range(8):
        c_ang = ca_idx * (2 * math.pi / 8)
        cax = ch_x + math.cos(c_ang) * 1.5
        cay = ch_y + math.sin(c_ang) * 1.5
        bpy.ops.mesh.primitive_cylinder_add(radius=0.05, depth=0.26, vertices=12, location=(cax, cay, ch_z + 0.18))
        c_light = bpy.context.active_object
        c_light.data.materials.append(mat_violet_flame)

    file_path = os.path.join(out_dir, "room_barao_lounge.glb")
    bpy.ops.export_scene.gltf(filepath=file_path, export_format='GLB', use_selection=False, export_apply=True)
    print(f"[Blender] Exported room_barao_lounge.glb ({os.path.getsize(file_path)} bytes)")

# ==========================================================
# 5. SALÃO DA DAMA (dama_salon) - BELLE ÉPOQUE, ESPELHOS & CHAMPANHE
# ==========================================================
def build_dama_salon():
    print("[Blender] Building unique 3D room: dama_salon (Belle Époque, espelhos ovais dourados, carrinho de champanhe e rosas)...")
    mats = build_base_room_shell(
        room_id="dama_salon",
        floor_color=(0.16, 0.05, 0.07, 1.0), floor_rough=0.18, floor_metal=0.08,
        rug_main=(0.20, 0.04, 0.08, 1.0), rug_border=(0.22, 0.12, 0.06, 1.0),
        boiserie_color=(0.19, 0.07, 0.09, 1.0), wall_color=(0.28, 0.06, 0.11, 1.0),
        accent_color=(0.90, 0.73, 0.30, 1.0), drapery_color=(0.24, 0.04, 0.08, 1.0),
        sconce_emission=(1.0, 0.86, 0.66, 1.0), sconce_strength=3.1
    )
    wall_y = mats["wall_y"]
    mat_gold = mats["mat_trim"]
    mat_rose_wall = mats["mat_wall"]
    mat_white_marble = make_mat("DamaWhiteMarble", (0.95, 0.94, 0.92, 1.0), metallic=0.08, roughness=0.12)
    mat_mirror = make_mat("DamaMirrorGlass", (0.92, 0.94, 0.98, 1.0), metallic=0.95, roughness=0.04)
    mat_rose_petals = make_mat("DamaRedRoses", (0.75, 0.04, 0.12, 1.0), roughness=0.55)
    mat_champagne_bottle = make_mat("DamaChampagneBottle", (0.10, 0.28, 0.14, 0.9), metallic=0.1, roughness=0.1)
    mat_champagne_flute = make_mat("DamaCrystalFlute", (0.92, 0.96, 1.0, 0.65), metallic=0.15, roughness=0.05)
    mat_sconce = mats["mat_sconce"]

    # Belle Époque Ceiling with Floral Medallion
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 1.5, 7.35))
    ceiling = bpy.context.active_object
    ceiling.scale = (28.0, 18.0, 1.0)
    ceiling.rotation_euler = (math.pi, 0, 0)
    ceiling.data.materials.append(mat_white_marble)
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    # Gilded curved ceiling moldings
    for r_mold in [2.2, 4.5, 6.8]:
        bpy.ops.mesh.primitive_torus_add(major_radius=r_mold, minor_radius=0.08, location=(0, 1.5, 7.26))
        c_ring = bpy.context.active_object
        c_ring.data.materials.append(mat_gold)

    # --- ARCHITECTURAL UNIQUE PIECE 1: GRAND GILDED OVAL FLOOR MIRRORS (ESPELHOS ROCOCÓ) ---
    for mx_pos in [-5.8, 5.8]:
        # Ornate Oval Mirror Frame
        bpy.ops.mesh.primitive_cylinder_add(radius=1.35, depth=0.08, vertices=32, location=(mx_pos, wall_y - 0.22, 3.4))
        m_frame = bpy.context.active_object
        m_frame.name = f"OvalMirrorFrame_{mx_pos}"
        m_frame.rotation_euler = (math.pi / 2, 0, 0)
        m_frame.scale = (1.0, 1.55, 1.0)
        m_frame.data.materials.append(mat_gold)
        bpy.ops.object.transform_apply(scale=True, rotation=True)

        # Highly Reflective Mirror Glass Plane
        bpy.ops.mesh.primitive_cylinder_add(radius=1.20, depth=0.04, vertices=32, location=(mx_pos, wall_y - 0.24, 3.4))
        m_glass = bpy.context.active_object
        m_glass.name = f"OvalMirrorGlass_{mx_pos}"
        m_glass.rotation_euler = (math.pi / 2, 0, 0)
        m_glass.scale = (1.0, 1.55, 1.0)
        m_glass.data.materials.append(mat_mirror)
        bpy.ops.object.transform_apply(scale=True, rotation=True)

        # Rococo Gilded Crest at top of mirror
        bpy.ops.mesh.primitive_cone_add(vertices=16, radius1=0.55, radius2=0.0, depth=0.45, location=(mx_pos, wall_y - 0.24, 5.6))
        crest = bpy.context.active_object
        crest.data.materials.append(mat_gold)

    # --- ARCHITECTURAL UNIQUE PIECE 2: ROLLING CHAMPAGNE BAR CART (CHARIOT À CHAMPAGNE) ---
    cart_x, cart_y = 0.0, wall_y - 0.90
    # Polished Brass 2-Tier Cart Frame
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cart_x, cart_y, 0.40))
    cart_top = bpy.context.active_object
    cart_top.scale = (2.2, 1.1, 0.05)
    cart_top.data.materials.append(mat_white_marble)
    bpy.ops.object.transform_apply(scale=True)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cart_x, cart_y, -0.15))
    cart_bottom = bpy.context.active_object
    cart_bottom.scale = (2.2, 1.1, 0.05)
    cart_bottom.data.materials.append(mat_gold)
    bpy.ops.object.transform_apply(scale=True)

    # Spoked Rolling Brass Wheels
    for wx_sign, wy_sign in [(-1, -1), (-1, 1), (1, -1), (1, 1)]:
        bpy.ops.mesh.primitive_torus_add(major_radius=0.28, minor_radius=0.03, location=(cart_x + wx_sign * 1.05, cart_y + wy_sign * 0.50, -0.42))
        wheel = bpy.context.active_object
        wheel.rotation_euler = (0, math.pi / 2, 0)
        wheel.data.materials.append(mat_gold)
        bpy.ops.object.transform_apply(rotation=True)

    # Champagne Ice Bucket with Ice and Dom Pérignon Bottles
    bpy.ops.mesh.primitive_cylinder_add(radius=0.32, depth=0.42, vertices=24, location=(cart_x - 0.45, cart_y, 0.62))
    bucket = bpy.context.active_object
    bucket.data.materials.append(mat_gold)

    for bx_off, b_rot in [(-0.10, 0.15), (0.10, -0.15)]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.085, depth=0.55, vertices=16, location=(cart_x - 0.45 + bx_off, cart_y, 0.82))
        champ = bpy.context.active_object
        champ.rotation_euler = (0, b_rot, 0)
        champ.data.materials.append(mat_champagne_bottle)
        bpy.ops.object.transform_apply(rotation=True)

    # Cluster of Crystal Champagne Flutes
    for fx_idx in range(5):
        for fy_idx in range(2):
            fx = cart_x + 0.25 + fx_idx * 0.16
            fy = cart_y - 0.20 + fy_idx * 0.24
            bpy.ops.mesh.primitive_cylinder_add(radius=0.045, depth=0.28, vertices=16, location=(fx, fy, 0.55))
            flute = bpy.context.active_object
            flute.data.materials.append(mat_champagne_flute)

    # --- ARCHITECTURAL UNIQUE PIECE 3: MARBLE URNS WITH BOUQUETS OF SCARLET ROSES ---
    for rx_pos in [-2.6, 2.6]:
        # White Carrara Marble Pedestal
        bpy.ops.mesh.primitive_cylinder_add(radius=0.32, depth=1.6, vertices=20, location=(rx_pos, wall_y - 0.55, 0.10))
        ped = bpy.context.active_object
        ped.data.materials.append(mat_white_marble)

        # Classical Golden Urn
        bpy.ops.mesh.primitive_cylinder_add(radius=0.42, depth=0.45, vertices=24, location=(rx_pos, wall_y - 0.55, 1.10))
        urn = bpy.context.active_object
        urn.data.materials.append(mat_gold)

        # Overflowing Dome of Scarlet Velvet Roses
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=0.55, location=(rx_pos, wall_y - 0.55, 1.55))
        rose_bouquet = bpy.context.active_object
        rose_bouquet.data.materials.append(mat_rose_petals)

    # Rose-Gold Grand Chandelier
    ch_x, ch_y, ch_z = 0.0, 0.0, 5.25
    bpy.ops.mesh.primitive_cylinder_add(radius=0.45, depth=0.12, vertices=24, location=(ch_x, ch_y, 7.28))
    canopy = bpy.context.active_object
    canopy.data.materials.append(mat_gold)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.038, depth=1.80, vertices=16, location=(ch_x, ch_y, 6.35))
    ch_rod = bpy.context.active_object
    ch_rod.data.materials.append(mat_gold)
    for a_idx in range(8):
        angle = a_idx * (2 * math.pi / 8)
        ax = ch_x + math.cos(angle) * 1.35
        ay = ch_y + math.sin(angle) * 1.35
        bpy.ops.mesh.primitive_cylinder_add(radius=0.055, depth=0.22, vertices=16, location=(ax, ay, ch_z + 0.30))
        c_lamp = bpy.context.active_object
        c_lamp.data.materials.append(mat_sconce)

    file_path = os.path.join(out_dir, "room_dama_salon.glb")
    bpy.ops.export_scene.gltf(filepath=file_path, export_format='GLB', use_selection=False, export_apply=True)
    print(f"[Blender] Exported room_dama_salon.glb ({os.path.getsize(file_path)} bytes)")

# ==========================================================
# 6. CASSINO CYBER (cyber_casino) - SKYLINE NEON & HOLOGRAFIA
# ==========================================================
def build_cyber_casino():
    print("[Blender] Building unique 3D room: cyber_casino (Janela panorâmica com skyline cyberpunk, LED e holografia)...")
    mats = build_base_room_shell(
        room_id="cyber_casino",
        floor_color=(0.04, 0.05, 0.07, 1.0), floor_rough=0.14, floor_metal=0.25,
        rug_main=(0.05, 0.07, 0.10, 1.0), rug_border=(0.10, 0.25, 0.30, 1.0),
        boiserie_color=(0.06, 0.08, 0.11, 1.0), wall_color=(0.08, 0.11, 0.15, 1.0),
        accent_color=(0.92, 0.78, 0.28, 1.0), drapery_color=(0.05, 0.09, 0.14, 1.0),
        sconce_emission=(0.18, 0.88, 0.98, 1.0), sconce_strength=4.0
    )
    wall_y = mats["wall_y"]
    mat_carbon = make_mat("CyberCarbon", (0.05, 0.06, 0.08, 1.0), metallic=0.45, roughness=0.35)
    mat_neon_cyan = make_mat("CyberNeonCyan", (0.15, 0.90, 1.0, 1.0), emission=(0.10, 0.95, 1.0, 1.0), emission_strength=7.0)
    mat_neon_magenta = make_mat("CyberNeonMagenta", (1.0, 0.15, 0.75, 1.0), emission=(1.0, 0.10, 0.80, 1.0), emission_strength=6.5)
    mat_skyline_dark = make_mat("CyberSkyDark", (0.02, 0.02, 0.04, 1.0), roughness=0.95)
    mat_skyscraper = make_mat("CyberBuilding", (0.03, 0.04, 0.06, 1.0), metallic=0.6, roughness=0.4)
    mat_window_glow = make_mat("CyberWindowGlow", (1.0, 0.85, 0.30, 1.0), emission=(1.0, 0.85, 0.30, 1.0), emission_strength=4.0)

    # Floor Cyan Laser Inlay Lines
    for fx_line in [-5.0, -2.5, 2.5, 5.0]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(fx_line, 1.5, -0.722))
        laser = bpy.context.active_object
        laser.scale = (0.06, 17.6, 0.01)
        laser.data.materials.append(mat_neon_cyan)
        bpy.ops.object.transform_apply(scale=True)

    # Sleek Hexagonal Ceiling Grid
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 1.5, 7.35))
    ceiling = bpy.context.active_object
    ceiling.scale = (28.0, 18.0, 1.0)
    ceiling.rotation_euler = (math.pi, 0, 0)
    ceiling.data.materials.append(mat_carbon)
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    for cx_led in [-6.0, 0.0, 6.0]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx_led, 1.5, 7.28))
        c_led = bpy.context.active_object
        c_led.scale = (0.12, 17.6, 0.06)
        c_led.data.materials.append(mat_neon_cyan)
        bpy.ops.object.transform_apply(scale=True)

    # --- ARCHITECTURAL UNIQUE PIECE 1: GIANT PANORAMIC WINDOW & CYBERPUNK SKYLINE ---
    # Dark Skyline Backdrop Plane (Y = wall_y + 2.5m)
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, wall_y + 2.5, 4.0))
    skyline_plane = bpy.context.active_object
    skyline_plane.name = "CyberSkylineBackdrop"
    skyline_plane.scale = (32.0, 1.0, 10.0)
    skyline_plane.rotation_euler = (math.pi / 2, 0, 0)
    skyline_plane.data.materials.append(mat_skyline_dark)
    bpy.ops.object.transform_apply(scale=True, rotation=True)

    # 3D Cyberpunk Skyscraper Silhouettes Outside the Window
    building_data = [
        (-9.5, 2.0, 4.8, 11.0, mat_neon_cyan),
        (-6.0, 1.8, 3.8, 14.5, mat_neon_magenta),
        (-2.5, 2.1, 4.2, 9.5, mat_window_glow),
        ( 1.5, 1.9, 4.5, 16.0, mat_neon_cyan),
        ( 5.5, 2.2, 3.6, 12.0, mat_window_glow),
        ( 9.0, 1.8, 5.0, 10.5, mat_neon_magenta),
    ]
    for b_x, b_y_off, b_w, b_h, b_glow_mat in building_data:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(b_x, wall_y + b_y_off, b_h / 2 - 0.72))
        tower = bpy.context.active_object
        tower.name = f"SkyTower_{b_x}"
        tower.scale = (b_w, 1.2, b_h)
        tower.data.materials.append(mat_skyscraper)
        bpy.ops.object.transform_apply(scale=True)

        # Rooftop Antenna with glowing beacon
        bpy.ops.mesh.primitive_cylinder_add(radius=0.08, depth=2.4, vertices=12, location=(b_x, wall_y + b_y_off, b_h + 0.5))
        ant = bpy.context.active_object
        ant.data.materials.append(mat_carbon)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.22, location=(b_x, wall_y + b_y_off, b_h + 1.7))
        beacon = bpy.context.active_object
        beacon.data.materials.append(b_glow_mat)

        # Window Grid Strips on Tower Facade
        for wy_level in range(3, int(b_h) - 1, 2):
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(b_x, wall_y + b_y_off - 0.62, wy_level))
            win_strip = bpy.context.active_object
            win_strip.scale = (b_w * 0.75, 0.05, 0.35)
            win_strip.data.materials.append(b_glow_mat)
            bpy.ops.object.transform_apply(scale=True)

    # Sleek Titanium Window Mullions Framing the View
    for mx in [-7.2, -3.6, 0.0, 3.6, 7.2]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(mx, wall_y - 0.08, 3.5))
        mullion = bpy.context.active_object
        mullion.scale = (0.24, 0.30, 7.8)
        mullion.data.materials.append(mat_carbon)
        bpy.ops.object.transform_apply(scale=True)
        # Neon edge strip on mullion
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(mx, wall_y - 0.22, 3.5))
        m_neon = bpy.context.active_object
        m_neon.scale = (0.05, 0.04, 7.6)
        m_neon.data.materials.append(mat_neon_cyan if mx % 2 == 0 else mat_neon_magenta)
        bpy.ops.object.transform_apply(scale=True)

    # --- ARCHITECTURAL UNIQUE PIECE 2: FLOATING HOLOGRAPHIC CARD PROJECTOR RING ---
    ch_x, ch_y, ch_z = 0.0, 0.0, 4.85
    # Ceiling projector disc
    bpy.ops.mesh.primitive_cylinder_add(radius=1.8, depth=0.18, vertices=32, location=(ch_x, ch_y, 7.20))
    proj = bpy.context.active_object
    proj.data.materials.append(mat_carbon)

    # Floating Glowing Hologram Ring
    bpy.ops.mesh.primitive_torus_add(major_radius=1.6, minor_radius=0.04, location=(ch_x, ch_y, ch_z))
    holo_ring = bpy.context.active_object
    holo_ring.name = "HologramRing"
    holo_ring.data.materials.append(mat_neon_cyan)

    # Floating Hologram Card Nodes
    for h_idx in range(6):
        h_ang = h_idx * (2 * math.pi / 6)
        hx = ch_x + math.cos(h_ang) * 1.6
        hy = ch_y + math.sin(h_ang) * 1.6
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(hx, hy, ch_z))
        holo_card = bpy.context.active_object
        holo_card.scale = (0.35, 0.04, 0.52)
        holo_card.rotation_euler = (0, 0, h_ang + math.pi / 2)
        holo_card.data.materials.append(mat_neon_magenta if h_idx % 2 == 0 else mat_neon_cyan)
        bpy.ops.object.transform_apply(scale=True, rotation=True)

    # --- ARCHITECTURAL UNIQUE PIECE 3: HIGH-TECH CYBER SERVER & SYNTHESIZER BAR ---
    bar_x, bar_y = 7.5, wall_y - 0.70
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bar_x, bar_y, 0.45))
    bar_stand = bpy.context.active_object
    bar_stand.scale = (3.2, 0.90, 2.3)
    bar_stand.data.materials.append(mat_carbon)
    bpy.ops.object.transform_apply(scale=True)
    # Glowing readout stripes on server rack
    for r_idx in range(6):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(bar_x, bar_y - 0.46, 0.10 + r_idx * 0.28))
        r_strip = bpy.context.active_object
        r_strip.scale = (2.8, 0.04, 0.08)
        r_strip.data.materials.append(mat_neon_cyan if r_idx % 2 == 0 else mat_neon_magenta)
        bpy.ops.object.transform_apply(scale=True)

    file_path = os.path.join(out_dir, "room_cyber_casino.glb")
    bpy.ops.export_scene.gltf(filepath=file_path, export_format='GLB', use_selection=False, export_apply=True)
    print(f"[Blender] Exported room_cyber_casino.glb ({os.path.getsize(file_path)} bytes)")

def main():
    build_club_chair()
    build_classic_club()
    build_barao_lounge()
    build_dama_salon()
    build_cyber_casino()
    print("\n[Blender] All 4 architecturally unique 3D club rooms and luxury chairs generated successfully!")

if __name__ == "__main__":
    main()
