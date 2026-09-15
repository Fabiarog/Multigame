import bpy
import mathutils
from mathutils import Vector, Euler
import math
import os

src_file = r'c:\workspace\multigame\assets\modelos 3d detalhados\Meshy_AI_Corvin_Dapperwing_0907033510_texture.blend'
bpy.ops.wm.open_mainfile(filepath=src_file)

mesh_obj = None
for o in bpy.data.objects:
    if o.type == 'MESH':
        mesh_obj = o
        break

# Ground mesh
bbox = [mesh_obj.matrix_world @ Vector(b) for b in mesh_obj.bound_box]
min_z = min(b.z for b in bbox)
mesh_obj.location.z -= min_z
bpy.ops.object.select_all(action='DESELECT')
mesh_obj.select_set(True)
bpy.context.view_layer.objects.active = mesh_obj
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

# Rename material to corvo_skin
if mesh_obj.data.materials:
    mat = mesh_obj.data.materials[0]
    mat.name = 'corvo_skin'
    mat.diffuse_color = (0.09, 0.12, 0.17, 1.0) # corvo skin palette

# Armature
arm_data = bpy.data.armatures.new('CorvoArmature')
arm_obj = bpy.data.objects.new('Armature', arm_data)
bpy.context.collection.objects.link(arm_obj)
bpy.context.view_layer.objects.active = arm_obj
bpy.ops.object.mode_set(mode='EDIT')

eb = arm_data.edit_bones

def add_bone(name, head, tail, parent=None):
    b = eb.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    if parent:
        b.parent = parent
        b.use_connect = False
    return b

root = add_bone('Root', (0, 0, 0), (0, 0, 0.15))
pelvis = add_bone('Pelvis', (0, 0, 0.90), (0, 0, 1.05), root)
spine = add_bone('Spine', (0, 0, 1.05), (0, 0, 1.25), pelvis)
chest = add_bone('Chest', (0, 0, 1.25), (0, 0, 1.45), spine)
neck = add_bone('Neck', (0, 0, 1.45), (0, -0.02, 1.55), chest)
head = add_bone('Head', (0, -0.02, 1.55), (0, -0.05, 1.85), neck)

# Arms
sh_l = add_bone('Shoulder.L', (0.05, 0, 1.42), (0.20, 0.01, 1.40), chest)
ua_l = add_bone('UpperArm.L', (0.20, 0.01, 1.40), (0.32, 0.05, 1.10), sh_l)
fa_l = add_bone('Forearm.L', (0.32, 0.05, 1.10), (0.26, -0.15, 0.85), ua_l)
h_l = add_bone('Hand.L', (0.26, -0.15, 0.85), (0.18, -0.26, 0.82), fa_l)

sh_r = add_bone('Shoulder.R', (-0.05, 0, 1.42), (-0.20, 0.01, 1.40), chest)
ua_r = add_bone('UpperArm.R', (-0.20, 0.01, 1.40), (-0.32, 0.05, 1.10), sh_r)
fa_r = add_bone('Forearm.R', (-0.32, 0.05, 1.10), (-0.26, -0.15, 0.85), ua_r)
h_r = add_bone('Hand.R', (-0.26, -0.15, 0.85), (-0.18, -0.26, 0.82), fa_r)

# Legs
th_l = add_bone('Thigh.L', (0.15, 0, 0.90), (0.16, 0.02, 0.50), pelvis)
shn_l = add_bone('Shin.L', (0.16, 0.02, 0.50), (0.16, 0.01, 0.12), th_l)
ft_l = add_bone('Foot.L', (0.16, 0.01, 0.12), (0.16, -0.18, 0.02), shn_l)

th_r = add_bone('Thigh.R', (-0.15, 0, 0.90), (-0.16, 0.02, 0.50), pelvis)
shn_r = add_bone('Shin.R', (-0.16, 0.02, 0.50), (-0.16, 0.01, 0.12), th_r)
ft_r = add_bone('Foot.R', (-0.16, 0.01, 0.12), (-0.16, -0.18, 0.02), shn_r)

bpy.ops.object.mode_set(mode='OBJECT')

# Head empty marker for GameplayChecks
head_marker = bpy.data.objects.new('Head', None)
head_marker.location = (0, -0.05, 1.60)
head_marker.parent = arm_obj
bpy.context.collection.objects.link(head_marker)

# Parent mesh with auto weights
mesh_obj.select_set(True)
arm_obj.select_set(True)
bpy.context.view_layer.objects.active = arm_obj
bpy.ops.object.parent_set(type='ARMATURE_AUTO')

# Switch to POSE mode to animate
bpy.context.view_layer.objects.active = arm_obj
bpy.ops.object.mode_set(mode='POSE')

for pb in arm_obj.pose.bones:
    pb.rotation_mode = 'XYZ'

# Define animation clips
clips = ['idle', 'entrance', 'truco', 'victory', 'boss_intro', 'flourish', 'play_card']

arm_obj.animation_data_create()

for clip in clips:
    act = bpy.data.actions.new(name=clip)
    arm_obj.animation_data.action = act
    
    # Create simple distinct keyframes for test
    if clip == 'idle':
        # Breathing
        chest_p = arm_obj.pose.bones['Chest']
        chest_p.location = (0, 0, 0)
        chest_p.keyframe_insert('location', frame=1)
        chest_p.location = (0, 0, 0.02)
        chest_p.keyframe_insert('location', frame=32)
        chest_p.location = (0, 0, 0)
        chest_p.keyframe_insert('location', frame=64)
    elif clip == 'play_card':
        r_arm = arm_obj.pose.bones['UpperArm.R']
        r_arm.rotation_euler = (0, 0, 0)
        r_arm.keyframe_insert('rotation_euler', frame=1)
        r_arm.rotation_euler = (-0.8, 0.2, -0.1)
        r_arm.keyframe_insert('rotation_euler', frame=24)
        r_arm.rotation_euler = (0, 0, 0)
        r_arm.keyframe_insert('rotation_euler', frame=48)
    else:
        spine_p = arm_obj.pose.bones['Spine']
        spine_p.rotation_euler = (0, 0, 0)
        spine_p.keyframe_insert('rotation_euler', frame=1)
        spine_p.rotation_euler = (-0.1, 0, 0)
        spine_p.keyframe_insert('rotation_euler', frame=24)
        spine_p.rotation_euler = (0, 0, 0)
        spine_p.keyframe_insert('rotation_euler', frame=48)
        
    track = arm_obj.animation_data.nla_tracks.new()
    track.name = clip
    strip = track.strips.new(clip, 1, act)
    strip.extrapolation = 'HOLD' if clip == 'idle' else 'NOTHING'

arm_obj.animation_data.action = None
bpy.ops.object.mode_set(mode='OBJECT')

out_test = r'c:\workspace\multigame\temp\test_corvo_rigged.glb'
os.makedirs(r'c:\workspace\multigame\temp', exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(
    filepath=out_test,
    export_format='GLB',
    use_selection=True,
    export_apply=True,
    export_animation_mode='NLA_TRACKS',
    export_materials='EXPORT'
)
print('Exported test GLB:', out_test, 'size:', os.path.getsize(out_test))
