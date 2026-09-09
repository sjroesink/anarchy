"""Render the editable Soldier collection for proportion and armor review."""
import bpy, os, sys, math, json
from mathutils import Vector
root=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
bpy.ops.wm.open_mainfile(filepath=os.path.join(root,'Art','Blender','DistrictKit.blend'))
objects=list(bpy.data.collections['Soldier'].objects)
for obj in bpy.data.objects:obj.hide_render=obj not in objects
meshes=[obj for obj in objects if obj.type=='MESH']
weapon='--weapon' in sys.argv
visible_meshes=[obj for obj in meshes if obj.name in ('Rifle','Barrel')] if weapon else meshes
if weapon:
    for obj in meshes:obj.hide_render=obj not in visible_meshes
points=[obj.matrix_world@Vector(v) for obj in visible_meshes for v in obj.bound_box]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
center=(lo+hi)/2
bpy.ops.object.camera_add(location=center+Vector((2.7,-4.5,1.6)))
camera=bpy.context.object
if weapon:camera.location=center+Vector((3,-.5,1))
camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=1.4 if weapon else 2.6
scene=bpy.context.scene;scene.camera=camera
for offset,power,size in [((2,-3,4),750,4),((-2,-1,2),500,3),((1,3,3),900,3)]:
    bpy.ops.object.light_add(type='AREA',location=center+Vector(offset))
    light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size
    light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
scene.world.color=(.06,.07,.09)
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
if weapon:scene.render.resolution_x=1400;scene.render.resolution_y=700
rig=next(obj for obj in objects if obj.type=='ARMATURE')
assert len(rig.data.bones)==17
vertices=0
for obj in meshes:
    assert obj.parent==rig
    assert any(mod.type=='ARMATURE' and mod.object==rig for mod in obj.modifiers)
    for vertex in obj.data.vertices:
        assert abs(sum(g.weight for g in vertex.groups)-1)<.00001
        assert all(obj.vertex_groups[g.group].name in rig.data.bones for g in vertex.groups)
        vertices+=1
posed='--pose' in sys.argv
if posed:
    rig.animation_data.action=None
    rig.pose.bones['Rig_ForearmR'].rotation_mode='XYZ'
    rig.pose.bones['Rig_ForearmR'].rotation_euler.x=math.radians(-65)
    rig.pose.bones['Rig_UpperArmL'].rotation_mode='XYZ'
    rig.pose.bones['Rig_UpperArmL'].rotation_euler.x=math.radians(15)
    bpy.context.view_layer.update()
report={'bones':len(rig.data.bones),'skinnedMeshes':len(meshes),'normalizedWeightedVertices':vertices,
        'posePreview':posed,'scope':'Blender binding integrity, not animation or AO appearance conformance'}
with open(os.path.join(root,'Artifacts','soldier-rig-validation.json'),'w') as output:json.dump(report,output,indent=2)
scene.render.image_settings.file_format='PNG';scene.render.filepath=os.path.join(root,'Artifacts','solar-rifle-study.png' if weapon else 'soldier-posed.png' if posed else 'soldier-study.png')
bpy.ops.render.render(write_still=True)
print('SOLDIER_REVIEW_RENDER_OK')
