"""Blender-only original reference study; transforms, UV and shading are provisional."""
import argparse,hashlib,json,math,struct,sys
from pathlib import Path
import bpy
from mathutils import Matrix,Vector

root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'Tools'))
from decode_client_items import records
from decode_client_mesh import decode
from decode_mesh_materials import resolve_materials

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--mesh-id',type=int,required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
client=root/'Research/ClientReference/18.8.50/app'
output=root/'Research/ClientReference/WeaponStudies'/str(args.mesh_id)
payload=next(data for identity,data in records(client,1010001) if identity==args.mesh_id)
data=decode(payload);graph=resolve_materials(data)
for material in graph:
    channels=material['textureChannels']
    if len(channels)!=1 or channels[0]['channel']!=0 or channels[0]['resourceType']!=1010004:
        raise ValueError('Unreviewed texture-channel layout')
textures={c['resourceId'] for m in graph for c in m['textureChannels']}
output.mkdir(parents=True,exist_ok=True)
paths={};texture_report=[]
for identity,raw in records(client,1010004):
    if identity not in textures:continue
    path=output/f'texture-{identity}.bin';path.write_bytes(raw);paths[identity]=path
    texture_report.append({'id':identity,'sha256':hashlib.sha256(raw).hexdigest()})
if set(paths)!=textures:raise ValueError('Missing texture resource')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials={}
for entry in graph:
    mat=bpy.data.materials.new('Original material '+str(entry['materialObject']));mat.use_nodes=True
    image=bpy.data.images.load(str(paths[entry['textureChannels'][0]['resourceId']]),check_existing=True)
    if min(image.size)<=0 or len(image.pixels)==0:raise ValueError('Texture failed to load')
    node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
    shader=mat.node_tree.nodes.get('Principled BSDF')
    mat.node_tree.links.new(node.outputs['Color'],shader.inputs['Base Color'])
    shader.inputs['Roughness'].default_value=.7
    materials[entry['materialObject']]=mat

meshes={m['objectId']:m for m in data['meshes']};nodes={n['objectId']:n for n in data['nodes']}
bindings=[];seen_meshes=set()
def field(identity,name):
    matches=[f['payload'] for f in data['objects'][identity+1] if f['name']==name]
    if len(matches)!=1:raise ValueError('Missing or ambiguous scene field '+name)
    return matches[0]
def visit(identity,parent,ancestors):
    if identity in ancestors:raise ValueError('Cyclic scene hierarchy')
    node=nodes.get(identity)
    if node is None:
        class_id=struct.unpack('<I',field(identity,'__class_id__'))[0]
        cls=data['symbols'][class_id][1];count=struct.unpack('<I',field(identity,'chld_cnt'))[0]
        if cls in ('FAFAttractor_t','FAFCollisionBox_c') and count==0:return
        if cls!='RRefFrame_t':raise ValueError('Unreviewed scene node')
        children=[v[0] for v in struct.iter_unpack('<i',field(identity,'chld'))] if count else []
        if len(children)!=count:raise ValueError('Scene child count mismatch')
        node={'matrix':struct.unpack('<16f',field(identity,'anim_matrix')),'children':children,'dataObject':None}
    flat=node['matrix'];world=parent@Matrix([flat[i:i+4] for i in range(0,16,4)]).transposed()
    mesh_refs=field(node['dataObject'],'mesh') if node['dataObject'] is not None else b''
    for (mesh_id,) in struct.iter_unpack('<i',mesh_refs):
        source=meshes[mesh_id]
        mesh=bpy.data.meshes.new(f'Original mesh {mesh_id}')
        mesh.from_pydata([v[:3] for v in source['vertices']],[],source['triangles']);mesh.update()
        mesh.normals_split_custom_set_from_vertices([v[3:6] for v in source['vertices']])
        for polygon in mesh.polygons:polygon.use_smooth=True
        obj=bpy.data.objects.new(mesh.name,mesh);bpy.context.collection.objects.link(obj);obj.matrix_world=world
        mesh.materials.append(materials[source['materialObject']]);uv=mesh.uv_layers.new(name='Provisional V-flipped UV')
        for loop in mesh.loops:
            v=source['vertices'][loop.vertex_index];uv.data[loop.index].uv=(v[6],1-v[7])
        seen_meshes.add(mesh_id)
        bindings.append({'nodeObject':identity,'meshObject':mesh_id,'materialObject':source['materialObject'],
                         'vertices':len(mesh.vertices),'triangles':len(mesh.polygons)})
    for child in node['children']:visit(child,world,ancestors|{identity})
root_ref=struct.unpack('<i',next(f['payload'] for f in data['objects'][0] if f['name']=='obj'))[0]
visit(root_ref,Matrix.Rotation(math.pi/2,4,'X'),set())
if seen_meshes!=set(meshes):raise ValueError('Scene traversal omitted geometry')
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(v) for o in bpy.data.objects if o.type=='MESH' for v in o.bound_box]
low=Vector(tuple(min(p[i] for p in points) for i in range(3)))
high=Vector(tuple(max(p[i] for p in points) for i in range(3)));center=(low+high)/2;extent=max(high-low)
if not math.isfinite(extent) or extent<=0:raise ValueError('Invalid world bounds')
bpy.ops.object.camera_add(location=center+Vector((3,-.5,1)).normalized()*extent*3)
camera=bpy.context.object;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO'
camera.data.clip_end=max(100,extent*10)
rotation=camera.rotation_euler.to_matrix().transposed();projected=[rotation@(p-center) for p in points]
width=max(p.x for p in projected)-min(p.x for p in projected);height=max(p.y for p in projected)-min(p.y for p in projected)
camera.data.ortho_scale=max(width,height*2)*1.15
scene=bpy.context.scene;scene.camera=camera;scene.world.color=(.08,.08,.08)
for offset in [(2,-2,3),(-2,1,2)]:
    bpy.ops.object.light_add(type='AREA',location=center+Vector(offset)*extent)
    light=bpy.context.object;light.data.energy=250*extent**2;light.data.shape='DISK';light.data.size=3*extent
    light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1400;scene.render.resolution_y=700;scene.render.resolution_percentage=100
scene.render.filepath=str(output/'reference.png')
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(output/'reference.blend'))
bpy.ops.render.render(write_still=True)
report={'meshId':args.mesh_id,'sourceVersion':'18.8.50_EP1','sourceSha256':hashlib.sha256(payload).hexdigest(),
        'scope':__doc__,'bindings':bindings,'materials':graph,'textures':texture_report,
        'renderSha256':hashlib.sha256((output/'reference.png').read_bytes()).hexdigest(),
        'blendSha256':hashlib.sha256((output/'reference.blend').read_bytes()).hexdigest()}
(root/'Artifacts'/f'weapon-study-{args.mesh_id}.json').write_text(json.dumps(report,indent=2)+'\n')
print('WEAPON_REFERENCE_OK',args.mesh_id,len(bindings),'parts',len(materials),'materials')
