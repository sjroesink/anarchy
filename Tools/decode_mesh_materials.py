"""Resolve observed material texture references; shader semantics remain opaque."""
import struct

def resolve_materials(decoded):
    objects=decoded['objects'][1:];symbols=decoded['symbols']
    def fields(identity,expected_class):
        if not 0<=identity<len(objects):raise ValueError('Invalid material graph reference')
        result=objects[identity]
        cls=integer(result,'__class_id__')
        if cls>=len(symbols) or tuple(symbols[cls]) != ('',expected_class):
            raise ValueError('Unexpected material graph class: '+expected_class)
        return result
    def value(obj,name):
        matches=[f for f in obj if f['name']==name]
        if len(matches)!=1:raise ValueError('Missing or ambiguous material field '+name)
        return matches[0]
    def integer(obj,name):return struct.unpack('<I',value(obj,name)['payload'])[0]
    def reference(obj,name):
        f=value(obj,name)
        if f['type']!=17 or f['unit']!=4:raise ValueError('Invalid material reference type')
        return struct.unpack('<i',f['payload'])[0]
    def array(obj,name,count,kind):
        f=value(obj,name)
        if f['type']!=kind or f['unit']!=4 or len(f['payload'])!=count*4:
            raise ValueError('Invalid material array '+name)
        return [v[0] for v in struct.iter_unpack('<i' if kind==17 else '<I',f['payload'])]
    materials=[]
    for identity in sorted({m['materialObject'] for m in decoded['meshes']}):
        material=fields(identity,'FAFMaterial_t')
        state_id=reference(material,'delta_state');channels=[]
        if state_id!=-1:
            state=fields(state_id,'RDeltaState');count=integer(state,'tch_count')
            if count:
                types=array(state,'tch_type',count,3);textures=array(state,'tch_text',count,17)
                for channel,texture_id in zip(types,textures):
                    texture=fields(texture_id,'FAFTexture_t')
                    creator_id=reference(texture,'creator');creator=fields(creator_id,'AnarchyTexCreator_t')
                    channels.append({'channel':channel,'textureObject':texture_id,'creatorObject':creator_id,
                                     'resourceType':integer(creator,'type'),'resourceId':integer(creator,'inst')})
        materials.append({'materialObject':identity,'deltaStateObject':state_id,'textureChannels':channels})
    return materials
