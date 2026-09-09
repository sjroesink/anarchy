"""Reproducible Blender source models and Unity FBX exports. Run with Blender -b -P."""
import bpy, math, os, random
from mathutils import Vector, Matrix
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'Unity', 'Assets', 'Art', 'Models')
os.makedirs(OUT, exist_ok=True)
os.makedirs(os.path.join(ROOT, 'Art', 'Blender'), exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
materials = {}
for name, color, metal, glow in [('Graphite',(0.065,.09,.12),.75,0),('Steel',(.24,.31,.36),.65,0),('Concrete',(.29,.32,.34),.1,0),('Dark',(.017,.029,.04),.3,0),('Cyan',(.08,.72,1),.3,3),('Amber',(1,.29,.055),.3,2),('Ivory',(.66,.71,.69),.5,0),('Armor',(.19,.205,.17),.35,0),('Fabric',(.038,.043,.037),0,0),('WeaponMetal',(.042,.038,.034),.65,0),('WeaponEdge',(.13,.12,.105),.7,0),('WeaponGrip',(.105,.073,.039),0,0),('Foliage',(.16,.22,.10),0,0),('FoliageLight',(.32,.35,.16),0,0),('Soil',(.12,.095,.065),0,0)]:
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1); p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=.78 if name=='Fabric' else .58 if name=='Armor' else .38
    p.inputs['Emission Color'].default_value=(*color,1); p.inputs['Emission Strength'].default_value=glow
    if name.startswith('Weapon'):p.inputs['Roughness'].default_value=.64 if name=='WeaponGrip' else .56
    materials[name]=m
def box(name, pos, size, mat='Steel', bevel=.04):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos); o=bpy.context.object; o.name=name; o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(materials[mat])
    if bevel:
        b=o.modifiers.new('Machined edges','BEVEL'); b.width=bevel; b.segments=2
        bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=b.name)
        o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return o
def sphere(name,pos,size,mat='Steel'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=pos); o=bpy.context.object; o.name=name; o.scale=size; o.data.materials.append(materials[mat]); bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for p in o.data.polygons: p.use_smooth=True
    return o
def cyl(name,pos,radius,depth,mat='Steel'):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=radius,depth=depth,location=pos); o=bpy.context.object; o.name=name; o.data.materials.append(materials[mat]); return o
def body_form(name,x,rings,mat='Fabric'):
    """Closed elliptical cross sections: (height, half-width, half-depth, y)."""
    sides=16;vertices=[];faces=[]
    for z,width,depth,y in rings:
        vertices.extend((x+width*math.cos(i*2*math.pi/sides),y+depth*math.sin(i*2*math.pi/sides),z) for i in range(sides))
    faces.append(tuple(reversed(range(sides))))
    for ring in range(len(rings)-1):
        for i in range(sides):
            a=ring*sides+i;b=ring*sides+(i+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces.append(tuple(range((len(rings)-1)*sides,len(rings)*sides)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    mesh.materials.append(materials[mat])
    for polygon in mesh.polygons:polygon.use_smooth=len(polygon.vertices)==4
    return obj

def armor_panel(name,outline,y,thickness=.045):
    vertices=[(x,depth,z) for depth in (y,y+thickness) for x,z in outline]
    n=len(outline);faces=[tuple(range(n)),tuple(reversed(range(n,2*n)))]
    faces.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);mesh.materials.append(materials['Armor'])
    bevel=obj.modifiers.new('Plate edge','BEVEL');bevel.width=.009;bevel.segments=2
    obj.modifiers.new('Plate normals','WEIGHTED_NORMAL')
    return obj
def export(name, fn, combine=True):
    bpy.ops.object.select_all(action='DESELECT'); old=set(bpy.data.objects); fn(); obs=list(set(bpy.data.objects)-old)
    if combine:
        groups={mat:[o for o in obs if o.type=='MESH' and o.data.materials[0].name==mat] for mat in materials}
        for mat,group in groups.items():
            if not group: continue
            bpy.ops.object.select_all(action='DESELECT')
            for o in group:o.select_set(True)
            bpy.context.view_layer.objects.active=group[0]; bpy.ops.object.convert(target='MESH'); bpy.ops.object.join(); bpy.context.object.name=name+'_'+mat
        obs=list(set(bpy.data.objects)-old)
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs:o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,name+'.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=name=='Soldier',bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,add_leaf_bones=False)
    collection=bpy.data.collections.new(name); bpy.context.scene.collection.children.link(collection)
    for o in obs:
        for c in list(o.users_collection): c.objects.unlink(o)
        collection.objects.link(o)
    # Arrange source kit for convenient editing; exports remain at their local origins.
    offset=len(bpy.data.collections)*18
    for o in obs:
        if o.parent not in obs:o.location.x+=offset

def rig_soldier(objects):
    """Authored skeleton and deterministic weights; armor uses rigid binding."""
    armature=bpy.data.armatures.new('SoldierSkeleton')
    rig=bpy.data.objects.new('SoldierRig',armature);bpy.context.collection.objects.link(rig)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    def bone(name,head,tail,parent=None):
        b=armature.edit_bones.new('Rig_'+name);b.head=head;b.tail=tail
        if parent:b.parent=armature.edit_bones['Rig_'+parent]
    bone('Hips',(0,0,.94),(0,0,1.10))
    bone('Spine',(0,0,1.10),(0,0,1.35),'Hips')
    bone('Chest',(0,0,1.35),(0,0,1.57),'Spine')
    bone('Neck',(0,0,1.57),(0,0,1.70),'Chest')
    bone('Head',(0,0,1.70),(0,0,1.98),'Neck')
    for side,x in [('L',-.30),('R',.30)]:
        bone('UpperArm'+side,(x,0,1.48),(x,0,1.09),'Chest')
        bone('Forearm'+side,(x,0,1.09),(x,-.04,.87),'UpperArm'+side)
        bone('Hand'+side,(x,-.04,.87),(x,-.055,.75),'Forearm'+side)
        leg_x=-.115 if side=='L' else .115
        bone('Thigh'+side,(leg_x,0,.94),(leg_x,0,.535),'Hips')
        bone('Shin'+side,(leg_x,0,.535),(leg_x,0,.20),'Thigh'+side)
        bone('Foot'+side,(leg_x,0,.20),(leg_x,-.23,.08),'Shin'+side)
    bpy.ops.object.mode_set(mode='OBJECT')
    for obj in objects:
        name=obj.name.split('.')[0];binding='Chest'
        if name=='Pelvis' or name.startswith(('Utility belt','Belt pouch')):binding='Hips'
        elif name.startswith('Abdominal'):binding='Spine'
        elif name.startswith('Neck'):binding='Neck'
        elif name.startswith(('Helmet','Visor','Respirator')):binding='Head'
        elif name in ('Rifle','Barrel'):binding='HandR'
        else:
            for side in ('L','R'):
                if name.endswith(side):
                    if name.startswith(('Thigh',)):binding='Thigh'+side
                    elif name.startswith(('Knee','Leg','Shin')):binding='Shin'+side
                    elif name.startswith(('Boot','Toe')):binding='Foot'+side
                    elif name.startswith(('Shoulder','Arm')):binding='UpperArm'+side
                    elif name.startswith(('Elbow','Forearm')):binding='Forearm'+side
                    elif name.startswith(('Wrist','Hand')):binding='Hand'+side
        groups={}
        def weight(index,bone_name,value):
            key='Rig_'+bone_name
            if key not in groups:groups[key]=obj.vertex_groups.new(name=key)
            if value>0:groups[key].add([index],value,'REPLACE')
        for vertex in obj.data.vertices:
            z=(obj.matrix_world@vertex.co).z
            if name=='Chest':
                # Cloth deforms between spine and chest instead of rotating as a plate.
                blend=max(0,min(1,(z-1.20)/.23))
                weight(vertex.index,'Spine',1-blend);weight(vertex.index,'Chest',blend)
            else:weight(vertex.index,binding,1)
        modifier=obj.modifiers.new('Soldier skin','ARMATURE');modifier.object=rig
        world=obj.matrix_world.copy();obj.parent=rig;obj.matrix_world=world
    animate_soldier(rig)
    return rig

def animate_soldier(rig):
    scene=bpy.context.scene;scene.render.fps=24;scene.frame_start=1;scene.frame_end=33
    rig.animation_data_create()
    for name in ('Soldier_Idle','Soldier_Walk','Soldier_Run','Soldier_StrafeLeft','Soldier_StrafeRight','Soldier_Backward'):
        action=bpy.data.actions.new(name);action.use_fake_user=True
        rig.animation_data.action=action
        running=name=='Soldier_Run';length=24 if running else 32
        for frame in range(1,length+2,2):
            phase=(frame-1)*2*math.pi/length
            if name=='Soldier_Backward':phase=-phase
            for bone in rig.pose.bones:
                bone.rotation_mode='XYZ';bone.rotation_euler=(0,0,0);bone.location=(0,0,0)
            if name in ('Soldier_Walk','Soldier_Run','Soldier_Backward'):
                rig.pose.bones['Rig_Hips'].location.y=(.028 if running else .016)*(1-math.cos(phase*2))
                rig.pose.bones['Rig_Spine'].rotation_euler.x=math.radians(9 if running else 0)
                for side,sign in [('L',1),('R',-1)]:
                    swing=math.sin(phase)*sign
                    rig.pose.bones['Rig_Thigh'+side].rotation_euler.x=math.radians(40 if running else 24)*swing
                    rig.pose.bones['Rig_Shin'+side].rotation_euler.x=math.radians(65 if running else 34)*max(0,-swing)
                    rig.pose.bones['Rig_Foot'+side].rotation_euler.x=-math.radians(20 if running else 12)*max(0,-swing)
                    rig.pose.bones['Rig_UpperArm'+side].rotation_euler.x=-math.radians(28 if running else 15)*swing
                    rig.pose.bones['Rig_Forearm'+side].rotation_euler.x=-math.radians(50 if running else 8)
                rig.pose.bones['Rig_Chest'].rotation_euler.y=math.radians(3)*math.sin(phase)
            if name in ('Soldier_StrafeLeft','Soldier_StrafeRight'):
                direction=-1 if name=='Soldier_StrafeLeft' else 1
                rig.pose.bones['Rig_Hips'].location.y=.012*(1-math.cos(phase*2))
                rig.pose.bones['Rig_Spine'].rotation_euler.z=math.radians(3)*direction
                for side,sign in [('L',1),('R',-1)]:
                    swing=math.sin(phase)*sign
                    lateral=math.radians(15)*direction*swing
                    rig.pose.bones['Rig_Thigh'+side].rotation_euler.z=lateral
                    rig.pose.bones['Rig_Shin'+side].rotation_euler.x=math.radians(26)*max(0,-swing)
                    rig.pose.bones['Rig_Foot'+side].rotation_euler.z=-lateral
                    rig.pose.bones['Rig_Foot'+side].rotation_euler.x=-math.radians(10)*max(0,-swing)
                    rig.pose.bones['Rig_UpperArm'+side].rotation_euler.z=-math.radians(5)*direction*swing
            for bone in rig.pose.bones:
                bone.keyframe_insert(data_path='rotation_euler',frame=frame,group=bone.name)
                bone.keyframe_insert(data_path='location',frame=frame,group=bone.name)
    rig.animation_data.action=bpy.data.actions['Soldier_Idle'];scene.frame_set(1)
def tower():
    box('Plinth',(0,0,1),(10,9,2),'Concrete',.15)
    box('Core',(0,0,15),(7,6,28),'Graphite',.14)
    box('Crown',(0,0,30),(5,4.8,3),'Steel',.1)
    for x in [-3.7,3.7]:
        for y in [-3.2,3.2]:box('Buttress',(x,y,14),(1,.9,28),'Steel',.08)
    for z in range(4,30,3):
        box('Floor belt',(0,0,z),(7.7,6.7,.22),'Steel')
        for x in [-2.6,-1.3,0,1.3,2.6]:
            for y in [-3.04,3.04]:box('Window',(x,y,z+1),(.35,.07,1.3),'Amber' if (z+int(x*10))%4==0 else 'Cyan',.01)
    for x in [-2,2]:
        cyl('Antenna',(x,0,34),.09,8); box('Spire',(x,0,31.8),(.65,.65,3))
    box('Facade light',(0,-3.5,22),(1.5,.14,9),'Cyan')
def stepped_tower():
    # Offset terraces break the repeated central-spire silhouette.
    box('Service podium',(0,0,2),(12,10,4),'Concrete',.18)
    for x,y,z,w,d,h in [(0,0,11,9,8,18),(-1,.5,23,7,6.5,10),(-1.7,1,31,4.8,4.6,6)]:
        box('Setback volume',(x,y,z),(w,d,h),'Graphite',.13)
        box('Terrace cap',(x,y,z+h/2),(w+.55,d+.55,.4),'Steel',.05)
        for edge in [-1,1]:
            box('Vertical frame',(x+edge*(w/2-.3),y-d/2-.08,z),(.28,.18,h),'Steel')
        for floor in range(int(z-h/2)+2,int(z+h/2),3):
            for face in [-1,1]:
                box('Recessed glazing',(x,y+face*(d/2+.015),floor),(w-1.2,.04,.85),'Dark',0)
                for bay in range(-2,3):
                    if (floor+bay)%3==0:
                        box('Occupied office',(x+bay*(w-1.8)/5,y+face*(d/2+.04),floor),(.48,.035,.64),'Amber',0)
    for x in [1.8,3.1]:
        cyl('Rooftop exchanger',(x,-1,21),.48,2,'Steel')
        cyl('Exchanger cap',(x,-1,22.03),.56,.12,'Dark')
    box('Communication mast',(-1.7,1,36),(.16,.16,5),'Steel',0)
    box('Roof beacon',(-1.7,1,38.55),(.22,.22,.14),'Amber',0)

def drum_tower():
    # Broad industrial drums echo the rounded habitat kit at skyline scale.
    cyl('Foundation',(0,0,1),6.2,2,'Concrete')
    cyl('Lower drum',(0,0,9),5.3,16,'Graphite')
    cyl('Upper drum',(0,0,21),4.1,8,'Steel')
    for z,radius in [(3,5.5),(8,5.5),(13,5.5),(17,5.6),(21,4.3),(25,4.4)]:
        cyl('Structural collar',(0,0,z),radius,.38,'Concrete')
    for i in range(12):
        angle=i*math.tau/12
        x,y=5.28*math.cos(angle),5.28*math.sin(angle)
        rib=box('Drum rib',(x,y,9),(.24,.3,15),'Steel');rib.rotation_euler.z=angle
        for z in [5.5,10.5,15.5]:
            window=box('Drum glazing',(x*1.016,y*1.016,z),(.055,.95,.7),'Cyan' if i%4==0 else 'Dark',0)
            window.rotation_euler.z=angle
    box('Roof plant',(0,0,26.2),(4.6,3.4,2.2),'Dark',.08)
    for x in [-1.5,0,1.5]:cyl('Roof exhaust',(x,0,28),.3,2,'Steel')

def terminal():
    # AO Universe / knowledge/386/mission_terminals.jpg, left (individual) booth.
    # Authored geometry; dimensions and unseen rear housing are estimates.
    box('Mission foot',(0,0,.10),(.50,.62,.20),'Dark',.025)
    box('Mission pedestal',(0,.06,.47),(.32,.38,.66),'Graphite',.025)
    for z in [.24,.43,.62]:
        box('Pedestal panel',(0,-.142,z),(.25,.024,.13),'Steel',.008)
    box('Console lower shelf',(0,0,.85),(.79,.61,.15),'Graphite',.022)
    box('Console housing',(0,.07,1.48),(.77,.43,1.13),'Graphite',.032)
    box('Screen bezel',(0,-.174,1.48),(.66,.095,1.03),'Steel',.020)
    box('Dark screen',(0,-.231,1.48),(.54,.022,.88),'Dark',.009)
    for x in [-.29,.29]:
        box('Screen illuminated edge',(x,-.250,1.48),(.012,.008,.91),'Cyan',0)
    for z in [1.03,1.93]:
        box('Screen illuminated edge',(0,-.250,z),(.59,.008,.012),'Cyan',0)
    box('Console hood',(0,.015,2.075),(.86,.61,.16),'Dark',.025)
    box('Individual indicator',(0,-.304,2.075),(.33,.027,.14),'Graphite',.008)
    # Single-person glyph distinguishes the individual terminal from team booths.
    sphere('Individual head',(0,-.328,2.107),(.025,.012,.025),'Cyan')
    box('Individual body',(0,-.329,2.052),(.026,.012,.063),'Cyan',.005)
    box('Individual shoulders',(0,-.329,2.073),(.068,.012,.018),'Cyan',.004)
    box('Sign support',(0,.07,2.23),(.12,.15,.22),'Steel',.010)
    box('M sign surround',(0,.02,2.48),(.39,.18,.47),'Steel',.018)
    box('M sign face',(0,-.081,2.48),(.31,.018,.39),'Ivory',.006)
    for name,body,z,size,mat in [
        ('Mission identifier','M',2.48,.33,'Dark'),
        ('Mission display lettering','MISSION\nCENTRAL',1.63,.105,'Cyan'),
        ('Mission display emblem','M',1.28,.28,'Cyan')]:
        bpy.ops.object.text_add(location=(0,-.099 if z>2.2 else -.252,z),rotation=(math.pi/2,0,0))
        text=bpy.context.object;text.name=name;text.data.body=body
        text.data.align_x='CENTER';text.data.align_y='CENTER';text.data.size=size
        text.data.space_line=.95;text.data.extrude=.0005
        text.data.materials.append(materials[mat]);bpy.ops.object.convert(target='MESH')
    for x in [-.22,.22]:
        box('Console key',(x,-.27,.96),(.10,.10,.025),'Steel',.005)
def crate():
    box('Cargo',(0,0,.65),(1.7,1.2,1.3),'Graphite',.12)
    for x in [-.63,.63]:box('Reinforcement',(x,0,.65),(.12,1.25,1.36),'Steel')
    box('ID',(0,-.615,.85),(.5,.025,.19),'Amber')
def drone():
    sphere('Core',(0,0,0),(.65,.45,.43),'Graphite')
    box('Upper carapace',(0,.025,.31),(.90,.58,.18),'Steel',.085)
    box('Lower keel',(0,.02,-.30),(.65,.48,.16),'Steel',.065)
    for x in [-.42,.42]:
        plate=box('Cheek armor',(x,-.24,.04),(.18,.25,.34),'Steel',.045)
        plate.rotation_euler.z=math.radians(-18 if x<0 else 18)
    optic=cyl('Optic housing',(0,-.44,0),.265,.19,'Dark')
    optic.rotation_euler.x=math.pi/2
    rim=cyl('Optic bezel',(0,-.545,0),.225,.025,'Steel')
    rim.rotation_euler.x=math.pi/2
    sphere('Optic',(0,-.568,0),(.178,.045,.178),'Amber')
    for x in [-.28,.28]:
        sphere('Rangefinder',(x,-.435,.19),(.055,.025,.045),'Cyan')
    for y in [-.12,0,.12]:
        box('Top cooling slot',(0,y,.408),(.50,.036,.012),'Dark',.006)
    for x in [-.8,.8]:
        box('Wing spar',(x,0,0),(.75,.24,.10),'Dark',.035)
        box('Wing armor',(x,.01,.095),(.67,.36,.09),'Steel',.028)
        cyl('Engine',(x,0,-.1),.24,.35,'Dark')
        for z in [-.23,-.08]:cyl('Engine band',(x,0,z),.252,.055,'Steel')
        cyl('Thruster shroud',(x,0,-.295),.205,.055,'Steel')
        cyl('Thruster',(x,0,-.326),.15,.012,'Cyan')
        for y in [-.105,.105]:cyl('Wing fastener',(x,y,.145),.026,.015,'Dark')
    box('Sensor base',(0,.1,.47),(.22,.20,.10),'Dark',.025)
    box('Sensor',(0,.1,.56),(.075,.10,.16),'Steel',.016)
    box('Rear status',(0,.442,.06),(.16,.014,.055),'Cyan',.008)
def glove(side,x):
    before=set(bpy.data.objects)
    box('Glove palm',(x,-.05,.808),(.105,.088,.10),'Fabric',.022)
    box('Glove back plate',(x,-.002,.816),(.085,.018,.061),'Armor',.009)
    # Four curled fingers retain small gaps and a separate thumb silhouette.
    for index,offset in enumerate([-.038,-.013,.013,.038]):
        length=.051 if index in (1,2) else .044
        box('Finger proximal',(x+offset,-.057,.754),(.021,.037,length),'Dark',.009)
        segment=box('Finger curled tip',(x+offset,-.083,.745),(.021,.036,.031),'Fabric',.009)
        segment.rotation_euler.x=math.radians(-28)
        box('Glove knuckle',(x+offset,-.029,.781),(.020,.021,.023),'Armor',.006)
    inward=1 if side=='L' else -1
    thumb=box('Thumb base',(x+inward*.059,-.065,.817),(.032,.047,.044),'Dark',.012)
    thumb.rotation_euler.y=math.radians(inward*25)
    thumb=box('Thumb tip',(x+inward*.055,-.093,.794),(.029,.035,.031),'Fabric',.011)
    thumb.rotation_euler.y=math.radians(inward*35)
    # Keep a small renderer count and the existing rigid HandL/HandR binding.
    pieces=set(bpy.data.objects)-before
    if side=='R':
        # Palm beside the pistol grip, fingers stacked vertically around it.
        # The weapon remains in its reference-derived position.
        pivot=Vector((x,-.05,.808))
        placement=(Matrix.Translation(Vector((.39,-.115,.84))) @
                   Matrix.Rotation(math.pi/2,4,'Y') @ Matrix.Translation(-pivot))
        for obj in pieces:obj.matrix_world=placement@obj.matrix_world
    groups={material:[o for o in pieces if o.data.materials[0].name==material]
            for material in ['Fabric','Armor','Dark']}
    for material,group in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in group:obj.select_set(True)
        bpy.context.view_layer.objects.active=group[0]
        bpy.ops.object.convert(target='MESH');bpy.ops.object.join()
        bpy.context.object.name='Hand '+material+side

def soldier():
    # Original modern armor study based on the supplied mood image, not an AO item replica.
    before=set(bpy.data.objects)
    body_form('Pelvis',0,[(.85,.13,.105,0),(.94,.19,.145,.015),(1.03,.175,.12,0)])
    body_form('Chest',0,[(1.01,.17,.12,0),(1.12,.155,.115,0),(1.30,.205,.14,0),
                         (1.44,.245,.165,0),(1.53,.23,.14,.01),(1.61,.10,.09,0)])
    for z,width in [(1.12,.14),(1.20,.16),(1.28,.18)]:
        armor_panel('Abdominal segment',[(-width,z+.035),(-width+.025,z-.035),(width-.025,z-.035),(width,z+.035)],-.16)
    for x in [-.125,.125]:
        sign=-1 if x<0 else 1
        outline=[(sign*a,z) for a,z in [(.02,1.49),(.20,1.515),(.245,1.45),(.19,1.34),(.045,1.32)]]
        if sign>0:outline.reverse()
        armor_panel('Pectoral armor',outline,-.195,.055)
        box('Harness',(x,.165,1.43),(.058,.055,.39),'Dark',.018)
    cyl('Neck seal',(0,0,1.65),.095,.15,'Dark')
    body_form('Helmet',0,[(1.675,.085,.085,.015),(1.72,.14,.13,.005),
                         (1.84,.158,.151,0),(1.94,.14,.135,.006),(2.005,.085,.085,.016)],'Armor')
    box('Helmet crown',(0,.015,1.982),(.07,.23,.026),'Steel',.009)
    box('Helmet rear seal',(0,.14,1.74),(.20,.045,.055),'Dark',.012)
    for x in [-.07,0,.07]:
        box('Helmet rear vent',(x,.151,1.81),(.035,.018,.07),'Dark',.006)
    box('Helmet brow',(0,-.115,1.89),(.29,.09,.08),'Armor',.025)
    box('Visor',(0,-.154,1.825),(.245,.035,.064),'Dark',.016)
    box('Visor lens',(0,-.175,1.832),(.19,.012,.022),'Cyan',.008)
    box('Respirator',(0,-.146,1.735),(.135,.08,.09),'Armor',.018)
    for x in [-.154,.154]:
        box('Helmet comms',(x,0,1.81),(.04,.105,.12),'Armor',.018)
    box('Backpack',(0,.225,1.38),(.32,.21,.38),'Fabric',.05)
    box('Backpack spine',(0,.34,1.38),(.10,.035,.28),'Armor',.018)
    box('Backlight',(0,.362,1.40),(.025,.01,.10),'Cyan',.006)
    box('Pack battery',(0,.27,1.12),(.29,.16,.10),'Armor',.025)
    for x in [-.17,.17]:
        box('Pack side pocket',(x,.245,1.34),(.075,.15,.23),'Dark',.025)
    for x in [-.12,.12]:
        # Separate plates frame the pack; their bevels remain visible from the play camera.
        plate=box('Scapular armor',(x,.19,1.56),(.18,.085,.15),'Armor',.023)
        plate.rotation_euler.y=math.radians(-12 if x<0 else 12)
    for z in [1.30,1.39,1.48]:
        box('Pack armor segment',(0,.347,z),(.25,.035,.045),'Armor',.008)
    for side,x in [('L',-.30),('R',.30)]:
        body_form('Shoulder'+side,x,[(1.40,.085,.115,.005),(1.47,.13,.16,0),
                                   (1.55,.12,.15,0),(1.595,.075,.105,.005)],'Armor')
        body_form('Shoulder cap'+side,x,[(1.575,.092,.12,.005),(1.605,.062,.085,.005)],'Steel')
        outer=x+(-.10 if x<0 else .10)
        box('Shoulder skirt'+side,(outer,.005,1.44),(.035,.255,.12),'Armor',.014)
        body_form('Arm'+side,x,[(1.08,.061,.070,0),(1.20,.087,.092,0),(1.34,.09,.095,0),(1.48,.07,.075,0)])
        sphere('Elbow'+side,(x,-.005,1.09),(.078,.085,.078),'Dark')
        body_form('Forearm'+side,x,[(.87,.056,.065,-.04),(.96,.075,.087,-.035),(1.065,.077,.080,-.015)],'Armor')
        box('Wrist seal'+side,(x,-.05,.86),(.12,.15,.06),'Dark',.018)
        glove(side,x)
        box('Shoulder inset'+side,(x,-.166,1.50),(.105,.012,.047),'Fabric',.01)
    box('Utility belt',(0,0,1.01),(.40,.32,.065),'Armor',.015)
    for x in [-.145,.145]:
        box('Belt pouch',(x,-.18,.955),(.105,.105,.15),'Fabric',.02)
    for side,x in [('L',-.115),('R',.115)]:
        body_form('Thigh'+side,x,[(.55,.076,.085,0),(.65,.09,.11,.015),(.80,.113,.135,.02),(.94,.108,.125,.015)])
        armor_panel('Thigh plate'+side,[(x-.065,.88),(x-.076,.80),(x-.048,.65),
                    (x+.048,.65),(x+.076,.80),(x+.065,.88)],-.135,.04)
        sphere('Knee joint'+side,(x,0,.535),(.09,.105,.09),'Dark')
        box('Knee'+side,(x,-.12,.54),(.17,.11,.135),'Armor',.032)
        body_form('Leg'+side,x,[(.20,.062,.07,0),(.29,.067,.083,.01),(.40,.088,.113,.03),(.49,.074,.09,.015)])
        box('Shin armor'+side,(x,-.10,.365),(.11,.055,.21),'Armor',.02)
        body_form('Boot'+side,x,[(.05,.094,.18,-.065),(.10,.098,.175,-.07),
                               (.16,.083,.13,-.035),(.23,.067,.082,0),(.27,.064,.075,0)],'Dark')
        body_form('Boot sole'+side,x,[(.018,.089,.173,-.065),(.035,.10,.185,-.065),
                                    (.065,.10,.183,-.065)],'Fabric')
        body_form('Toe cap'+side,x,[(.07,.084,.078,-.164),(.11,.09,.077,-.165),
                                  (.145,.067,.055,-.155)],'Armor')
    solar_rifle()
    # Stable surface coordinates: material detail follows each skinned mesh.
    for obj in set(bpy.data.objects)-before:
        if obj.type!='MESH':continue
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.025)
        bpy.ops.object.mode_set(mode='OBJECT')
    rig=rig_soldier([obj for obj in set(bpy.data.objects)-before if obj.type=='MESH'])
    marker=bpy.data.objects.new('RifleMuzzle',None);bpy.context.collection.objects.link(marker)
    marker.parent=rig;marker.parent_type='BONE';marker.parent_bone='Rig_HandR'
    bpy.context.view_layer.update()
    marker.matrix_world=Matrix.Translation((.34,-1.019,.95))
    support=bpy.data.objects.new('RifleSupportWrist',None);bpy.context.collection.objects.link(support)
    support.parent=rig;support.parent_type='BONE';support.parent_bone='Rig_HandR'
    bpy.context.view_layer.update()
    support.matrix_world=Matrix.Translation((.34,-.35,.93))

def solar_rifle():
    # Authored Blender reconstruction guided by the local mesh-15839 reference render.
    old=set(bpy.data.objects)
    box('Receiver',(.34,-.26,.925),(.085,.32,.10),'WeaponMetal',.012)
    box('Receiver cover',(.34,-.26,.983),(.065,.29,.025),'WeaponMetal',.006)
    box('Stock rod',(.34,.055,.965),(.045,.35,.040),'WeaponMetal',.006)
    box('Butt shoulder stop',(.34,.225,1.00),(.057,.027,.15),'Dark',.007)
    grip=box('Grip',(.34,-.135,.84),(.06,.07,.13),'WeaponGrip',.012)
    grip.rotation_euler.x=math.radians(-15)
    box('Trigger guard',(.34,-.205,.845),(.038,.11,.015),'WeaponEdge',.004)
    box('Guard front',(.34,-.252,.871),(.038,.018,.06),'WeaponEdge',.003)
    box('Lower barrel support',(.34,-.57,.856),(.038,.36,.028),'Dark',.005)
    brace=box('Support tip',(.34,-.75,.874),(.038,.028,.065),'WeaponMetal',.005)
    brace.rotation_euler.x=math.radians(25)
    box('Action bolt',(.393,-.28,.943),(.025,.065,.017),'Dark',.004)
    parts=list(set(bpy.data.objects)-old)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.convert(target='MESH');bpy.ops.object.join();bpy.context.object.name='Rifle'
    old=set(bpy.data.objects)
    def subtract(target,cutter):
        bpy.context.view_layer.objects.active=target
        modifier=target.modifiers.new('Machined opening','BOOLEAN')
        modifier.operation='DIFFERENCE';modifier.solver='EXACT';modifier.object=cutter
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        bpy.data.objects.remove(cutter,do_unlink=True)
    for y,radius,length in [(-.535,.043,.23),(-.75,.039,.20),(-.92,.030,.14)]:
        sleeve=cyl('Vented barrel sleeve',(.34,y,.95),radius,length,'WeaponMetal');sleeve.rotation_euler.x=math.pi/2
        inner=cyl('Temporary sleeve bore',(.34,y,.95),radius-.004,length+.02,'Dark');inner.rotation_euler.x=math.pi/2
        subtract(sleeve,inner)
        for j in range(4):
            yy=y+(j-1.5)*length/5
            cutter=box('Temporary vent cutter',(.34,yy,.945),(.14,length/7,.018),'Dark',.004)
            subtract(sleeve,cutter)
            # The authored slot must pass through both sides of this hollow sleeve.
            bpy.context.view_layer.update()
            origin=sleeve.matrix_world.inverted()@Vector((.14,yy,.945))
            direction=sleeve.matrix_world.to_3x3().inverted()@Vector((1,0,0))
            if sleeve.ray_cast(origin,direction)[0]:raise ValueError('Blocked rifle ventilation opening')
        collar=cyl('Sleeve collar',(.34,y+length/2,.95),radius+.006,.035,'WeaponEdge');collar.rotation_euler.x=math.pi/2
    core=cyl('Inner barrel',(.34,-.70,.95),.014,.59,'Dark');core.rotation_euler.x=math.pi/2
    muzzle=cyl('Muzzle',(.34,-1.005,.95),.028,.025,'Dark');muzzle.rotation_euler.x=math.pi/2
    parts=list(set(bpy.data.objects)-old)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.convert(target='MESH');bpy.ops.object.join();bpy.context.object.name='Barrel'

def lamp():
    box('Base',(0,0,.15),(.8,.8,.3),'Concrete')
    box('Mast',(0,0,3),(.15,.2,6),'Graphite')
    box('Arm',(0,-.7,5.9),(.15,1.6,.15),'Steel')
    box('Light',(0,-1.25,5.8),(.45,.65,.07),'Cyan')
def shuttle():
    sphere('Hull',(0,0,0),(1.8,4,.7),'Graphite')
    box('Canopy',(0,-1,.5),(1.6,1.8,.4),'Dark',.18)
    for x in [-2,2]:
        box('Wing',(x,.6,-.1),(2,2,.16),'Steel',.08)
        sphere('Nacelle',(x,1,0),(.5,1.8,.5),'Graphite')
        sphere('Drive',(x,2.5,0),(.28,.25,.28),'Cyan')
def whompah():
    box('Transit base',(0,0,.15),(4.8,3.2,.3),'Concrete',.1)
    box('Rear housing',(0,.8,2.4),(4,1.4,4.5),'Concrete',.22)
    box('Door recess',(0,-.05,2.1),(2.15,.12,3.7),'Dark',.15)
    for x in [-1.55,1.55]:
        box('Portal column',(x,-.4,2.3),(.8,1,4.5),'Steel',.2)
        box('Portal rail',(x*.76,-.96,2.3),(.09,.055,3.6),'Cyan',.03)
    box('Header',(0,-.4,4.5),(4,.95,.6),'Steel',.16)
    box('Name plate',(0,-.925,4.5),(2.7,.03,.4),'Dark',.02)
    box('Threshold',(0,-.8,.35),(2.4,.8,.18),'Steel',.04)
    for z in [.8,1.5,2.2,2.9,3.6]:box('Transit field',(0,-.14,z),(2.05,.025,.035),'Cyan',0)
    box('Control',(2,-.4,1.3),(.35,.7,.6),'Graphite',.04)
    box('Control screen',(2,-.77,1.4),(.23,.03,.26),'Amber',.01)
def tapered_housing(name,pos,bottom,top,height,mat):
    vertices=[]
    for z,size in [(-height/2,bottom),(height/2,top)]:
        vertices.extend((pos[0]+x*size[0]/2,pos[1]+y*size[1]/2,pos[2]+z)
                        for x,y in [(-1,-1),(1,-1),(1,1),(-1,1)])
    faces=[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    mesh.materials.append(materials[mat])
    bevel=obj.modifiers.new('Housing edges','BEVEL');bevel.width=.012;bevel.segments=2
    obj.modifiers.new('Housing normals','WEIGHTED_NORMAL')
    return obj

def grid_terminal():
    # Visual reference: AO Universe / knowledge/497/2_grid_entrance.jpg.
    # Original authored geometry; proportions estimated from the screenshot.
    tapered_housing('Flared foot',(0,0,.12),(.76,.69),(.52,.47),.24,'Dark')
    box('Foot shoulder',(0,0,.29),(.61,.55,.17),'Graphite',.035)
    box('Upright housing',(0,0,1.03),(.59,.48,1.38),'Graphite',.045)
    box('Front service panel',(0,-.25,1.04),(.43,.035,1.12),'Dark',.025)
    box('Vertical panel seam',(.08,-.272,1.04),(.016,.014,1.09),'Steel',.002)
    for x in [-.27,.27]:
        box('Side rail',(x,-.24,1.03),(.035,.052,1.23),'Steel',.012)
    box('Service collar',(0,0,1.69),(.66,.53,.13),'Dark',.025)
    for x in [-.23,-.115,0,.115,.23]:
        box('Collar slot',(x,-.28,1.69),(.075,.02,.045),'Steel',.003)
    box('Interface neck',(0,0,1.87),(.59,.47,.25),'Graphite',.035)
    box('Status inset',(0,-.25,1.9),(.32,.028,.10),'Dark',.009)
    for i in range(3):
        box('Status pixel',(-.075+i*.058,-.269,1.905),(.032,.014,.017),'Cyan',.002)
    box('Monitor chassis',(0,0,2.25),(.84,.66,.55),'Graphite',.065)
    box('Monitor bezel',(0,-.34,2.25),(.66,.08,.41),'Steel',.032)
    box('Screen recess',(0,-.387,2.25),(.51,.025,.30),'Dark',.018)
    # The source screen is dark; avoid replacing it with a large neon panel.
    box('Inner display',(0,-.404,2.25),(.39,.012,.22),'Graphite',.009)
    for x in [-.35,.35]:
        box('Monitor side ridge',(x,-.21,2.25),(.052,.30,.43),'Steel',.015)
    box('Beacon sill',(0,0,2.57),(.80,.60,.10),'Dark',.028)
    tapered_housing('Blue access beacon',(0,0,2.69),(.65,.46),(.51,.34),.18,'Cyan')
    box('Beacon top rail',(0,.20,2.78),(.67,.045,.035),'Graphite',.009)
    box('Sign bracket',(-.53,0,2.24),(.34,.24,.22),'Steel',.025)
    box('Cantilever sign',(-1.08,-.03,2.24),(1.33,.43,.58),'Dark',.045)
    box('Sign face',(-1.08,-.253,2.24),(1.20,.02,.49),'Graphite',.012)
    bpy.ops.object.text_add(location=(-1.08,-.272,2.24),rotation=(math.pi/2,0,0))
    text=bpy.context.object;text.name='Grid access lettering'
    text.data.body='GRID\nACCESS';text.data.align_x='CENTER';text.data.align_y='CENTER'
    text.data.size=.215;text.data.space_line=.85;text.data.extrude=.001
    text.data.materials.append(materials['Cyan']);bpy.ops.object.convert(target='MESH')
    for z in [.52,1.51]:
        for x in [-.19,.19]:
            screw=cyl('Fastener',(x,-.285,z),.018,.018,'Steel')
            screw.rotation_euler.x=math.pi/2

def dish():
    box('Station',(0,0,1),(7,7,2),'Concrete',.2)
    cyl('Mount',(0,0,4),1.2,6,'Steel')
    verts=[];faces=[];rings=8;segments=48
    for i in range(rings+1):
        r=8*i/rings
        for j in range(segments):
            a=j*math.tau/segments;verts.append((r*math.cos(a),r*math.sin(a),7+r*r/18))
    for i in range(rings):
        for j in range(segments):
            k=i*segments+j;n=i*segments+(j+1)%segments;faces.append((k,n,n+segments,k+segments))
    mesh=bpy.data.meshes.new('Parabolic reflector');mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new('Subspace dish',mesh);bpy.context.collection.objects.link(obj);obj.data.materials.append(materials['Ivory'])
    solid=obj.modifiers.new('Reflector thickness','SOLIDIFY');solid.thickness=.15
    for x,y in [(4,0),(-4,0),(0,4),(0,-4)]:
        o=box('Feed strut',(x/2,y/2,10.1),(.15,.15,6),'Steel',.03);direction=Vector((0,0,12))-Vector((x,y,8));o.rotation_euler=direction.to_track_quat('Z','Y').to_euler()
    sphere('Receiver',(0,0,12),(.6,.6,.35),'Dark');sphere('Receiver light',(0,0,12.3),(.15,.15,.12),'Cyan')
def habitat():
    box('Foundation',(0,0,.5),(10,8,1),'Concrete',.15)
    box('Habitat',(0,0,3),(9,7,5),'Concrete',.5)
    box('Upper tier',(0,.7,5.8),(7,5,1.8),'Steel',.4)
    for x in [-3,-1.5,0,1.5,3]:
        box('Window recess',(x,-3.51,3.8),(1,.08,1.1),'Dark',.04)
        box('Warm window',(x,-3.57,3.8),(.76,.025,.7),'Amber',.02)
    box('Service entrance',(0,-3.52,1.8),(1.6,.1,2.5),'Dark',.1)
    for x in [-4.7,4.7]:box('Rib',(x,0,2.6),(.32,7.5,4.8),'Steel',.1)
    for z in [1,2,3,4]:box('Vent',(4.55,-1,z),(.025,2,.08),'Dark',0)
def utility_habitat():
    # Authored Rubi-Ka architecture study, not a measured original city asset.
    cyl('Circular foundation',(0,0,.45),6.3,.9,'Concrete')
    cyl('Residential drum',(0,0,5.2),5.5,9.4,'Concrete')
    for z in [1,4.2,7.4,10]:
        cyl('Armored ring',(0,0,z),5.8,.32,'Steel')
    for angle in range(0,360,45):
        a=math.radians(angle)
        for z in [2.6,5.8,8.8]:
            x,y=5.45*math.sin(a),-5.45*math.cos(a)
            recess=box('Recessed window',(x,y,z),(1.65,.16,.95),'Dark',.03)
            recess.rotation_euler.z=a
            light=box('Window strip',(x*1.018,y*1.018,z),(1.35,.035,.34),'Amber' if angle%90==0 else 'Cyan',.01)
            light.rotation_euler.z=a
        rib=box('Vertical service rib',(5.65*math.sin(a),-5.65*math.cos(a),5.2),(.28,.3,9.5),'Steel',.04)
        rib.rotation_euler.z=a
    cyl('Roof machinery',(0,0,10.9),3.5,1.5,'Graphite')
    cyl('Roof cap',(0,0,11.7),3.8,.22,'Steel')
    for x in [-1.6,1.6]:
        cyl('Pressure vessel',(x,.4,12.4),.65,1.3,'Steel')
        sphere('Vessel cap',(x,.4,13.05),(.65,.65,.25),'Steel')
    cyl('Communications mast',(-2,1,14),.08,5,'Steel')
    box('Entry recess',(0,-5.48,1.5),(2.4,.2,2.6),'Dark',.12)
    box('Entry canopy',(0,-6,3),(3.3,1.7,.3),'Steel',.08)
    for x in [-1.3,1.3]:box('Entry lamp',(x,-5.7,1.8),(.08,.12,1.6),'Cyan',.01)

def garden_island():
    # Authored street furniture; kept outside the central movement/mission corridor.
    box('Planter foundation',(0,0,.12),(4.25,1.65,.24),'Dark',.08)
    box('Soil',(0,0,.48),(3.92,1.30,.48),'Soil',.05)
    for y in [-.73,.73]:box('Planter wall',(0,y,.42),(4.15,.20,.66),'Concrete',.075)
    for x in [-1.99,1.99]:box('Planter end',(x,0,.42),(.20,1.48,.66),'Concrete',.075)
    for y in [-.75,.75]:box('Planter lip',(0,y,.76),(4.18,.22,.10),'Steel',.035)
    for x in [-1.8,1.8]:box('Maintenance inset',(x,-.84,.42),(.18,.015,.22),'Dark',.006)
    box('Planter ID',(-1.25,-.85,.47),(.52,.018,.045),'Ivory',.004)
    rng=random.Random(184)
    for clump in range(18):
        center=Vector((rng.uniform(-1.8,1.8),rng.uniform(-.48,.48),.74))
        for leaf in range(11):
            angle=rng.random()*math.tau;direction=Vector((math.cos(angle),math.sin(angle),0));side=Vector((-direction.y,direction.x,0))
            height=rng.uniform(.3,.92);reach=rng.uniform(.18,.45);verts=[]
            for t,width in [(0,.012),(.35,.035),(.72,.024),(1,.001)]:
                middle=center+direction*(reach*t*t)+Vector((0,0,height*t))
                verts.extend([middle-side*width,middle+side*width])
            faces=[]
            for k in range(3):
                a=k*2;faces.extend([(a,a+1,a+3,a+2),(a+2,a+3,a+1,a)])
            mesh=bpy.data.meshes.new('Leaf');mesh.from_pydata(verts,[],faces);mesh.update()
            obj=bpy.data.objects.new('Native grass',mesh);bpy.context.collection.objects.link(obj)
            mesh.materials.append(materials['FoliageLight' if leaf%4==0 else 'Foliage'])

def transit_bench():
    for x in [-1.0,1.0]:
        box('Bench foot',(x,0,.09),(.24,.75,.18),'Dark',.045)
        box('Bench support',(x,0,.30),(.12,.45,.45),'Steel',.025)
    for x in [-.88,0,.88]:
        box('Seat panel',(x,-.05,.53),(.81,.68,.12),'Steel',.045)
        back=box('Seat back',(x,.29,.85),(.81,.10,.54),'Graphite',.04);back.rotation_euler.x=math.radians(-12)
        box('Back inset',(x,.22,.84),(.64,.025,.30),'Steel',.025)
    for x in [-1.32,1.32]:
        box('Arm support',(x,.05,.67),(.065,.09,.4),'Steel',.018)
        box('Armrest',(x,-.04,.87),(.10,.57,.07),'Dark',.025)

for name,fn in [('Tower',tower),('SteppedTower',stepped_tower),('DrumTower',drum_tower),('Terminal',terminal),('Cargo',crate),('Drone',drone),('Soldier',soldier),('Streetlight',lamp),('Shuttle',shuttle),('Whompah',whompah),('GridTerminal',grid_terminal),('SubspaceDish',dish),('Habitat',habitat),('UtilityHabitat',utility_habitat),('GardenIsland',garden_island),('TransitBench',transit_bench)]:export(name,fn,name!='Soldier')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'Art','Blender','DistrictKit.blend'))
print('ART_EXPORT_OK: 13 models and editable Blender source')

