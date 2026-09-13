import bpy,json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/blender/patch27';OUT.mkdir(parents=True,exist_ok=True)
original=bpy.context.window.scene;rows=[]
try:
    for ident in ['room_classic_club','room_barao_lounge','room_dama_salon','room_cyber_casino','club_chair','club_bar_cart']:
        scene=bpy.data.scenes.new('Audit27');bpy.context.window.scene=scene
        path=ROOT/'assets/models/club'/f'{ident}.glb';bpy.ops.import_scene.gltf(filepath=str(path))
        meshes=[o for o in scene.objects if o.type=='MESH'];mats={m for o in meshes for m in o.data.materials if m}
        rows.append({'id':ident,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'meshes':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),'polygons':sum(len(o.data.polygons) for o in meshes),'uv_layers':sorted(set(len(o.data.uv_layers) for o in meshes)),'materials':[{'name':m.name,'base':list(n.inputs['Base Color'].default_value),'roughness':n.inputs['Roughness'].default_value,'metallic':n.inputs['Metallic'].default_value,'textures':[x.image.size[:] for x in m.node_tree.nodes if x.type=='TEX_IMAGE' and x.image]} for m in mats if m.use_nodes for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'],'objects':[o.name for o in meshes]})
        bpy.context.window.scene=original
        for o in list(scene.objects):bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.scenes.remove(scene)
finally:bpy.context.window.scene=original
(OUT/'scene-audit.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
print(json.dumps(rows))
