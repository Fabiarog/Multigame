import bpy
import os

orig_scene = bpy.context.window.scene
temp_scene = bpy.data.scenes.new("InspectDimensions")
bpy.context.window.scene = temp_scene

try:
    for name in ["corvo.glb", "zeca.glb"]:
        p = os.path.abspath(os.path.join("assets", "models", "club", name))
        for obj in list(temp_scene.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.ops.import_scene.gltf(filepath=p)
        mesh = next(o for o in temp_scene.objects if o.type == 'MESH')
        arm = next((o for o in temp_scene.objects if o.type == 'ARMATURE'), None)
        print(f"=== {name} ===")
        print(f"  Mesh bbox dims: {mesh.dimensions[:]}")
        print(f"  Mesh world loc: {mesh.matrix_world.translation[:]}")
        if arm:
            print(f"  Armature scale: {arm.scale[:]}")
            for bname in ["Root", "Pelvis", "Chest", "Head"]:
                b = arm.data.bones.get(bname)
                if b:
                    print(f"    Bone {bname}: head={b.head_local[:]}")
finally:
    for obj in list(temp_scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.context.window.scene = orig_scene
    bpy.data.scenes.remove(temp_scene)
