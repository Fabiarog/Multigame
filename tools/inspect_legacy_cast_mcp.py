import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
original=bpy.context.window.scene
scene=bpy.data.scenes.new('Inspect26')
try:
    bpy.context.window.scene=scene
    bpy.ops.import_scene.gltf(filepath=str(root/'assets/models/club/onca.glb'))
    print(json.dumps([{'name':o.name,'type':o.type,'parent':o.parent.name if o.parent else None,'position':list(o.matrix_world.translation),'animations':[t.name for t in o.animation_data.nla_tracks] if o.animation_data else []} for o in scene.objects],ensure_ascii=False))
finally:
    bpy.context.window.scene=original
    for o in list(scene.objects): bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.scenes.remove(scene)
