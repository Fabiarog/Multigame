import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath="assets/models/club/aki.glb")

body = bpy.data.objects.get("Body")
mesh = body.data

# Polygons of Mat 0 with Z > 1.4
high_polys = [p for p in mesh.polygons if p.material_index == 0 and p.center.z > 1.4]
print(f"Mat 0 (aki_skin) polygons above Z=1.4: {len(high_polys)}")
if high_polys:
    min_y = min(p.center.y for p in high_polys)
    max_y = max(p.center.y for p in high_polys)
    min_z = min(p.center.z for p in high_polys)
    max_z = max(p.center.z for p in high_polys)
    print(f"  Head part of aki_skin: Y[{min_y:.3f}, {max_y:.3f}], Z[{min_z:.3f}, {max_z:.3f}]")

# Compare with Face_00_SKIN (Mat 9)
face_polys = [p for p in mesh.polygons if p.material_index == 9]
print(f"Mat 9 (Face_00_SKIN) polygons: {len(face_polys)}")
if face_polys:
    min_y = min(p.center.y for p in face_polys)
    max_y = max(p.center.y for p in face_polys)
    min_z = min(p.center.z for p in face_polys)
    max_z = max(p.center.z for p in face_polys)
    print(f"  Face_00_SKIN: Y[{min_y:.3f}, {max_y:.3f}], Z[{min_z:.3f}, {max_z:.3f}]")
