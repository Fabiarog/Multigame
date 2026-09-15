"""Preserve shipped meshes and working rigs; refine acting and authoring controls.
Run through blender_bridge.py. Candidate files never replace runtime files here.
"""
import bpy, math, json, subprocess, hashlib
from pathlib import Path
from mathutils import Vector, Quaternion
from mathutils.kdtree import KDTree

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'art/blender/patch26'
OUT.mkdir(parents=True,exist_ok=True)
BASELINE='10c4b4d18ece647bd52cc42e3f50cad658ec8f5c'
CAST=globals().get('CAST_IDS', ['onca'])
original=bpy.context.window.scene
report=[]

def repair_limb_fallback_weights(objects, ident):
    """Repair unmapped source twist/finger weights assigned rigidly to the torso.

    Limit transfer to distal arm regions in these two audited rest meshes;
    interpolate nearby existing limb weights, never fabricate new skin groups.
    """
    if ident not in ('morgana','carnical'):return 0
    changed=0
    for obj in objects:
        if obj.type!='MESH' or not obj.vertex_groups:continue
        weights=[{obj.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>1e-6} for v in obj.data.vertices]
        seeds=[i for i,w in enumerate(weights) if sum(v for n,v in w.items() if n.startswith(('Hand.','Forearm.','UpperArm.')))>.95]
        if not seeds:continue
        tree=KDTree(len(seeds))
        for i in seeds:tree.insert(obj.data.vertices[i].co,i)
        tree.balance()
        for vertex in obj.data.vertices:
            w=weights[vertex.index]
            if abs(vertex.co.x)<.25 or vertex.co.z<.70:continue
            if sum(v for n,v in w.items() if n in ('Root','Pelvis','Spine','Chest'))<.999:continue
            near=[(i,d) for co,i,d in tree.find_n(vertex.co,6) if d<.08]
            if not near:continue
            total=sum(1/max(d,.001)**2 for i,d in near);new={}
            for i,d in near:
                factor=(1/max(d,.001)**2)/total
                for name,value in weights[i].items():new[name]=new.get(name,0)+value*factor
            for group in list(vertex.groups):obj.vertex_groups[group.group].remove([vertex.index])
            for name,value in new.items():obj.vertex_groups[name].add([vertex.index],value,'REPLACE')
            changed+=1
    return changed

def curves(action):
    result=[]
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags: result.extend(bag.fcurves)
    return result

def owned_curves(action):
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                slot=next((s for s in action.slots if s.handle==bag.slot_handle),None)
                owner=slot.identifier if slot else ''
                for fc in bag.fcurves:yield fc,owner

def polish(objects, fps):
    changes=[]
    processed={}
    for obj in objects:
        if not obj.animation_data: continue
        for track in obj.animation_data.nla_tracks:
            for strip in track.strips:
                action=strip.action
                if action in processed:
                    end=processed[action]
                    strip.action_frame_start=0;strip.action_frame_end=end;strip.frame_start=0;strip.frame_end=end
                    strip.extrapolation='NOTHING'
                    continue
                old_start,old_end=action.frame_range
                old_span=old_end-old_start
                if old_span <= 0: continue
                clip=track.name
                # Keep card contact speed; give held reactions and idle more reading time.
                factor=1.0 if clip in ('play_card','shuffle','deal','cut_deck') else 1.12
                if clip=='idle': factor=max(factor,4.0*fps/old_span)
                end=round(old_span*factor)
                for fc,owner in owned_curves(action):
                    path=(owner+' '+fc.data_path).lower()
                    lead=.026 if ('head' in path or 'neck' in path) else -.018 if ('hand' in path or 'wing' in path or 'tail' in path) else 0
                    samples=[]
                    for frame in range(end+1):
                        t=frame/max(end,1)
                        # Monotonic time warp: head anticipates, extremities settle later.
                        source_t=max(0,min(1,t+lead*math.sin(math.pi*t)))
                        value=fc.evaluate(old_start+old_span*source_t)
                        settle_start=.85 if clip=='idle' else .88
                        if clip!='entrance' and t>settle_start:
                            a=(t-settle_start)/(1-settle_start); a=a*a*(3-2*a)
                            value=value*(1-a)+fc.evaluate(old_start)*a
                        samples.append(value)
                    fc.keyframe_points.clear()
                    fc.keyframe_points.add(len(samples))
                    for i,value in enumerate(samples):
                        key=fc.keyframe_points[i];key.co=(i,value);key.interpolation='LINEAR'
                    fc.update()
                # Small layered motion, with zero displacement at both clip ends.
                quaternion_groups={}
                for fc,owner in owned_curves(action):
                    if fc.data_path.endswith('rotation_quaternion'):
                        quaternion_groups.setdefault((owner,fc.data_path),{})[fc.array_index]=fc
                for (owner,path),channels in quaternion_groups.items():
                    if len(channels)!=4:continue
                    label=(owner+' '+path).lower()
                    head='head' in label
                    chest='chest' in label or 'body' in label
                    secondary=any(s in label for s in ('wing','tail','ear'))
                    if clip!='idle' or not (head or chest or secondary):continue
                    axis=(0,0,1) if head else (1,0,0)
                    amplitude=.028 if head else .012 if chest else .018
                    for frame in range(end+1):
                        t=frame/max(1,end)
                        angle=amplitude*math.sin(2*math.pi*t)*math.sin(math.pi*t)**2
                        q=Quaternion([channels[i].keyframe_points[frame].co.y for i in range(4)]).normalized()
                        q=q@Quaternion(axis,angle)
                        for i in range(4):channels[i].keyframe_points[frame].co.y=q[i]
                    for fc in channels.values():fc.update()
                strip.action_frame_start=0;strip.action_frame_end=end
                strip.frame_start=0;strip.frame_end=end
                strip.extrapolation='NOTHING'
                processed[action]=end
                changes.append({'object':obj.name,'clip':clip,'before_seconds':round(old_span/fps,3),'after_seconds':round(end/fps,3)})
    return changes


def add_authoring_ik(rig, scene):
    """Non-deforming controls; current FK clips remain the runtime authority."""
    controls=[]
    scene.frame_set(0);bpy.context.view_layer.update()
    for side in ('L','R'):
        for label,upper,middle,end in [('Arm','UpperArm.','Forearm.','Hand.'),('Leg','Thigh.','Shin.','Foot.')]:
            names=[n+side for n in (upper,middle,end)]
            if not all(n in rig.pose.bones for n in names):continue
            a,b,c=[rig.pose.bones[n].head.copy() for n in names]
            axis=c-a
            projected=a+axis*((b-a).dot(axis)/max(axis.length_squared,1e-8))
            outward=b-projected
            if outward.length<.001:outward=Vector((0,-1,0))
            pole=b+outward.normalized()*.4
            controls.append((label,side,names[1],c,pole))
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    for label,side,middle,target,pole in controls:
        for suffix,position in [('IK',target),('Pole',pole)]:
            bone=rig.data.edit_bones.new(f'CTRL_{label}{suffix}.{side}')
            bone.head=position;bone.tail=position+Vector((0,0,.10));bone.use_deform=False
    bpy.ops.object.mode_set(mode='OBJECT')
    for label,side,middle,target,pole in controls:
        constraint=rig.pose.bones[middle].constraints.new('IK')
        constraint.name=f'Authoring {label} IK — FK clips preserved'
        constraint.target=rig;constraint.subtarget=f'CTRL_{label}IK.{side}'
        constraint.pole_target=rig;constraint.pole_subtarget=f'CTRL_{label}Pole.{side}'
        constraint.chain_count=2;constraint.influence=0
    rig['authoring_notes']='IK controls are non-deforming and default to zero influence. Match targets to the intended pose before animating; bake any new IK animation before exporting. Existing FK clips preserved.'
    return len(controls)*2

try:
    for ident in CAST:
        source=OUT/(ident+'_original.glb')
        baseline=subprocess.check_output(['git','show',f'{BASELINE}:assets/models/club/{ident}.glb'],cwd=ROOT)
        if source.exists():
            assert source.read_bytes()==baseline, f'{source} differs from the pinned baseline; inspect before proceeding'
        else:source.write_bytes(baseline)
        scene=bpy.data.scenes.new('Refined26_'+ident);bpy.context.window.scene=scene
        scene.render.fps=30
        bpy.ops.import_scene.gltf(filepath=str(source))
        objects=list(scene.objects)
        repaired_weights=repair_limb_fallback_weights(objects,ident)
        # Compact the legacy neck gap without adding any geometry or changing faces.
        if ident in ('nina','bento','onca'):
            head=next(o for o in objects if o.type=='EMPTY' and o.name.split('.')[0]=='Head')
            head.location.z-=.035
            if head.animation_data:
                for track in head.animation_data.nla_tracks:
                    for strip in track.strips:
                        for fc,owner in owned_curves(strip.action):
                            if 'head' in owner.lower() and fc.data_path=='location' and fc.array_index==2:
                                for key in fc.keyframe_points:key.co.y-=.035;key.handle_left.y-=.035;key.handle_right.y-=.035
        changes=polish(objects,scene.render.fps)
        rigs=[o for o in objects if o.type=='ARMATURE']
        export_objects=objects
        authoring_controls=sum(add_authoring_ik(rig,scene) for rig in rigs)
        for o in export_objects:
            if o.type!='MESH':continue
            for mat in o.data.materials:
                if not mat or not mat.use_nodes:continue
                for node in mat.node_tree.nodes:
                    if node.type!='BSDF_PRINCIPLED':continue
                    if not node.inputs['Roughness'].is_linked:
                        name=mat.name.lower()
                        if '_coat' in name:node.inputs['Roughness'].default_value=.78
                        elif '_satin' in name:node.inputs['Roughness'].default_value=.42
        scene.frame_set(0)
        bpy.ops.object.select_all(action='DESELECT')
        for o in export_objects:o.select_set(True)
        bpy.context.view_layer.objects.active=export_objects[0]
        destination=OUT/(ident+'_refined.glb')
        bpy.ops.export_scene.gltf(filepath=str(destination),export_format='GLB',use_selection=True,use_active_scene=True,
            export_animations=True,export_animation_mode='NLA_TRACKS',export_force_sampling=True,export_frame_range=False,export_def_bones=True)
        bpy.data.libraries.write(str(OUT/(ident+'_refined.blend')),{scene},fake_user=True,compress=True)
        report.append({'id':ident,'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'candidate_sha256':hashlib.sha256(destination.read_bytes()).hexdigest(),
            'vertices':sum(len(o.data.vertices) for o in export_objects if o.type=='MESH'),'repaired_weight_vertices':repaired_weights,'authoring_controls':authoring_controls,'rigs':[{'name':o.name,'bones':len(o.data.bones)} for o in export_objects if o.type=='ARMATURE'],'changes':changes})
        bpy.context.window.scene=original
        for o in list(scene.objects):bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.scenes.remove(scene)
finally:bpy.context.window.scene=original
previous=json.loads((OUT/'refinement-report.json').read_text(encoding='utf-8')) if (OUT/'refinement-report.json').exists() else []
merged={r['id']:r for r in previous}
merged.update({r['id']:r for r in report})
(OUT/'refinement-report.json').write_text(json.dumps(list(merged.values()),indent=2),encoding='utf-8')
print(json.dumps([{'id':r['id'],'vertices':r['vertices'],'rigs':r['rigs'],'updated_tracks':len(r['changes'])} for r in report]))
