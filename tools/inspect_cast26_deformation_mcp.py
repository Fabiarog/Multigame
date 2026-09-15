"""Report edges that elongate substantially in an exported victory pose."""
import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/blender/patch26'
original=bpy.context.window.scene;report=[]
version=globals().get('VERSION','original')
try:
    for ident in ('morgana','carnical'):
        scene=bpy.data.scenes.new('Deformation26');bpy.context.window.scene=scene
        bpy.ops.import_scene.gltf(filepath=str(OUT/(ident+'_'+version+'.glb')))
        actors=list(scene.objects);duration=0
        for o in actors:
            if not o.animation_data:continue
            o.animation_data.action=None
            for t in o.animation_data.nla_tracks:
                t.mute=t.name!='victory'
                if not t.mute:duration=max(duration,max(s.frame_end for s in t.strips))
        scene.frame_set(round(duration*.5));bpy.context.view_layer.update()
        for obj in actors:
            if obj.type!='MESH':continue
            evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
            suspect=[]
            for edge in obj.data.edges:
                a,b=edge.vertices;rest=(obj.data.vertices[a].co-obj.data.vertices[b].co).length
                posed=(mesh.vertices[a].co-mesh.vertices[b].co).length
                if rest>.0001 and posed>rest*5 and posed>.08:
                    suspect.append({'ratio':posed/rest,'length':posed,'vertices':[{'index':i,'position':list(obj.data.vertices[i].co),'weights':{obj.vertex_groups[g.group].name:g.weight for g in obj.data.vertices[i].groups}} for i in (a,b)]})
            suspect.sort(key=lambda x:x['ratio'],reverse=True)
            report.append({'id':ident,'mesh':obj.name,'suspect_edges':len(suspect),'worst':suspect[:20]})
            evaluated.to_mesh_clear()
        bpy.context.window.scene=original
        for obj in list(scene.objects):bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.scenes.remove(scene)
finally:bpy.context.window.scene=original
(OUT/('deformation-'+version+'.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
if version=='refined':
    counts={r['id']:r['suspect_edges'] for r in report}
    assert counts['carnical']==0,counts
    # Morgana's shoulder transition remains imperfect; guard the measured gain.
    assert counts['morgana']<=74,counts
    print('DEFORMATION_REGRESSION_PASS',counts)
print(json.dumps(report))
