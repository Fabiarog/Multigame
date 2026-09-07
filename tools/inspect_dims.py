import bpy
import os
import mathutils
from mathutils import Vector

folder = r'c:\workspace\multigame\assets\modelos 3d detalhados'
blend_files = {
    'corvo': 'Meshy_AI_Corvin_Dapperwing_0907033510_texture.blend',
    'barao': 'Meshy_AI_The_Dapper_Owl_0907034231_texture.blend',
    'dama': 'Meshy_AI_Cobra_Queen_0907035025_texture.blend',
    'zeca': 'Meshy_AI_Dapper_Fox_Detective_0907034820_texture.blend',
    'iara': 'Meshy_AI_Capybara_Lady_0907035257_texture.blend'
}

for ident, fname in blend_files.items():
    path = os.path.join(folder, fname)
    bpy.ops.wm.open_mainfile(filepath=path)
    
    mesh = None
    for o in bpy.data.objects:
        if o.type == 'MESH':
            mesh = o
            break
            
    bbox = [mesh.matrix_world @ Vector(b) for b in mesh.bound_box]
    min_x, max_x = min(b.x for b in bbox), max(b.x for b in bbox)
    min_y, max_y = min(b.y for b in bbox), max(b.y for b in bbox)
    min_z, max_z = min(b.z for b in bbox), max(b.z for b in bbox)
    
    print(f'[{ident.upper()}]')
    print(f'  X: {min_x:.3f} to {max_x:.3f} (width: {max_x-min_x:.3f})')
    print(f'  Y: {min_y:.3f} to {max_y:.3f} (depth: {max_y-min_y:.3f})')
    print(f'  Z: {min_z:.3f} to {max_z:.3f} (height: {max_z-min_z:.3f})')
