import bpy,math,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/blender/patch27';original=bpy.context.window.scene
scene=bpy.data.scenes.new('Card27');bpy.context.window.scene=scene
try:
    verts=[];faces=[];radius=.026
    for z in (-.009,.009):
        for cx,cy,start in ((.29-radius,.41-radius,0),(-.29+radius,.41-radius,90),(-.29+radius,-.41+radius,180),(.29-radius,-.41+radius,270)):
            for i in range(8):
                a=math.radians(start+i*90/7);verts.append((cx+radius*math.cos(a),cy+radius*math.sin(a),z))
    faces.extend([tuple(reversed(range(32))),tuple(range(32,64))])
    for i in range(32):faces.append((i,(i+1)%32,(i+1)%32+32,i+32))
    mesh=bpy.data.meshes.new('RoundedCard');mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new('RoundedCard',mesh);scene.collection.objects.link(obj);obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bevel=obj.modifiers.new('Paper edge highlight','BEVEL');bevel.width=.002;bevel.segments=2;bpy.ops.object.modifier_apply(modifier=bevel.name)
    material=bpy.data.materials.new('IvoryPaper');material.use_nodes=True;n=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED');n.inputs['Base Color'].default_value=(.82,.76,.60,1);n.inputs['Roughness'].default_value=.8;obj.data.materials.append(material)
    bpy.ops.export_scene.gltf(filepath=str(OUT/'club_card_blank.glb'),export_format='GLB',use_selection=True,use_active_scene=True,export_animations=False)
    bpy.data.libraries.write(str(OUT/'club_card_blank.blend'),{scene},fake_user=True,compress=True)
finally:
    bpy.context.window.scene=original
    for o in list(scene.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.scenes.remove(scene)
for ident,scale,target in [('club_card_blank',1.15,(0,0,0)),('club_table_premium',11,(0,0,0))]:
    scene=bpy.data.scenes.new('Review27');bpy.context.window.scene=scene
    try:
        bpy.ops.import_scene.gltf(filepath=str(OUT/(ident+'.glb')))
        scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=800;scene.render.resolution_y=600;scene.render.resolution_percentage=100
        world=bpy.data.worlds.new('Studio');world.use_nodes=True;bg=world.node_tree.nodes.new('ShaderNodeBackground');bg.inputs[0].default_value=(.3,.32,.35,1);bg.inputs[1].default_value=.5;output=world.node_tree.nodes.new('ShaderNodeOutputWorld');world.node_tree.links.new(bg.outputs[0],output.inputs[0]);scene.world=world
        camera=bpy.data.objects.new('Camera',bpy.data.cameras.new('Camera'));scene.collection.objects.link(camera);camera.location=(scale*.4,-scale*.7,scale*.6);camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=scale;scene.camera=camera
        for name,pos,power in [('Key',(-.3,-.3,.8),80),('Rim',(.5,.4,.5),100)]:
            light=bpy.data.lights.new(name,'AREA');light.energy=power*scale*scale;light.shape='DISK';light.size=scale*.5;o=bpy.data.objects.new(name,light);scene.collection.objects.link(o);o.location=Vector(pos)*scale;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(OUT/(ident+'-preview.png'));bpy.ops.render.render(write_still=True)
    finally:
        bpy.context.window.scene=original
        for o in list(scene.objects):bpy.data.objects.remove(o,do_unlink=True)
        bpy.data.scenes.remove(scene)
print('CARD_AND_TABLE_PREVIEW_PASS')
