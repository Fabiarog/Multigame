import bpy
import math
import os

# Clean existing objects
bpy.ops.wm.read_factory_settings(use_empty=True)

# Ensure output directory exists
out_dir = os.path.abspath("assets/models/club")
os.makedirs(out_dir, exist_ok=True)
out_file = os.path.join(out_dir, "club_chair.glb")

# --- MATERIALS ---
def create_material(name, base_color, metallic=0.0, roughness=0.5, clearcoat=0.0):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = base_color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if "Clearcoat" in bsdf.inputs:
            bsdf.inputs["Clearcoat"].default_value = clearcoat
        elif "Coat Weight" in bsdf.inputs:
            bsdf.inputs["Coat Weight"].default_value = clearcoat
    return mat

# Velvet / Upholstery: Deep luxurious emerald/bottle green with warm sheen
mat_upholstery = create_material("ClubVelvet", (0.08, 0.24, 0.17, 1.0), metallic=0.05, roughness=0.55)
# Polished Walnut Wood: Rich dark brown with warm undertone
mat_wood = create_material("WalnutWood", (0.16, 0.09, 0.05, 1.0), metallic=0.0, roughness=0.35, clearcoat=0.3)
# Brass Studs / Trim: Vintage gold brass
mat_brass = create_material("BrassTrim", (0.83, 0.69, 0.22, 1.0), metallic=0.85, roughness=0.28)

# --- 1. SEAT CUSHION ---
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.46))
cushion = bpy.context.active_object
cushion.name = "SeatCushion"
cushion.scale = (0.78, 0.72, 0.16)
cushion.data.materials.append(mat_upholstery)
bpy.ops.object.transform_apply(scale=True)

# Add bevel modifier for soft leather/cushion edges
bev = cushion.modifiers.new("Bevel", "BEVEL")
bev.width = 0.045
bev.segments = 4

# Seat base frame (wood border under cushion)
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.36))
base_frame = bpy.context.active_object
base_frame.name = "SeatBaseFrame"
base_frame.scale = (0.80, 0.74, 0.08)
base_frame.data.materials.append(mat_wood)
bpy.ops.object.transform_apply(scale=True)
bev_frame = base_frame.modifiers.new("Bevel", "BEVEL")
bev_frame.width = 0.02
bev_frame.segments = 2

# --- 2. FOUR WOODEN LEGS ---
leg_offsets = [
    (-0.33, -0.29),  # front left
    (0.33, -0.29),   # front right
    (-0.31, 0.29),   # back left
    (0.31, 0.29),    # back right
]

for i, (lx, ly) in enumerate(leg_offsets):
    bpy.ops.mesh.primitive_cylinder_add(
        radius=0.034, depth=0.36, vertices=16,
        location=(lx, ly, 0.18)
    )
    leg = bpy.context.active_object
    leg.name = f"Leg_{i}"
    splay_x = -0.05 if lx < 0 else 0.05
    splay_y = -0.05 if ly < 0 else 0.05
    leg.rotation_euler = (splay_y, splay_x, 0)
    leg.data.materials.append(mat_wood)
    
    # Brass cap on each foot
    bpy.ops.mesh.primitive_cylinder_add(
        radius=0.036, depth=0.04, vertices=16,
        location=(lx + splay_x * 0.18, ly + splay_y * 0.18, 0.02)
    )
    foot = bpy.context.active_object
    foot.name = f"FootCap_{i}"
    foot.data.materials.append(mat_brass)

# --- 3. CURVED BACKREST ---
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.34, 0.84))
back = bpy.context.active_object
back.name = "Backrest"
back.scale = (0.76, 0.14, 0.62)
back.data.materials.append(mat_upholstery)
bpy.ops.object.transform_apply(scale=True)
bev_back = back.modifiers.new("Bevel", "BEVEL")
bev_back.width = 0.05
bev_back.segments = 4

# Backrest wooden rear support shell
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.39, 0.84))
back_shell = bpy.context.active_object
back_shell.name = "BackrestShell"
back_shell.scale = (0.78, 0.04, 0.64)
back_shell.data.materials.append(mat_wood)
bpy.ops.object.transform_apply(scale=True)

# --- 4. ARMRESTS ---
for side, sign in [("Left", -1), ("Right", 1)]:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sign * 0.40, 0.05, 0.68))
    arm = bpy.context.active_object
    arm.name = f"Armrest_{side}"
    arm.scale = (0.09, 0.56, 0.07)
    arm.data.materials.append(mat_upholstery)
    bpy.ops.object.transform_apply(scale=True)
    bev_arm = arm.modifiers.new("Bevel", "BEVEL")
    bev_arm.width = 0.025
    bev_arm.segments = 3

    bpy.ops.mesh.primitive_cylinder_add(
        radius=0.025, depth=0.28, vertices=16,
        location=(sign * 0.39, -0.16, 0.50)
    )
    post = bpy.context.active_object
    post.name = f"ArmPost_{side}"
    post.data.materials.append(mat_wood)

# --- 5. DECORATIVE BRASS RIVETS ---
for j in range(7):
    rx = -0.30 + j * 0.10
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=0.016, segments=10, ring_count=8,
        location=(rx, 0.31, 1.12)
    )
    stud = bpy.context.active_object
    stud.name = f"BrassStud_Top_{j}"
    stud.data.materials.append(mat_brass)

for j in range(6):
    rx = -0.30 + j * 0.12
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=0.014, segments=10, ring_count=8,
        location=(rx, -0.34, 0.41)
    )
    stud = bpy.context.active_object
    stud.name = f"BrassStud_Front_{j}"
    stud.data.materials.append(mat_brass)

# Apply all modifiers
bpy.ops.object.select_all(action='SELECT')
for obj in bpy.context.selected_objects:
    if obj.type == 'MESH':
        bpy.context.view_layer.objects.active = obj
        for mod in obj.modifiers:
            bpy.ops.object.modifier_apply(modifier=mod.name)

# Export to GLB
print(f"[Blender] Exporting club chair to {out_file}...")
bpy.ops.export_scene.gltf(
    filepath=out_file,
    export_format='GLB',
    use_selection=False,
    export_apply=True
)
print(f"[Blender] Successfully exported club chair GLB! Size: {os.path.getsize(out_file)} bytes")
