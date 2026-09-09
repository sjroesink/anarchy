import struct,unittest
from pathlib import Path
from decode_client_mesh import decode
from decode_client_items import records

class MeshTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data=Path('Research/ClientReference/solar-rifle-mesh.bin').read_bytes()
        wanted={30234,165052,203233,7826,21182,21191}
        cls.originals={}
        for identity,payload in records(Path('Research/ClientReference/18.8.50/app'),1010001):
            if identity in wanted:cls.originals[identity]=payload
            if len(cls.originals)==len(wanted):break
        cls.multi_data=cls.originals[30234]
    def test_real_geometry(self):
        result=decode(self.data)
        self.assertEqual(len(result['objects']),22)
        self.assertEqual([len(m['vertices']) for m in result['meshes']],[29,29,215])
        self.assertEqual([len(m['triangles']) for m in result['meshes']],[45,45,352])
        self.assertEqual(result['verifiedBoundsComponents'],18)
    def test_truncation(self):
        for n in [0,19,200,736,770,2100,6531,len(self.data)-1]:
            with self.subTest(n=n),self.assertRaises(ValueError):decode(self.data[:n])
    def test_trailing_data(self):
        with self.assertRaisesRegex(ValueError,'Trailing mesh'):decode(self.data+b'\0')
    def test_unknown_header(self):
        with self.assertRaisesRegex(ValueError,'header'):decode(b'\4'+self.data[1:])
    def test_corrupt_position_vs_stored_bounds(self):
        result=decode(self.data);vertex=result['meshes'][0]['vertices'][0]
        data=bytearray(self.data);offset=data.index(struct.pack('<8f',*vertex))
        struct.pack_into('<f',data,offset,100)
        with self.assertRaisesRegex(ValueError,'stored bounds'):decode(bytes(data))
    def test_out_of_range_triangle(self):
        result=decode(self.data)
        field=next(f for obj in result['objects'] for f in obj if f['name']=='triangles')
        data=bytearray(self.data);offset=data.index(field['payload']);struct.pack_into('<H',data,offset+4,65535)
        with self.assertRaisesRegex(ValueError,'Triangle outside'):decode(bytes(data))
    def test_invalid_vertex_descriptor(self):
        data=self.data.replace(struct.pack('<4I',16,65536,274,29),struct.pack('<4I',16,65536,275,29),1)
        with self.assertRaisesRegex(ValueError,'descriptor'):decode(data)

    def test_original_alternate_vertex_flags(self):
        for identity,vertices,triangles in [(165052,612,282),(203233,55,82)]:
            with self.subTest(identity=identity):
                result=decode(self.originals[identity])
                self.assertEqual(len(result['meshes']),1)
                mesh=result['meshes'][0]
                self.assertEqual(tuple(mesh['vertexDescriptor']),(16,67584,274,vertices))
                self.assertEqual(len(mesh['vertices']),vertices)
                self.assertEqual(len(mesh['triangles']),triangles)
                self.assertEqual(result['verifiedBoundsComponents'],6)

    def test_unobserved_vertex_flags(self):
        data=self.data.replace(struct.pack('<4I',16,65536,274,29),struct.pack('<4I',16,1,274,29),1)
        with self.assertRaisesRegex(ValueError,'descriptor'):decode(data)

    def test_original_conservative_sentinel_bounds(self):
        for identity,exact,sentinel in [(7826,5,1),(21182,5,1),(21191,4,2)]:
            with self.subTest(identity=identity):
                result=decode(self.originals[identity])
                self.assertEqual(result['verifiedBoundsComponents'],exact)
                self.assertEqual(result['sentinelExpandedBoundsComponents'],sentinel)
        negative=decode(self.originals[7826])['meshes'][0]['vertices']
        planar=decode(self.originals[21182])['meshes'][0]['vertices']
        self.assertLess(max(v[1] for v in negative),0)
        self.assertEqual(max(v[2] for v in planar),0)

    def mutate_bound(self,name,axis,value):
        data=self.originals[7826];result=decode(data)
        target=next(f for obj in result['objects'] for f in obj if f['name']==name)
        old=bytes([target['symbol']])+struct.pack('<3I',target['type'],target['unit'],len(target['payload']))+target['payload']
        self.assertEqual(data.count(old),1)
        payload=bytearray(target['payload']);struct.pack_into('<f',payload,axis*4,value)
        return data.replace(old,old[:-len(payload)]+payload,1)

    def test_arbitrary_expanded_maximum_rejected(self):
        with self.assertRaisesRegex(ValueError,'stored bounds'):
            decode(self.mutate_bound('max_pos',1,1e-10))

    def test_sentinel_cannot_exclude_positive_vertices(self):
        with self.assertRaisesRegex(ValueError,'stored bounds'):
            decode(self.mutate_bound('max_pos',0,2**-126))

    def test_sentinel_not_accepted_as_minimum(self):
        with self.assertRaisesRegex(ValueError,'stored bounds'):
            decode(self.mutate_bound('min_pos',1,2**-126))

    def test_original_multi_mesh_geometry(self):
        result=decode(self.multi_data)
        self.assertEqual([len(m['vertices']) for m in result['meshes']],[48,64])
        self.assertEqual([len(m['triangles']) for m in result['meshes']],[52,72])
        self.assertEqual(result['verifiedBoundsComponents'],6)

    def mutate_multi_mesh_field(self,name,payload):
        data=self.multi_data
        result=decode(data)
        target=next(f for obj in result['objects'] for f in obj if f['name']==name)
        original=bytes([target['symbol']])+struct.pack('<3I',target['type'],target['unit'],len(target['payload']))+target['payload']
        self.assertEqual(data.count(original),1)
        self.assertEqual(len(payload),len(target['payload']))
        return data.replace(original,original[:-len(payload)]+payload,1)

    def test_multi_mesh_count_mismatch(self):
        data=self.mutate_multi_mesh_field('num_meshes',struct.pack('<I',1))
        with self.assertRaisesRegex(ValueError,'Mesh reference count'):decode(data)

    def test_multi_mesh_wrong_object_class(self):
        data=self.mutate_multi_mesh_field('mesh',struct.pack('<2i',6,18))
        with self.assertRaisesRegex(ValueError,'mesh reference class'):decode(data)

    def test_multi_mesh_outside_object_table(self):
        data=self.mutate_multi_mesh_field('mesh',struct.pack('<2i',6,100000))
        with self.assertRaisesRegex(ValueError,'Invalid object reference'):decode(data)

if __name__=='__main__':unittest.main()
