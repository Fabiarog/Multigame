"""Blender Cycles comparison renders and sampled bounds (not extreme-pose proof)."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/blender/patch26'
original=bpy.context.window.scene
rows=[]
try:
    for ident in globals().get('CAST_IDS',['onca']):
        for version in (('original','refined') if ident in globals().get('COMPARE_IDS',('onca','corvo')) else ('refined',)):
            scene=bpy.data.scenes.new('Preview26');bpy.context.window.scene=scene
            bpy.ops.import_scene.gltf(filepath=str(OUT/(ident+'_'+version+'.glb')))
            actors=list(scene.objects)
            scene.render.engine='CYCLES';scene.cycles.samples=16
            scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
            world=bpy.data.worlds.new('PreviewWorld');world.use_nodes=True
            bg=world.node_tree.nodes.new('ShaderNodeBackground');bg.inputs[0].default_value=(.16,.18,.2,1);bg.inputs[1].default_value=.4
            output=world.node_tree.nodes.new('ShaderNodeOutputWorld');world.node_tree.links.new(bg.outputs[0],output.inputs[0]);scene.world=world
            cam=bpy.data.objects.new('ReviewCamera',bpy.data.cameras.new('ReviewCamera'));scene.collection.objects.link(cam);cam.location=(2.7,-5.2,2.5);cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.65;scene.camera=cam
            for name,loc,power,size in [('Key',(-3,-4,5),650,4),('Fill',(3,-2,3),250,3),('Rim',(0,3,4),750,3)]:
                light=bpy.data.lights.new(name,'AREA');light.energy=power;light.shape='DISK';light.size=size;o=bpy.data.objects.new(name,light);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
            clips=sorted({t.name for o in actors if o.animation_data for t in o.animation_data.nla_tracks})
            for clip in clips:
                duration=0
                for obj in actors:
                    if not obj.animation_data:continue
                    obj.animation_data.action=None
                    for track in obj.animation_data.nla_tracks:
                        track.mute=track.name!=clip
                        if not track.mute:duration=max(duration,max(s.frame_end for s in track.strips))
                for t in (0,.25,.5,.75,1):
                    scene.frame_set(round(duration*t));bpy.context.view_layer.update()
                    points=[]
                    for obj in actors:
                        if obj.type!='MESH':continue
                        evaluated=obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
                        points.extend(evaluated.matrix_world@Vector(c) for c in evaluated.bound_box)
                    extent=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
                    assert all(math.isfinite(v) and v<8 for v in extent),(ident,version,clip,t,extent)
                    center=[(max(p[i] for p in points)+min(p[i] for p in points))/2 for i in range(3)]
                    assert all(abs(v)<4 for v in center),(ident,version,clip,t,center)
                    rows.append({'id':ident,'version':version,'clip':clip,'phase':t,'extent':extent,'center':center})
                    if clip in ('idle','truco','victory') and t==.5:
                        scene.render.filepath=str(OUT/f'{ident}_{version}_{clip}.png');bpy.ops.render.render(write_still=True)
            bpy.context.window.scene=original
            for obj in list(scene.objects):bpy.data.objects.remove(obj,do_unlink=True)
            bpy.data.scenes.remove(scene)
finally:bpy.context.window.scene=original
report_name='stress-bounds.json' if len({r['id'] for r in rows})==11 else 'stress-bounds-subset.json'
(OUT/report_name).write_text(json.dumps(rows,indent=2),encoding='utf-8')
print('STRESS_BOUNDS_PASS',len(rows))
