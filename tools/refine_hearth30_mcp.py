"""Refine existing fireplace materials/edges; export UV2-ready static room."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/blender/patch30';OUT.mkdir(parents=True,exist_ok=True)
original=bpy.context.window.scene;selected=list(bpy.context.selected_objects);active=bpy.context.view_layer.objects.active
for existing in list(bpy.data.scenes):
    bpy.context.window.scene=existing;bpy.ops.object.select_all(action='DESELECT')
with bpy.data.libraries.load(str(ROOT/'art/blender/patch27/room_classic_club.blend')) as (data,loaded):loaded.scenes=data.scenes
scene=loaded.scenes[0];bpy.context.window.scene=scene
report={'source':'art/blender/patch27/room_classic_club.blend','changes':[]}
def material(name,base,rough,emission=None):
    m=bpy.data.materials.new(name);m.use_nodes=True;n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    n.inputs['Base Color'].default_value=(*base,1);n.inputs['Roughness'].default_value=rough
    if emission:n.inputs['Emission Color'].default_value=(*emission,1);n.inputs['Emission Strength'].default_value=.7
    return m
try:
    coal=material('Hearth30CharredWood',(.022,.012,.008),.94)
    ember=material('Hearth30EmberEnds',(.16,.028,.004),.88,(1,.055,.003))
    cavity=next(o for o in scene.objects if o.name.startswith('FireplaceCavity'))
    target=sum((cavity.matrix_world@Vector(c) for c in cavity.bound_box),Vector())/8
    # Imported cavity was a solid slab in front of the logs, not a recess.
    back=max(v.co.y for v in cavity.data.vertices)
    front=min(v.co.y for v in cavity.data.vertices)
    wall=next(o for o in scene.objects if o.name.startswith('BackWall_Classic'))
    panel_back=min(v.co.y for v in wall.data.vertices)-.005
    for v in cavity.data.vertices:
        v.co.y=panel_back-.06 if v.co.y<(front+back)*.5 else panel_back
        v.co.x=1.41 if v.co.x>0 else -1.41
        v.co.z=2.31 if v.co.z>0 else -.48
    cavity.data.update()
    report['cavity_depth_before']=back-front;report['cavity_depth_after']=.06
    for o in list(scene.objects):
        if o.type!='MESH':continue
        if o.name.startswith('FireLog_'):
            for vertex in o.data.vertices:vertex.co.y-=.30
            o.data.update()
            o.data.materials.clear();o.data.materials.append(coal);o.data.materials.append(ember)
            caps=0
            for face in o.data.polygons:
                face.material_index=1 if abs(face.normal.x)>.85 else 0
                caps+=face.material_index
            assert caps>0 and caps<len(o.data.polygons),'Log must retain both wood and ember faces'
            report['changes'].append({'object':o.name,'ember_faces':caps,'total_faces':len(o.data.polygons)})
        elif o.name.startswith(('FireplaceCol','FireplaceHearth','FireplaceMantel')):
            bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
            previous=len(o.data.vertices)
            bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bm.to_mesh(o.data);bm.free()
            mod=o.modifiers.new('Crafted hearth edges','BEVEL');mod.width=.035;mod.segments=3;mod.limit_method='ANGLE'
            bpy.ops.object.modifier_apply(modifier=mod.name)
            assert len(o.data.vertices)>previous,'Bevel must add real edge geometry'
            report['changes'].append({'object':o.name,'vertices_before':previous,'vertices_after':len(o.data.vertices)})
    bpy.data.libraries.write(str(OUT/'classic_club_hearth_author.blend'),{scene},fake_user=True,compress=True)
    buckets={}
    for o in list(scene.objects):
        if o.type=='MESH':buckets.setdefault(round(o.matrix_world.translation.x/5),[]).append(o)
    for key,objects in buckets.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects:o.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=f'ClassicZone_{key}'
        o.data.uv_layers.new(name='LightmapUV');o.data.uv_layers.active_index=1
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.02)
        bpy.ops.object.mode_set(mode='OBJECT');o.data.uv_layers.active_index=0
    meshes=[o for o in scene.objects if o.type=='MESH']
    report['meshes']=len(meshes);report['vertices']=sum(len(o.data.vertices) for o in meshes)
    report['triangles']=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
    report['uv_layers']={o.name:len(o.data.uv_layers) for o in meshes}
    report['bounds']={o.name:[[min((o.matrix_world@Vector(c))[i] for c in o.bound_box) for i in range(3)],[max((o.matrix_world@Vector(c))[i] for c in o.bound_box) for i in range(3)]] for o in meshes}
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=str(OUT/'room_classic_club_premium.glb'),export_format='GLB',use_selection=True,use_active_scene=True,export_animations=False,export_texcoords=True)
    bpy.data.libraries.write(str(OUT/'classic_club_hearth_uv2.blend'),{scene},fake_user=True,compress=True)
    scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=1000;scene.render.resolution_y=750;scene.render.resolution_percentage=100
    cam=bpy.data.objects.new('ReviewCamera',bpy.data.cameras.new('ReviewCamera'));scene.collection.objects.link(cam)
    cam.location=target+Vector((3,-6,2));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=6;scene.camera=cam
    light=bpy.data.lights.new('ReviewArea','AREA');light.energy=500;light.size=4
    obj=bpy.data.objects.new('ReviewArea',light);scene.collection.objects.link(obj);obj.location=target+Vector((0,-3,4));obj.rotation_euler=(target-obj.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(OUT/'hearth-review.png');bpy.ops.render.render(write_still=True)
    (OUT/'report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
finally:
    if bpy.context.object and bpy.context.object.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.window.scene=original
    for o in list(scene.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.scenes.remove(scene)
    for o in selected:o.select_set(True)
    bpy.context.view_layer.objects.active=active
