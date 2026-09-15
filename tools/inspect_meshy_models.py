import bpy
import os
import json

folder = r'c:\workspace\multigame\assets\modelos 3d detalhados'
blend_files = [f for f in os.listdir(folder) if f.endswith('.blend')]

info = {}
for bf in blend_files:
    path = os.path.join(folder, bf)
    bpy.ops.wm.open_mainfile(filepath=path)
    
    scene_info = {
        'objects': [],
        'meshes': [],
        'materials': [],
        'images': [],
        'armatures': []
    }
    
    for obj in bpy.data.objects:
        obj_data = {
            'name': obj.name,
            'type': obj.type,
            'location': list(obj.location),
            'dimensions': list(obj.dimensions),
        }
        if obj.type == 'MESH' and obj.data:
            obj_data['vertices'] = len(obj.data.vertices)
            obj_data['polygons'] = len(obj.data.polygons)
        scene_info['objects'].append(obj_data)
        
    for mat in bpy.data.materials:
        scene_info['materials'].append(mat.name)
        
    for img in bpy.data.images:
        scene_info['images'].append({'name': img.name, 'size': list(img.size), 'filepath': img.filepath})
        
    for arm in bpy.data.armatures:
        scene_info['armatures'].append({'name': arm.name, 'bones': [b.name for b in arm.bones]})
        
    info[bf] = scene_info

print('=== INSPECTION RESULT ===')
print(json.dumps(info, indent=2))
