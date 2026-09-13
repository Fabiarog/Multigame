"""Inspect the shipped cast in isolated Blender scenes through the local MCP."""
import bpy, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'art/blender/patch26'
OUT.mkdir(parents=True, exist_ok=True)
original = bpy.context.window.scene
report = []
try:
    for ident in ('nina','bento','corvo','onca','iara','zeca','aki','barao','dama','morgana','carnical'):
        scene = bpy.data.scenes.new('Audit26_' + ident)
        bpy.context.window.scene = scene
        bpy.ops.import_scene.gltf(filepath=str(ROOT / 'assets/models/club' / (ident + '.glb')))
        meshes = [o for o in scene.objects if o.type == 'MESH']
        rigs = [o for o in scene.objects if o.type == 'ARMATURE']
        animations = {}
        for o in scene.objects:
            if not o.animation_data: continue
            for track in o.animation_data.nla_tracks:
                for strip in track.strips:
                    animations[track.name] = round((strip.frame_end-strip.frame_start)/scene.render.fps,3)
        report.append({'id':ident, 'vertices':sum(len(o.data.vertices) for o in meshes),
            'polygons':sum(len(o.data.polygons) for o in meshes), 'mesh_count':len(meshes),
            'materials':sorted({m.name for o in meshes for m in o.data.materials if m}),
            'rigs':[{'name':o.name,'bones':[b.name for b in o.data.bones]} for o in rigs],
            'unweighted':sum(1 for o in meshes if o.vertex_groups for v in o.data.vertices if not v.groups),
            'animations':animations,
            'mesh_details':[{'name':o.name,'vertices':len(o.data.vertices),'polygons':len(o.data.polygons),'groups':len(o.vertex_groups)} for o in meshes]})
        bpy.context.window.scene = original
        for o in list(scene.objects): bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.scenes.remove(scene)
finally:
    bpy.context.window.scene = original
(OUT / 'cast-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps([{'id':r['id'],'vertices':r['vertices'],'bones':sum(len(a['bones']) for a in r['rigs']),'clips':len(r['animations']),'unweighted':r['unweighted']} for r in report]))
