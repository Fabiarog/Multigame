"""Test building Zeca with ears and card socket in Blender 5.2."""
import bpy
from pathlib import Path

ROOT = Path(r"c:\workspace\multigame")
blend_path = ROOT / "assets/modelos 3d detalhados/Meshy_AI_Dapper_Fox_Detective_0907034820_texture.blend"
bpy.ops.wm.open_mainfile(filepath=str(blend_path))

# Locate mesh
mesh_obj = next((o for o in bpy.data.objects if o.type == 'MESH'), None)
print(f"Found mesh: {mesh_obj.name}, verts: {len(mesh_obj.data.vertices)}")
print("Test load succeeded!")
