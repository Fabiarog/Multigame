import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

body = bpy.data.objects.get("Body")
print("=== Aki.fbx Body Materials ===")
for i, m in enumerate(body.data.materials):
    print(f"Mat {i}: {m.name if m else 'None'}")
    polys = [p for p in body.data.polygons if p.material_index == i]
    print(f"  polygons: {len(polys)}")
    if polys:
        z_min = min(p.center.z for p in polys)
        z_max = max(p.center.z for p in polys)
        print(f"  Z range: {z_min:.3f} to {z_max:.3f}")
