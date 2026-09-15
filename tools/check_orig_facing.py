import bpy

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath="assets/modelos 3d detalhados/aki_fbx/Aki.fbx")

face = bpy.data.objects.get("Face")
# Check normal of eyes or face
iris_mats = [i for i, m in enumerate(face.data.materials) if "eyeiris" in m.name.lower()]
if iris_mats:
    idx = iris_mats[0]
    iris_polys = [p for p in face.data.polygons if p.material_index == idx]
    avg_normal = sum((p.normal for p in iris_polys), bpy.data.objects['Body'].location) / len(iris_polys)
    print(f"Original Eye Iris normal in Aki.fbx: {avg_normal}")
    if avg_normal.y < 0:
        print("-> Aki was ALREADY facing -Y (front) in Aki.fbx!")
    else:
        print("-> Aki was facing +Y (back) in Aki.fbx!")
