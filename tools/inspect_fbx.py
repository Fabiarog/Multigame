import bpy

files = [
    "assets/modelos 3d detalhados/ghoul_ue5.fbx",
    "assets/modelos 3d detalhados/aki_fbx/Aki.fbx"
]

for path in files:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    print("=== FILE:", path, "===")
    for o in bpy.data.objects:
        p_name = o.parent.name if o.parent else None
        print(f"  {o.name} ({o.type}), parent: {p_name}, loc: {o.location}, rot: {o.rotation_euler}")
        if o.type == "ARMATURE":
            print(f"    Bones ({len(o.data.bones)}): {[b.name for b in o.data.bones[:15]]}...")
        if o.type == "MESH":
            print(f"    Mesh verts: {len(o.data.vertices)}, materials: {[m.name for m in o.data.materials if m]}")
            arm_mods = [m for m in o.modifiers if m.type == 'ARMATURE']
            print(f"    Armature modifiers: {[m.name for m in arm_mods]}, VGroups: {len(o.vertex_groups)}")
