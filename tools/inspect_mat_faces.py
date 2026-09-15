import bpy
from mathutils import Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath="assets/models/club/aki.glb")

body = bpy.data.objects.get("Body")
mesh = body.data

# Check faces by material
for i, m in enumerate(mesh.materials):
    m_faces = [poly for poly in mesh.polygons if poly.material_index == i]
    if m_faces:
        center_z = sum(poly.center.z for poly in m_faces) / len(m_faces)
        center_y = sum(poly.center.y for poly in m_faces) / len(m_faces)
        center_x = sum(poly.center.x for poly in m_faces) / len(m_faces)
        normal_y = sum(poly.normal.y for poly in m_faces) / len(m_faces)
        print(f"Mat {i} ({m.name}): {len(m_faces)} polygons, center=({center_x:.3f}, {center_y:.3f}, {center_z:.3f}), avg normal Y={normal_y:.3f}")
