import unittest
from extract_burst_reference import project

class BurstReferenceTests(unittest.TestCase):
    def item(self,stats):return {'id':1,'name':'fixture','payloadSha256':'fixture','stats':stats}
    def test_absent_cycle_is_unknown(self):
        result=project(self.item({'30':2048}))
        self.assertFalse(result['burstCyclePresent']);self.assertIsNone(result['burstCycleRaw'])
    def test_explicit_zero_is_preserved(self):
        result=project(self.item({'30':2048,'374':0}))
        self.assertTrue(result['burstCyclePresent']);self.assertEqual(result['burstCycleRaw'],0)
    def test_cycle_and_timing_not_rescaled_or_defaulted(self):
        result=project(self.item({'30':3077,'374':3000,'294':100,'210':150,'212':4294967295}))
        self.assertEqual((result['burstCycleRaw'],result['attackTimeRaw'],result['rechargeTimeRaw'],result['clipSizeRaw']),(3000,100,150,4294967295))
    def test_non_burst_item_is_excluded(self):
        self.assertIsNone(project(self.item({'30':1029,'374':3000})))

if __name__=='__main__':unittest.main()
