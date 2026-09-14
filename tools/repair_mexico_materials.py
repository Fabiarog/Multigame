"""Run inside Blender MCP. Repair exported PBR values using the authored palette, without rebuilding geometry."""
import bpy, ast, json
from pathlib import Path
root=Path('C:/workspace/multigame')
source=root/'assets/models/club/room_mexico_recuerdos.glb'
out=root/'temp/audio-night/room_mexico_recuerdos.glb'
out.parent.mkdir(parents=True,exist_ok=True)
original=bpy.context.window.scene
selected=list(bpy.context.selected_objects)
active=bpy.context.view_layer.objects.active
scene=bpy.data.scenes.new('MexicoMaterialRepair')
materials={}
for node in ast.walk(ast.parse((root/'tools/build_mexico_recuerdos.py').read_text(encoding='utf-8-sig'))):
 if isinstance(node,ast.Call) and isinstance(node.func,ast.Name) and node.func.id=='create_pbr_mat':
  args=[ast.literal_eval(a) for a in node.args]
  materials[args[0]]={'base':args[1],**{k.arg:ast.literal_eval(k.value) for k in node.keywords}}
try:
 bpy.context.window.scene=scene
 for sc in bpy.data.scenes:
  for ob in sc.objects: ob.select_set(False)
 bpy.ops.import_scene.gltf(filepath=str(source))
 repaired={}
 for ob in scene.objects:
  if ob.type!='MESH':continue
  for slot in ob.material_slots:
   old=slot.material
   if not old:continue
   name=old.name.split('.')[0]
   if name not in materials:raise RuntimeError('Unknown palette '+name)
   if name not in repaired:
    data=materials[name];mat=bpy.data.materials.new('Repaired_'+name);mat.use_nodes=True
    mat.node_tree.nodes.clear()
    bsdf=mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled');output=mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
    mat.node_tree.links.new(bsdf.outputs['BSDF'],output.inputs['Surface'])
    bsdf.inputs['Base Color'].default_value=(*data['base'],1)
    bsdf.inputs['Roughness'].default_value=data.get('roughness',.75)
    bsdf.inputs['Metallic'].default_value=data.get('metallic',0)
    if data.get('emission_rgb'):
     bsdf.inputs['Emission Color'].default_value=(*data['emission_rgb'],1)
     bsdf.inputs['Emission Strength'].default_value=data.get('emission_strength',0)
    mat.diffuse_color=(*data['base'],1);repaired[name]=mat
   slot.material=repaired[name]
 for ob in scene.objects:ob.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(out),export_format='GLB',use_selection=True,export_materials='EXPORT')
 print(json.dumps({'original_scene':original.name,'objects':len(scene.objects),'materials':len(repaired),'candidate':str(out)}))
finally:
 bpy.context.window.scene=original
 for ob in selected:
  if ob.name in original.objects:ob.select_set(True)
 if active and active.name in original.objects:bpy.context.view_layer.objects.active=active
 bpy.data.scenes.remove(scene)
