"""Classic Club physical polish. Isolated scenes; immutable Git originals."""
import bpy,math,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art/blender/patch27';OUT.mkdir(parents=True,exist_ok=True)
original=bpy.context.window.scene;report=[]
def mat(name,color,rough=.6,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    n.inputs['Base Color'].default_value=(*color,1);n.inputs['Roughness'].default_value=rough;n.inputs['Metallic'].default_value=metal
    return m
def bevel(o,width,segments=3):
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    mod=o.modifiers.new('Crafted edge','BEVEL');mod.width=width;mod.segments=segments
    mod.limit_method='ANGLE';mod.angle_limit=.5
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in o.data.polygons:p.use_smooth=True
    mod=o.modifiers.new('Weighted highlights','WEIGHTED_NORMAL');mod.keep_sharp=True;mod.weight=40
    bpy.ops.object.modifier_apply(modifier=mod.name);o.select_set(False)
def cylinder(name,radius,depth,z,material,oval=.62):
    bpy.ops.mesh.primitive_cylinder_add(vertices=128,radius=radius,depth=depth,location=(0,0,z))
    o=bpy.context.object;o.name=name;o.scale.y=oval;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material);bevel(o,min(depth*.25,.035));return o
def ring(name,radius,tube,z,material,oval=.62):
    verts=[];faces=[];segments=160;sides=10
    for i in range(segments):
        a=i*math.tau/segments
        for j in range(sides):
            b=j*math.tau/sides;r=radius+tube*math.cos(b)
            verts.append((r*math.cos(a),r*oval*math.sin(a),z+tube*math.sin(b)))
    for i in range(segments):
        for j in range(sides):faces.append((i*sides+j,((i+1)%segments)*sides+j,((i+1)%segments)*sides+(j+1)%sides,i*sides+(j+1)%sides))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(o);mesh.materials.append(material)
    for p in mesh.polygons:p.use_smooth=True
    return o
def export(scene,ident):
    bpy.ops.object.select_all(action='DESELECT')
    for o in scene.objects:o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/f'{ident}.glb'),export_format='GLB',use_selection=True,use_active_scene=True,export_animations=False)
    report.append({'id':ident,'objects':len(scene.objects),'vertices':sum(len(o.data.vertices) for o in scene.objects if o.type=='MESH'),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in scene.objects if o.type=='MESH')})
def cleanup(scene):
    bpy.context.window.scene=original
    for o in list(scene.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.scenes.remove(scene)
try:
    for ident in ['room_classic_club','club_chair']:
        scene=bpy.data.scenes.new('Premium27_'+ident);bpy.context.window.scene=scene
        source=OUT/f'{ident}_original.glb'
        baseline=subprocess.check_output(['git','show',f'a1981dd:assets/models/club/{ident}.glb'],cwd=ROOT)
        if source.exists():assert source.read_bytes()==baseline
        else:source.write_bytes(baseline)
        bpy.ops.import_scene.gltf(filepath=str(source))
        meshes=[o for o in scene.objects if o.type=='MESH']
        for m in {m for o in meshes for m in o.data.materials if m}:
            if not m.use_nodes:continue
            n=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
            if not n:continue
            name=m.name.lower()
            if 'wall' in name:n.inputs['Base Color'].default_value=(.035,.09,.075,1);n.inputs['Roughness'].default_value=.88
            elif 'rugmain' in name:n.inputs['Base Color'].default_value=(.07,.055,.032,1)
            elif 'velvet' in name:n.inputs['Base Color'].default_value=(.075,.025,.019,1);n.inputs['Roughness'].default_value=.87;n.inputs['Metallic'].default_value=0
            elif 'book' in name:
                c=n.inputs['Base Color'].default_value[:];gray=sum(c[:3])/3
                n.inputs['Base Color'].default_value=(*(v*.48+gray*.18 for v in c[:3]),1)
            elif any(s in name for s in ('wood','boiserie','floor')):
                n.inputs['Roughness'].default_value=.46
                c=n.inputs['Base Color'].default_value[:];n.inputs['Base Color'].default_value=(c[0]*.65,c[1]*.6,c[2]*.57,1)
            elif 'trim' in name or 'brass' in name:n.inputs['Roughness'].default_value=.33;n.inputs['Metallic'].default_value=.9
            if 'sconce' in name:n.inputs['Emission Strength'].default_value=1.4
            if 'fireglow' in name:n.inputs['Emission Strength'].default_value=1.6
        if ident=='room_classic_club':
            for o in meshes:
                if len(o.data.polygons)<100 and any(s in o.name.lower() for s in ('bookcase','frame','mantel','baseboard','panel','shelf')):bevel(o,.012,2)
        bpy.data.libraries.write(str(OUT/f'{ident}.blend'),{scene},fake_user=True,compress=True)
        # Export static architecture in spatial groups; source retains editable props.
        if ident=='room_classic_club':
            buckets={}
            for o in meshes:buckets.setdefault(round(o.matrix_world.translation.x/5),[]).append(o)
            for key,objects in buckets.items():
                bpy.ops.object.select_all(action='DESELECT')
                for o in objects:o.select_set(True)
                bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();bpy.context.object.name=f'ClassicZone_{key}'
        export(scene,ident);cleanup(scene)
    scene=bpy.data.scenes.new('Premium27_Table');bpy.context.window.scene=scene
    wood=mat('TargetWalnut',(.065,.027,.013),.42);leather=mat('TargetLeather',(.043,.017,.012),.72);gold=mat('TargetBrass',(.62,.39,.13),.31,.92);felt=mat('TargetFelt',(.018,.10,.065),.94)
    cylinder('WalnutBase',4.75,.25,-.16,wood)
    cylinder('GreenFelt',4.43,.06,.039,felt)
    ring('PaddedLeatherRail',4.60,.13,-.025,leather)
    ring('BrassInnerInlay',4.445,.014,.025,gold)
    ring('BrassLowerLip',4.73,.018,-.16,gold)
    # A continuous fine seam reads at close range without dozens of draw calls.
    seam=mat('TargetStitch',(.24,.135,.06),.9)
    ring('LeatherSeam',4.61,.004,.106,seam)
    bpy.data.libraries.write(str(OUT/'club_table_premium.blend'),{scene},fake_user=True,compress=True)
    export(scene,'club_table_premium');cleanup(scene)
finally:bpy.context.window.scene=original
(OUT/'asset-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))
