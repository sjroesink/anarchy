"""Local reference reconstruction. Transform/UV convention remains provisional."""
import bpy,json,os,math,struct
from mathutils import Vector,Matrix
root=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
directory=os.path.join(root,'Research','ClientReference')
data=json.load(open(os.path.join(directory,'solar-rifle-geometry.json')))
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
texture=bpy.data.images.load(os.path.join(directory,'solar-rifle-texture.bin'))
material=bpy.data.materials.new('Original medium_blaster reference');material.use_nodes=True
nodes=material.node_tree.nodes;image=nodes.new('ShaderNodeTexImage');image.image=texture
material.node_tree.links.new(image.outputs['Color'],nodes.get('Principled BSDF').inputs['Base Color'])
nodes.get('Principled BSDF').inputs['Roughness'].default_value=.7
meshes={m['objectId']:m for m in data['meshes']};nodes_by_id={n['objectId']:n for n in data['nodes']}
def value(identity,name):
    raw=next(f['payload'] for f in data['objects'][identity+1] if f['name']==name)
    return struct.unpack('<I',bytes.fromhex(raw))[0]
def visit(identity,parent):
    node=nodes_by_id.get(identity)
    if node is None:return
    flat=node['matrix'];local=Matrix([flat[n:n+4] for n in range(0,16,4)]).transposed()
    world=parent@local
    source=meshes[value(node['dataObject'],'mesh')]
    mesh=bpy.data.meshes.new('Original mesh '+str(source['objectId']))
    mesh.from_pydata([v[:3] for v in source['vertices']],[],source['triangles']);mesh.update()
    obj=bpy.data.objects.new(mesh.name,mesh);bpy.context.collection.objects.link(obj);obj.matrix_world=world
    mesh.materials.append(material);uv=mesh.uv_layers.new(name='Reference UV')
    for loop in mesh.loops:
        v=source['vertices'][loop.vertex_index];uv.data[loop.index].uv=(v[6],1-v[7])
    for child in node['children']:visit(child,world)
visit(0,Matrix.Rotation(math.pi/2,4,'X'))
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(v) for o in bpy.data.objects if o.type=='MESH' for v in o.bound_box]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)));center=(lo+hi)/2
bpy.ops.object.camera_add(location=center+Vector((3,-.5,1)))
camera=bpy.context.object;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=1.5
scene=bpy.context.scene;scene.camera=camera;scene.world.color=(.08,.08,.08)
for offset in [(2,-2,3),(-2,1,2)]:
    bpy.ops.object.light_add(type='AREA',location=center+Vector(offset));light=bpy.context.object;light.data.energy=250;light.data.shape='DISK';light.data.size=3
    light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1400;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.render.filepath=os.path.join(directory,'original-rifle-reference.png');bpy.ops.render.render(write_still=True)
print('ORIGINAL_REFERENCE_RENDER_OK: provisional transform and UV reconstruction, not shipped art')
