import bpy
import mathutils
from mathutils import Vector, Euler
import math
import os

src_file = r'c:\workspace\multigame\assets\modelos 3d detalhados\Meshy_AI_Corvin_Dapperwing_0907033510_texture.blend'
bpy.ops.wm.open_mainfile(filepath=src_file)

# Find mesh object
mesh_obj = None
for o in bpy.data.objects:
    if o.type == 'MESH':
        mesh_obj = o
        break

print('Found mesh:', mesh_obj.name, 'verts:', len(mesh_obj.data.vertices))

# Remove cameras and lights
for o in list(bpy.data.objects):
    if o.type in ('CAMERA', 'LIGHT'):
        bpy.data.objects.remove(o, do_unlink=True)

# Adjust mesh so feet touch ground (Z = 0)
bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
min_z = min(b.z for b in bbox)
max_z = max(b.z for b in bbox)
height = max_z - min_z

print(f'Original min_z={min_z:.3f}, max_z={max_z:.3f}, height={height:.3f}')

# Translate mesh to ground
mesh_obj.location.z -= min_z
bpy.ops.object.select_all(action='DESELECT')
mesh_obj.select_set(True)
bpy.context.view_layer.objects.active = mesh_obj
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

# Recompute bbox
bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
min_z = min(b.z for b in bbox)
max_z = max(b.z for b in bbox)
print(f'Grounded min_z={min_z:.3f}, max_z={max_z:.3f}')

# Create Armature
arm_data = bpy.data.armatures.new('ArmatureData')
arm_obj = bpy.data.objects.new('Armature', arm_data)
bpy.context.collection.objects.link(arm_obj)
bpy.context.view_layer.objects.active = arm_obj
bpy.ops.object.mode_set(mode='EDIT')

edit_bones = arm_data.edit_bones

def make_bone(name, head, tail, parent=None):
    b = edit_bones.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    if parent:
        b.parent = parent
        b.use_connect = False
    return b

# Central spine chain
root = make_bone('Root', (0, 0, 0), (0, 0, 0.15))
pelvis = make_bone('Pelvis', (0, 0, 0.90), (0, 0, 1.05), root)
spine = make_bone('Spine', (0, 0, 1.05), (0, 0, 1.25), pelvis)
chest = make_bone('Chest', (0, 0, 1.25), (0, 0, 1.45), spine)
neck = make_bone('Neck', (0, 0, 1.45), (0, -0.02, 1.55), chest)
head_bone = make_bone('Head', (0, -0.02, 1.55), (0, -0.05, 1.85), neck)

# Arms
for side, sign in [('L', 1), ('R', -1)]:
    sh = make_bone(f'Shoulder.{side}', (sign * 0.05, 0, 1.42), (sign * 0.20, 0.01, 1.40), chest)
    ua = make_bone(f'UpperArm.{side}', (sign * 0.20, 0.01, 1.40), (sign * 0.32, 0.05, 1.10), sh)
    fa = make_bone(f'Forearm.{side}', (sign * 0.32, 0.05, 1.10), (sign * 0.26, -0.15, 0.85), ua)
    hnd = make_bone(f'Hand.{side}', (sign * 0.26, -0.15, 0.85), (sign * 0.18, -0.26, 0.82), fa)

# Legs
for side, sign in [('L', 1), ('R', -1)]:
    thigh = make_bone(f'Thigh.{side}', (sign * 0.15, 0, 0.90), (sign * 0.16, 0.02, 0.50), pelvis)
    shin = make_bone(f'Shin.{side}', (sign * 0.16, 0.02, 0.50), (sign * 0.16, 0.01, 0.12), thigh)
    foot = make_bone(f'Foot.{side}', (sign * 0.16, 0.01, 0.12), (sign * 0.16, -0.18, 0.02), shin)

bpy.ops.object.mode_set(mode='OBJECT')
print('Armature created with', len(arm_data.bones), 'bones')

# Parent mesh to armature with automatic weights
mesh_obj.select_set(True)
arm_obj.select_set(True)
bpy.context.view_layer.objects.active = arm_obj
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
print('Parented with ARMATURE_AUTO successfully!')
