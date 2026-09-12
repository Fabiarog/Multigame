import bpy
import math
import os

bpy.ops.wm.read_factory_settings(use_empty=True)

out_dir = os.path.abspath("assets/models/club")
room_path = os.path.join(out_dir, "room_classic_club.glb")
chair_path = os.path.join(out_dir, "club_chair.glb")
corvo_path = os.path.join(out_dir, "corvo.glb")
preview_png = r"C:\Users\Lucas\.gemini\antigravity-ide\brain\eabdfecc-ffb3-4c81-afb0-7677193c59e8\table_room_preview.png"

# 1. Import Room
bpy.ops.import_scene.gltf(filepath=room_path)

# 2. Add Table
# Walnut Rim
bpy.ops.mesh.primitive_cylinder_add(radius=4.7, depth=0.26, vertices=48, location=(0, 0, -0.15))
rim = bpy.context.active_object
rim.scale = (1.0, 0.62, 1.0)
rim.rotation_euler = (1.57, 0, 0)
bpy.ops.object.transform_apply(scale=True, rotation=True)
mat_rim = bpy.data.materials.new("RimMat")
mat_rim.use_nodes = True
mat_rim.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (0.18, 0.09, 0.06, 1.0)
mat_rim.node_tree.nodes.get("Principled BSDF").inputs["Roughness"].default_value = 0.4
rim.data.materials.append(mat_rim)

# Green Felt
bpy.ops.mesh.primitive_cylinder_add(radius=4.45, depth=0.08, vertices=48, location=(0, 0, 0.04))
felt = bpy.context.active_object
felt.scale = (1.0, 0.62, 1.0)
felt.rotation_euler = (1.57, 0, 0)
bpy.ops.object.transform_apply(scale=True, rotation=True)
mat_felt = bpy.data.materials.new("FeltMat")
mat_felt.use_nodes = True
mat_felt.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (0.08, 0.33, 0.23, 1.0)
mat_felt.node_tree.nodes.get("Principled BSDF").inputs["Roughness"].default_value = 0.8
felt.data.materials.append(mat_felt)

# Brass Inlay
bpy.ops.mesh.primitive_cylinder_add(radius=4.57, depth=0.06, vertices=48, location=(0, 0, 0.0))
inlay = bpy.context.active_object
inlay.scale = (1.0, 0.62, 1.0)
inlay.rotation_euler = (1.57, 0, 0)
bpy.ops.object.transform_apply(scale=True, rotation=True)
mat_inlay = bpy.data.materials.new("InlayMat")
mat_inlay.use_nodes = True
mat_inlay.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (0.88, 0.70, 0.25, 1.0)
mat_inlay.node_tree.nodes.get("Principled BSDF").inputs["Metallic"].default_value = 0.85
mat_inlay.node_tree.nodes.get("Principled BSDF").inputs["Roughness"].default_value = 0.25
inlay.data.materials.append(mat_inlay)

# 3. Add 4 Chairs at the exact new outer positions
chair_positions = [
    ( -1.8,  3.10 + 0.72, -0.72,  3.14 ),  # South (Você)
    (  5.20 + 0.72, 0.0,  -0.72, -1.57 ),  # East (Adv 1)
    (  0.0, -3.30 - 0.72, -0.72,  0.00 ),  # North (Aliado)
    ( -5.20 - 0.72, 0.0,  -0.72,  1.57 ),  # West (Adv 2)
]

for cx, cy_world, cz_world, rot in chair_positions:
    bpy.ops.import_scene.gltf(filepath=chair_path)
    imported = bpy.context.selected_objects
    for obj in imported:
        # Translate to world position (Blender Y is Godot Z)
        obj.location.x += cx
        obj.location.y += cy_world
        obj.location.z += cz_world
        obj.scale = (1.28, 1.28, 1.28)
        obj.rotation_euler.z += rot

# 4. Lighting
# Pendant Chandelier
light_data = bpy.data.lights.new(name="Chandelier", type='POINT')
light_data.energy = 800.0
light_data.color = (1.0, 0.94, 0.82)
light_obj = bpy.data.objects.new("Chandelier", light_data)
bpy.context.scene.collection.objects.link(light_obj)
light_obj.location = (0, 0.1, 3.2)

# Sun / Key Light
sun_data = bpy.data.lights.new(name="SunKey", type='SUN')
sun_data.energy = 2.8
sun_data.color = (1.0, 0.95, 0.88)
sun_obj = bpy.data.objects.new("SunKey", sun_data)
bpy.context.scene.collection.objects.link(sun_obj)
sun_obj.rotation_euler = (0.78, -0.35, 0.4)

# 5. Camera (Orthographic matching TableStage)
cam_data = bpy.data.cameras.new("StageCam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 13.5
cam_obj = bpy.data.objects.new("StageCam", cam_data)
bpy.context.scene.collection.objects.link(cam_obj)
bpy.context.scene.camera = cam_obj
# Position camera in front and looking down at table
cam_obj.location = (0, -11.0, 6.2)
cam_obj.rotation_euler = (1.05, 0, 0)

# Render
bpy.context.scene.render.engine = 'BLENDER_EEVEE'
bpy.context.scene.render.resolution_x = 1080
bpy.context.scene.render.resolution_y = 608
bpy.context.scene.render.filepath = preview_png

print(f"[Render] Rendering table in room to {preview_png}...")
bpy.ops.render.render(write_still=True)
print("[Render] Done!")
