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
    bev_bk.width = 0.04
    bev_bk.segments = 3

    # Back Wood Enclosure Frame
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.35, 0.72))
    back_wood = bpy.context.active_object
    back_wood.name = "Backrest_Wood"
    back_wood.scale = (0.76, 0.04, 0.54)
    back_wood.data.materials.append(mat_wood)
    bpy.ops.object.transform_apply(scale=True)

    # Armrests (Left & Right)
    for sign, side in [(-1, "L"), (1, "R")]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sign * 0.37, 0.06, 0.62))
        arm = bpy.context.active_object
        arm.name = f"ArmrestPad_{side}"
        arm.scale = (0.10, 0.54, 0.07)
        arm.data.materials.append(mat_velvet)
        bpy.ops.object.transform_apply(scale=True)
        bev_a = arm.modifiers.new("Bevel", "BEVEL")
        bev_a.width = 0.025
        bev_a.segments = 3

        bpy.ops.mesh.primitive_cylinder_add(radius=0.03, depth=0.22, vertices=16, location=(sign * 0.37, -0.16, 0.46))
        post_front = bpy.context.active_object
        post_front.name = f"ArmPostFront_{side}"
        post_front.data.materials.append(mat_wood)

        bpy.ops.mesh.primitive_cylinder_add(radius=0.026, depth=0.22, vertices=16, location=(sign * 0.37, 0.24, 0.46))
        post_back = bpy.context.active_object
        post_back.name = f"ArmPostBack_{side}"
        post_back.data.materials.append(mat_wood)

    # Four Splayed Turned Legs with Brass Ferrules
    leg_coords = [
        (-0.29, -0.26, "FrontL", 0.08, -0.08),
        ( 0.29, -0.26, "FrontR", 0.08,  0.08),
        (-0.29,  0.24, "BackL", -0.09, -0.08),
        ( 0.29,  0.24, "BackR", -0.09,  0.08)
    ]
    for lx, ly, name, tilt_x, tilt_y in leg_coords:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.034, depth=0.26, vertices=16, location=(lx, ly, 0.16))
        leg = bpy.context.active_object
        leg.name = f"Leg_{name}"
        leg.rotation_euler = (tilt_x, tilt_y, 0)
        leg.data.materials.append(mat_wood)
        bpy.ops.object.transform_apply(rotation=True)

        bpy.ops.mesh.primitive_cylinder_add(radius=0.036, depth=0.06, vertices=16, location=(lx + tilt_y*0.22, ly - tilt_x*0.22, 0.03))
        ferrule = bpy.context.active_object
        ferrule.name = f"Ferrule_{name}"
        ferrule.data.materials.append(mat_brass)

    chair_path = os.path.join(out_dir, "club_chair.glb")
    bpy.ops.export_scene.gltf(filepath=chair_path, export_format='GLB', use_selection=False, export_apply=True)
    print(f"[Blender] Saved club_chair.glb ({os.path.getsize(chair_path)} bytes)")

# ==========================================================
# 2. BUILD 3D ROOM ENVIRONMENTS (Blender +Y = glTF -Z North)
# ==========================================================
def build_room(room_id, floor_color, floor_roughness, floor_metallic,
               rug_main_color, rug_border_color,
               boiserie_color, wall_color, sconce_emission, sconce_strength,
               accent_color, drapery_color):
    clear_scene()
    print(f"[Blender] Building rich 3D room: {room_id}...")

    mat_floor = make_mat(f"{room_id}_Floor", floor_color, metallic=floor_metallic, roughness=floor_roughness)
    mat_rug_main = make_mat(f"{room_id}_RugMain", rug_main_color, metallic=0.02, roughness=0.88)
    mat_rug_border = make_mat(f"{room_id}_RugBorder", rug_border_color, metallic=0.15, roughness=0.65)
    mat_boiserie = make_mat(f"{room_id}_Boiserie", boiserie_color, metallic=0.0, roughness=0.35)
    mat_boiserie_panel = make_mat(f"{room_id}_Panel", (boiserie_color[0]*0.82, boiserie_color[1]*0.82, boiserie_color[2]*0.82, 1.0), metallic=0.0, roughness=0.42)
    mat_wall = make_mat(f"{room_id}_Wall", wall_color, metallic=0.0, roughness=0.72)
    mat_trim = make_mat(f"{room_id}_Trim", accent_color, metallic=0.85, roughness=0.22)
    mat_drapery = make_mat(f"{room_id}_Drapery", drapery_color, metallic=0.04, roughness=0.85)
    mat_sconce = make_mat(f"{room_id}_Sconce", sconce_emission, metallic=0.1, roughness=0.1,
                          emission=sconce_emission, emission_strength=sconce_strength)

    # In Blender coordinate system:
    # +Z is Up (glTF +Y)
    # +Y is North / Background into screen (glTF -Z)
    # -Y is South / Foreground toward camera (glTF +Z)
    # +X is East / Right (glTF +X)

    # --- 1. POLISHED WOOD / MARBLE FLOOR ---
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 1.5, -0.73))
    floor = bpy.context.active_object
    floor.name = "Floor"
    floor.scale = (28.0, 18.0, 1.0)
    floor.data.materials.append(mat_floor)
    bpy.ops.object.transform_apply(scale=True)

    # --- 2. LUXURY CASINO RUG UNDER TABLE ---
    # Outer Border
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.0, -0.718))
    rug_outer = bpy.context.active_object
    rug_outer.name = "ClubRug_Outer"
    rug_outer.scale = (14.2, 9.8, 0.016)
    rug_outer.data.materials.append(mat_rug_border)
    bpy.ops.object.transform_apply(scale=True)

    # Gold Inlay Stripe
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.0, -0.714))
    rug_stripe = bpy.context.active_object
    rug_stripe.name = "ClubRug_Stripe"
    rug_stripe.scale = (13.6, 9.2, 0.018)
    rug_stripe.data.materials.append(mat_trim)
    bpy.ops.object.transform_apply(scale=True)

    # Inner Velvet/Wool Field
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.0, -0.710))
    rug_inner = bpy.context.active_object
    rug_inner.name = "ClubRug_Inner"
    rug_inner.scale = (13.2, 8.8, 0.020)
    rug_inner.data.materials.append(mat_rug_main)
    bpy.ops.object.transform_apply(scale=True)

    # Corner Accents on Rug
    for rx, ry in [(-6.2, -4.0), (6.2, -4.0), (-6.2, 4.0), (6.2, 4.0)]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(rx, ry, -0.706))
        cmed = bpy.context.active_object
        cmed.name = f"RugMedallion_{rx}_{ry}"
        cmed.scale = (0.55, 0.55, 0.022)
        cmed.data.materials.append(mat_trim)
        bpy.ops.object.transform_apply(scale=True)

    # --- 3. BACK WALL (Brought to Y = +6.4 -> glTF Z = -6.4 so it frames the table prominently) ---
    wall_y = 6.4

    # Plinth Baseboard (Z = -0.72 to -0.22, height 0.50m)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y, -0.47))
    baseboard = bpy.context.active_object
    baseboard.name = "Baseboard"
    baseboard.scale = (28.0, 0.42, 0.50)
    baseboard.data.materials.append(mat_boiserie)
    bpy.ops.object.transform_apply(scale=True)

    # Baseboard Gold Cap
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.08, -0.21))
    bb_cap = bpy.context.active_object
    bb_cap.name = "BaseboardCap"
    bb_cap.scale = (28.0, 0.28, 0.04)
    bb_cap.data.materials.append(mat_trim)
    bpy.ops.object.transform_apply(scale=True)

    # Lower Wainscot Boiserie Wall (Z = -0.22 to 2.10, height 2.32m)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y, 0.94))
    lower_wall = bpy.context.active_object
    lower_wall.name = "BackWall_Lower"
    lower_wall.scale = (28.0, 0.38, 2.32)
    lower_wall.data.materials.append(mat_boiserie)
    bpy.ops.object.transform_apply(scale=True)

    # Recessed Boiserie Panels with Raised Moldings (6 Panels across the back wall)
    panel_xs = [-9.5, -5.5, -1.8, 1.8, 5.5, 9.5]
    for px in panel_xs:
        # Outer Gold Frame
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, wall_y - 0.20, 0.94))
        p_frame = bpy.context.active_object
        p_frame.name = f"BoiserieFrame_{px}"
        p_frame.scale = (2.6, 0.06, 1.70)
        p_frame.data.materials.append(mat_trim)
        bpy.ops.object.transform_apply(scale=True)

        # Recessed Panel Field
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(px, wall_y - 0.18, 0.94))
        p_field = bpy.context.active_object
        p_field.name = f"BoiseriePanel_{px}"
        p_field.scale = (2.4, 0.05, 1.52)
        p_field.data.materials.append(mat_boiserie_panel)
        bpy.ops.object.transform_apply(scale=True)

    # Gold Chair Rail Divider at Z = 2.10m
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.10, 2.12))
    rail = bpy.context.active_object
    rail.name = "ChairRail"
    rail.scale = (28.0, 0.22, 0.12)
    rail.data.materials.append(mat_trim)
    bpy.ops.object.transform_apply(scale=True)

    # Upper Damask Wallpaper (Z = 2.18 to 7.20, height 5.02m)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y, 4.69))
    upper_wall = bpy.context.active_object
    upper_wall.name = "BackWall_Upper"
    upper_wall.scale = (28.0, 0.35, 5.02)
    upper_wall.data.materials.append(mat_wall)
    bpy.ops.object.transform_apply(scale=True)

    # Upper Wallpaper Trim Moldings (Decorative vertical stiles)
    for ux in [-7.5, -3.8, 0.0, 3.8, 7.5]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(ux, wall_y - 0.16, 4.69))
        stile = bpy.context.active_object
        stile.name = f"WallStile_{ux}"
        stile.scale = (0.08, 0.06, 5.0)
        stile.data.materials.append(mat_trim)
        bpy.ops.object.transform_apply(scale=True)

    # Ceiling Crown Molding at Z = 7.15m
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.12, 7.15))
    crown = bpy.context.active_object
    crown.name = "CrownMolding"
    crown.scale = (28.0, 0.35, 0.28)
    crown.data.materials.append(mat_boiserie)
    bpy.ops.object.transform_apply(scale=True)

    # Crown Molding Gold Dentil Strip
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, wall_y - 0.18, 7.04))
    dentil = bpy.context.active_object
    dentil.name = "CrownDentil"
    dentil.scale = (28.0, 0.15, 0.06)
    dentil.data.materials.append(mat_trim)
    bpy.ops.object.transform_apply(scale=True)

    # --- 4. FLUTED ARCHITECTURAL PILASTERS / COLUMNS ---
    for cx in [-7.5, -2.5, 2.5, 7.5]:
        # Plinth Base (Z = -0.72 to -0.15)
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx, wall_y - 0.22, -0.42))
        col_base = bpy.context.active_object
        col_base.name = f"PilasterBase_{cx}"
        col_base.scale = (0.78, 0.26, 0.60)
        col_base.data.materials.append(mat_boiserie)
        bpy.ops.object.transform_apply(scale=True)

        # Fluted Column Shaft
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx, wall_y - 0.20, 3.42))
        col = bpy.context.active_object
        col.name = f"PilasterShaft_{cx}"
        col.scale = (0.64, 0.22, 7.08)
        col.data.materials.append(mat_boiserie)
        bpy.ops.object.transform_apply(scale=True)

        # Flute Inlays
        for fx_offset in [-0.20, 0.0, 0.20]:
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx + fx_offset, wall_y - 0.31, 3.42))
            flute = bpy.context.active_object
            flute.name = f"PilasterFlute_{cx}_{fx_offset}"
            flute.scale = (0.06, 0.03, 6.8)
            flute.data.materials.append(mat_trim)
            bpy.ops.object.transform_apply(scale=True)

        # Capital Top at Z = 6.95m
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(cx, wall_y - 0.24, 6.95))
        cap = bpy.context.active_object
        cap.name = f"PilasterCap_{cx}"
        cap.scale = (0.82, 0.30, 0.26)
        cap.data.materials.append(mat_trim)
        bpy.ops.object.transform_apply(scale=True)

    # --- 5. ORNATE WALL ART / PAINTING FRAMES ---
    paintings = [
        (-5.0, 4.4, (2.6, 0.12, 3.2), (2.2, 0.04, 2.8), "LeftArt"),
        ( 0.0, 4.7, (3.2, 0.14, 3.6), (2.8, 0.05, 3.2), "CenterGrandArt"),
        ( 5.0, 4.4, (2.6, 0.12, 3.2), (2.2, 0.04, 2.8), "RightArt")
    ]
    for fx, fz, fscale, cscale, fname in paintings:
        # Ornate Beveled Gold Frame
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(fx, wall_y - 0.22, fz))
        frame = bpy.context.active_object
        frame.name = f"FrameOuter_{fname}"
        frame.scale = fscale
        frame.data.materials.append(mat_trim)
        bpy.ops.object.transform_apply(scale=True)
        bev_f = frame.modifiers.new("Bevel", "BEVEL")
        bev_f.width = 0.04
        bev_f.segments = 3

        # Canvas Artwork
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(fx, wall_y - 0.25, fz))
        canvas = bpy.context.active_object
        canvas.name = f"FrameCanvas_{fname}"
        canvas.scale = cscale
        canvas_color = (drapery_color[0]*0.55, drapery_color[1]*0.55, drapery_color[2]*0.55, 1.0)
        canvas.data.materials.append(make_mat(f"{fname}_Canvas", canvas_color, metallic=0.08, roughness=0.80))
        bpy.ops.object.transform_apply(scale=True)

    # --- 6. VELVET DRAPERY / CURTAINS ON FLANKS ---
    for sign, dside in [(-1, "Left"), (1, "Right")]:
        dx = sign * 10.8
        # Drapery Pelmet / Valance Box at Top
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(dx, wall_y - 0.32, 6.80))
        valance = bpy.context.active_object
        valance.name = f"Valance_{dside}"
        valance.scale = (2.8, 0.38, 0.45)
        valance.data.materials.append(mat_drapery)
        bpy.ops.object.transform_apply(scale=True)

        # Main Hanging Drapes
        for fold in range(4):
            fx_curtain = dx + (fold - 1.5) * 0.55
            bpy.ops.mesh.primitive_cylinder_add(radius=0.22, depth=7.2, vertices=16, location=(fx_curtain, wall_y - 0.26, 3.2))
            drape_col = bpy.context.active_object
            drape_col.name = f"DraperyFold_{dside}_{fold}"
            drape_col.data.materials.append(mat_drapery)

        # Gold Tieback Band at Z = 2.4m
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(dx, wall_y - 0.38, 2.4))
        tieback = bpy.context.active_object
        tieback.name = f"Tieback_{dside}"
        tieback.scale = (2.4, 0.16, 0.12)
        tieback.data.materials.append(mat_trim)
        bpy.ops.object.transform_apply(scale=True)

    # --- 7. WALL SCONCES (ARANDELAS DE LATÃO COM LUZ) ---
    for sx in [-4.0, 0.0, 4.0]:
        # Wall Mounting Rosette
        bpy.ops.mesh.primitive_cylinder_add(radius=0.12, depth=0.08, vertices=16, location=(sx, wall_y - 0.18, 3.8))
        mount = bpy.context.active_object
        mount.name = f"SconceMount_{sx}"
        mount.rotation_euler = (1.57, 0, 0)
        mount.data.materials.append(mat_trim)
        bpy.ops.object.transform_apply(rotation=True)

        # Ornate Double Scroll Arm
        bpy.ops.mesh.primitive_cylinder_add(radius=0.032, depth=0.36, vertices=16, location=(sx, wall_y - 0.32, 3.92))
        stem = bpy.context.active_object
        stem.name = f"SconceStem_{sx}"
        stem.rotation_euler = (0.28, 0, 0)
        stem.data.materials.append(mat_trim)
        bpy.ops.object.transform_apply(rotation=True)

        # Brass Candle Cup
        bpy.ops.mesh.primitive_cylinder_add(radius=0.085, depth=0.08, vertices=16, location=(sx, wall_y - 0.44, 4.04))
        cup = bpy.context.active_object
        cup.name = f"SconceCup_{sx}"
        cup.data.materials.append(mat_trim)

        # Glowing Candle / Flame Lamp
        bpy.ops.mesh.primitive_cylinder_add(radius=0.062, depth=0.24, vertices=16, location=(sx, wall_y - 0.44, 4.22))
        lamp = bpy.context.active_object
        lamp.name = f"SconceLamp_{sx}"
        lamp.data.materials.append(mat_sconce)

    # --- 8. SIDE WALLS (X = -13.5, +13.5) ---
    for sign, side in [(-1, "Left"), (1, "Right")]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sign * 13.5, 1.5, 3.1))
        sw = bpy.context.active_object
        sw.name = f"SideWall_{side}"
        sw.scale = (0.6, 18.0, 7.8)
        sw.data.materials.append(mat_boiserie)
        bpy.ops.object.transform_apply(scale=True)

    # Export to GLB
    file_path = os.path.join(out_dir, f"room_{room_id}.glb")
    bpy.ops.export_scene.gltf(filepath=file_path, export_format='GLB', use_selection=False, export_apply=True)
    print(f"[Blender] Successfully exported rich room_{room_id}.glb ({os.path.getsize(file_path)} bytes)")

def main():
    build_club_chair()

    # A) Classic Club: Mahogany boiserie, emerald damask wallpaper, chevron floor, forest green rug
    build_room(
        room_id="classic_club",
        floor_color=(0.15, 0.09, 0.05, 1.0),
        floor_roughness=0.22,
        floor_metallic=0.02,
        rug_main_color=(0.04, 0.16, 0.10, 1.0),
        rug_border_color=(0.14, 0.10, 0.05, 1.0),
        boiserie_color=(0.18, 0.10, 0.06, 1.0),
        wall_color=(0.07, 0.22, 0.14, 1.0),
        sconce_emission=(1.0, 0.90, 0.60, 1.0),
        sconce_strength=3.0,
        accent_color=(0.86, 0.72, 0.26, 1.0),
        drapery_color=(0.05, 0.18, 0.11, 1.0)
    )

    # B) Barão's Lounge: Obsidian walnut, purple imperial damask, midnight navy rug, dark marble floor
    build_room(
        room_id="barao_lounge",
        floor_color=(0.07, 0.06, 0.10, 1.0),
        floor_roughness=0.16,
        floor_metallic=0.12,
        rug_main_color=(0.06, 0.05, 0.14, 1.0),
        rug_border_color=(0.18, 0.14, 0.08, 1.0),
        boiserie_color=(0.11, 0.08, 0.15, 1.0),
        wall_color=(0.12, 0.08, 0.20, 1.0),
        sconce_emission=(0.98, 0.84, 0.52, 1.0),
        sconce_strength=3.4,
        accent_color=(0.90, 0.75, 0.28, 1.0),
        drapery_color=(0.10, 0.06, 0.16, 1.0)
    )

    # C) Dama's Salon: Rosewood, ruby crimson damask, royal burgundy rug, rose gold accents
    build_room(
        room_id="dama_salon",
        floor_color=(0.16, 0.05, 0.07, 1.0),
        floor_roughness=0.18,
        floor_metallic=0.08,
        rug_main_color=(0.20, 0.04, 0.08, 1.0),
        rug_border_color=(0.22, 0.12, 0.06, 1.0),
        boiserie_color=(0.19, 0.07, 0.09, 1.0),
        wall_color=(0.28, 0.06, 0.11, 1.0),
        sconce_emission=(1.0, 0.86, 0.66, 1.0),
        sconce_strength=3.1,
        accent_color=(0.90, 0.73, 0.30, 1.0),
        drapery_color=(0.24, 0.04, 0.08, 1.0)
    )

    # D) Cyber Casino: Dark polished granite, cyan and gold neon accents, carbon fiber rug
    build_room(
        room_id="cyber_casino",
        floor_color=(0.04, 0.05, 0.07, 1.0),
        floor_roughness=0.14,
        floor_metallic=0.25,
        rug_main_color=(0.05, 0.07, 0.10, 1.0),
        rug_border_color=(0.10, 0.25, 0.30, 1.0),
        boiserie_color=(0.06, 0.08, 0.11, 1.0),
        wall_color=(0.08, 0.11, 0.15, 1.0),
        sconce_emission=(0.18, 0.88, 0.98, 1.0),
        sconce_strength=4.0,
        accent_color=(0.92, 0.78, 0.28, 1.0),
        drapery_color=(0.05, 0.09, 0.14, 1.0)
    )

    print("\n[Blender] All 3D club rooms and luxury chairs generated successfully!")

if __name__ == "__main__":
    main()
