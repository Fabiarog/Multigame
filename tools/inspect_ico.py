import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath="assets/models/club/aki.glb")

ico = bpy.data.objects.get("Icosphere")
if ico:
    print("Found Icosphere!")
    print("Location:", ico.location)
    print("Scale:", ico.scale)
    print("Matrix World:", ico.matrix_world)
    verts = [ico.matrix_world @ v.co for v in ico.data.vertices]
    min_z = min(v.z for v in verts)
    max_z = max(v.z for v in verts)
    min_y = min(v.y for v in verts)
    max_y = max(v.y for v in verts)
    min_x = min(v.x for v in verts)
    max_x = max(v.x for v in verts)
    print(f"X: {min_x:.3f} to {max_x:.3f}, Y: {min_y:.3f} to {max_y:.3f}, Z: {min_z:.3f} to {max_z:.3f}")
    print("Materials on Icosphere:", [m.name if m else 'None' for m in ico.data.materials])
else:
    print("Icosphere NOT found!")
