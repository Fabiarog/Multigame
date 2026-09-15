import bpy
import os

src_folder = r'c:\workspace\multigame\assets\modelos 3d detalhados'
dst_folder = r'c:\workspace\multigame\assets\models\club_detailed'
os.makedirs(dst_folder, exist_ok=True)

models = {
    'Meshy_AI_Corvin_Dapperwing_0907033510_texture.blend': 'corvo_detailed.glb',
    'Meshy_AI_The_Dapper_Owl_0907034231_texture.blend': 'barao_detailed.glb',
    'Meshy_AI_Cobra_Queen_0907035025_texture.blend': 'dama_detailed.glb',
    'Meshy_AI_Dapper_Fox_Detective_0907034820_texture.blend': 'zeca_detailed.glb',
    'Meshy_AI_Capybara_Lady_0907035257_texture.blend': 'iara_detailed.glb'
}

for src_name, dst_name in models.items():
    src_path = os.path.join(src_folder, src_name)
    if not os.path.exists(src_path):
        continue
    bpy.ops.wm.open_mainfile(filepath=src_path)
    
    # Check meshes
    mesh_obj = None
    for o in bpy.data.objects:
        if o.type == 'MESH':
            mesh_obj = o
            # ensure origin at feet
            o.select_set(True)
            bpy.context.view_layer.objects.active = o
            break
            
    if mesh_obj:
        # Reposition mesh so feet touch Z=0
        bbox = mesh_obj.bound_box
        min_z = min(b[2] for b in bbox)
        # Apply translation
        mesh_obj.location.z -= min_z
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
        
    dst_path = os.path.join(dst_folder, dst_name)
    bpy.ops.export_scene.gltf(
        filepath=dst_path,
        export_format='GLB',
        use_selection=False,
        export_apply=True,
        export_materials='EXPORT',
        export_image_format='AUTO'
    )
    print(f'Exported: {dst_path} (size: {os.path.getsize(dst_path)} bytes)')
