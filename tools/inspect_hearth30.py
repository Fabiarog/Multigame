"""Read Blender context and inspect the existing fireplace without replacing it."""
import json
from pathlib import Path
from blender_bridge import request
root=Path(__file__).resolve().parents[1]
out=root/'art/blender/patch30';out.mkdir(parents=True,exist_ok=True)
info=request('get_scene_info')
(out/'scene-before.json').write_text(json.dumps(info,indent=2))
request('get_viewport_screenshot',{'filepath':str(out/'viewport-before.png'),'max_size':800})
statuses={}
for provider in ['polyhaven','sketchfab','polypizza','hyper3d','hunyuan3d']:
    try:
        response=request('get_'+provider+'_status')
        statuses[provider]={'status':response.get('status'),'enabled':response.get('result',{}).get('enabled')}
    except Exception as e: statuses[provider]={'unavailable':type(e).__name__}
(out/'integrations.json').write_text(json.dumps(statuses,indent=2))
code='''import bpy,json
from mathutils import Vector
original=bpy.context.window.scene
with bpy.data.libraries.load(SOURCE) as (data,loaded):loaded.scenes=data.scenes
scene=loaded.scenes[0]
try:
 bpy.context.window.scene=scene
 rows=[]
 for o in scene.objects:
  if 'Fire' in o.name or 'Wall' in o.name:
   rows.append({'name':o.name,'bounds':[[min((o.matrix_world@Vector(c))[i] for c in o.bound_box) for i in range(3)],[max((o.matrix_world@Vector(c))[i] for c in o.bound_box) for i in range(3)]],'vertices':len(o.data.vertices) if o.type=='MESH' else 0,'materials':[m.name for m in o.data.materials] if o.type=='MESH' else []})
 print(json.dumps(rows))
finally:
 bpy.context.window.scene=original
 for o in list(scene.objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.scenes.remove(scene)
'''
response=request('execute_code',{'code':'SOURCE='+repr(str(root/'art/blender/patch27/room_classic_club.blend'))+'\n'+code})
(out/'hearth-audit.json').write_text(json.dumps(response,indent=2))
print(response)
