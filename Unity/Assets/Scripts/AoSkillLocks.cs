using System;
using System.Collections.Generic;

namespace Reborn
{
    // Original 18.8.50 list operations: Docs/CLIENT-REFERENCE.md.
    // Durations are already adjusted inputs. This class assigns no clock unit or cadence.
    public sealed class AoSkillLocks
    {
        [Serializable]
        public struct Entry
        {
            public int opaque, skill, duration, remaining;
            public Entry(int opaque, int skill, int duration, int remaining)
            { this.opaque=opaque; this.skill=skill; this.duration=duration; this.remaining=remaining; }
        }
        readonly List<Entry> entries;
        public AoSkillLocks() { entries=new List<Entry>(); }
        public AoSkillLocks(IEnumerable<Entry> initial) { entries=new List<Entry>(initial); }
        public Entry[] Snapshot() { return entries.ToArray(); }
        int Find(int skill) { return entries.FindIndex(e=>e.skill==skill); }
        public bool Contains(int skill) { return Find(skill)>=0; }
        public int Remaining(int skill) { int i=Find(skill); return i<0?0:entries[i].remaining; }
        public int Duration(int skill) { int i=Find(skill); return i<0?0:entries[i].duration; }
        // Presence blocks reuse even when the stored remaining value is zero or negative.
        public bool Insert(int skill, int adjustedDuration)
        {
            if(Contains(skill))return false;
            entries.Add(new Entry(0,skill,adjustedDuration,adjustedDuration));
            return true;
        }
        public bool Extend(int skill, int adjustedDuration)
        {
            int i=Find(skill);
            if(i<0)return Insert(skill,adjustedDuration);
            var e=entries[i];
            e.duration=unchecked(e.duration+adjustedDuration);
            e.remaining=unchecked(e.remaining+adjustedDuration);
            entries[i]=e;
            return false;
        }
        // Caller supplies one original lock-update event, not a Unity frame.
        public void Tick()
        {
            for(int i=0;i<entries.Count;)
            {
                var e=entries[i];e.remaining=unchecked(e.remaining-1);
                if(e.remaining<=0)entries.RemoveAt(i);
                else { entries[i]=e;i++; }
            }
        }
    }
}
