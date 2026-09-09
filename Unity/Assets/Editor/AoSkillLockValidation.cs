using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Reborn;

public static class AoSkillLockValidation
{
    [Serializable] public class Fixtures { public string sourceReportSha256; public Fixture[] fixtures; }
    [Serializable] public class Fixture
    {
        public string operation;
        public AoSkillLocks.Entry[] initial, expected;
        public int skill, duration, ticks, remaining;
        public bool membership, inserted;
    }
    public static void Run()
    {
        var data=JsonUtility.FromJson<Fixtures>(File.ReadAllText("../Artifacts/unity-skill-lock-fixtures.json"));
        using(var sha=SHA256.Create())
        {
            string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes("../Artifacts/special-lock-lookup-emulation.json"))).Replace("-","").ToLowerInvariant();
            if(hash!=data.sourceReportSha256)throw new Exception("Stale skill-lock fixtures: run Tools/export_skill_lock_fixtures.py");
        }
        if(data.fixtures.Length!=44)throw new Exception("Unexpected original fixture coverage");
        foreach(var f in data.fixtures)
        {
            var locks=new AoSkillLocks(f.initial);
            if(f.operation=="read")
            {
                if(locks.Contains(f.skill)!=f.membership||locks.Remaining(f.skill)!=f.remaining)
                    throw new Exception("Original lock reader mismatch");
                continue;
            }
            if(f.operation=="tick")for(int i=0;i<f.ticks;i++)locks.Tick();
            else
            {
                bool inserted;
                if(f.operation=="extend")inserted=locks.Extend(f.skill,f.duration);
                else if(f.operation=="insert")inserted=locks.Insert(f.skill,f.duration);
                else throw new Exception("Unknown fixture operation");
                if(inserted!=f.inserted)throw new Exception("Original insertion notification mismatch");
            }
            if(!locks.Snapshot().SequenceEqual(f.expected))throw new Exception("Original lock payload mismatch: "+f.operation);
        }
        File.WriteAllText("../Artifacts/skill-lock-validation.txt","PASS: 44 Unity fixtures match original instruction results. Clock cadence, duration calculation and network transport are outside this comparison.\n");
        Debug.Log("SKILL_LOCK_VALIDATION_OK: 44 original-result fixtures");
    }
}
