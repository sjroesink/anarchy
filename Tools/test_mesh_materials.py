import copy,struct,unittest
from pathlib import Path
from decode_client_mesh import decode
from decode_mesh_materials import resolve_materials

class MaterialTests(unittest.TestCase):
    def setUp(self):
        self.mesh=decode(Path('Research/ClientReference/solar-rifle-mesh.bin').read_bytes())
    def field(self,identity,name):
        return next(f for f in self.mesh['objects'][identity+1] if f['name']==name)
    def test_original_rifle_texture_graph(self):
        materials=resolve_materials(self.mesh)
        self.assertEqual(materials,[{'materialObject':6,'deltaStateObject':7,'textureChannels':[
            {'channel':0,'textureObject':8,'creatorObject':9,'resourceType':1010004,'resourceId':17222}]}])
    def test_texture_identity_is_read_from_creator(self):
        self.field(9,'inst')['payload']=struct.pack('<I',12345)
        self.assertEqual(resolve_materials(self.mesh)[0]['textureChannels'][0]['resourceId'],12345)
    def test_wrong_creator_class(self):
        self.field(8,'creator')['payload']=struct.pack('<i',10)
        with self.assertRaisesRegex(ValueError,'class'):resolve_materials(self.mesh)
    def test_reference_outside_table(self):
        self.field(8,'creator')['payload']=struct.pack('<i',999)
        with self.assertRaisesRegex(ValueError,'graph reference'):resolve_materials(self.mesh)
    def test_channel_count_mismatch(self):
        self.field(7,'tch_count')['payload']=struct.pack('<I',2)
        with self.assertRaisesRegex(ValueError,'material array'):resolve_materials(self.mesh)
    def test_duplicate_ambiguous_reference(self):
        self.mesh['objects'][9].append(copy.deepcopy(self.field(8,'creator')))
        with self.assertRaisesRegex(ValueError,'ambiguous'):resolve_materials(self.mesh)
    def test_wrong_reference_type(self):
        self.field(8,'creator')['type']=3
        with self.assertRaisesRegex(ValueError,'reference type'):resolve_materials(self.mesh)

if __name__=='__main__':unittest.main()
