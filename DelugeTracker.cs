using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESOLogs
{
    public class DelugeTrackerData()
    {
        public int TSGained;
        public int TSFaded;
        public int TSSlaughterfishAttack;
        public int TSDied;
        public int CountWasNotInWater;
        public void ClearA()
        {
            TSGained = 0;
            TSFaded = 0;
            TSSlaughterfishAttack = 0;
            TSDied = 0;
        }
    }


    public class DelugeTracker()
    {
        public Dictionary<int, DelugeTrackerData> Data = new();
        
        public void TestEvent(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            var rtx = LogLine.ReadLineHeader(line);
            if (!rtx.rt) return;
            if (rtx.tp != ELineType.EFFECT_CHANGED &&
                rtx.tp != ELineType.COMBAT_EVENT) return;
            var parts = line.Split(',');
            TestEvent(parts);
        }

        public void TestEvent(string[] line)
        {
            var eventtype = line[1];
            string changetype = null;
            string abilityid = null;
            int targetid = 0;
            int ts = int.Parse(line[0]);
            if (eventtype == "EFFECT_CHANGED")
            {
                abilityid = line[5];
                if (abilityid != "174960") return;
                changetype = line[2];
                targetid = int.Parse(line[16]);
                if (changetype == "GAINED")
                {
                    var data = GetData(targetid);
                    if (data.TSGained == 0 || (ts - data.TSGained > 10000))
                    {
                        data.ClearA();
                        data.TSGained = ts;
                    }
                }
                else if (changetype == "FADED")
                {
                    var data = GetData(targetid);
                    if (data.TSGained > 0 && ts > data.TSGained && (ts - data.TSGained < 10000))
                    {
                        data.TSFaded = ts;
                        if ((data.TSSlaughterfishAttack == 0 ||
                            ts - data.TSSlaughterfishAttack < 100) &&
                            (data.TSDied == 0 || ts - data.TSDied < 10))
                        {
                            data.CountWasNotInWater++;
                        }
                    }
                }
            }
            else if (eventtype == "COMBAT_EVENT" && line.Length > 19)
            {
                abilityid = line[8];
                changetype = line[2];
                if (changetype == "DIED")
                {
                    targetid = int.Parse(line[19]);
                    var data = GetData(targetid, false);
                    if (data != null && data.TSGained > 0 &&
                        ts > data.TSGained && (ts - data.TSGained < 10000))
                    {
                        data.TSDied = ts;
                    }

                }
                if (abilityid != "167627") return;
                if (changetype == "DAMAGE")
                {
                    targetid = int.Parse(line[19]);
                    var data = GetData(targetid);
                    if (data.TSGained > 0 &&
                        ts > data.TSGained &&
                        (ts - data.TSGained < 10000) &&
                        data.TSSlaughterfishAttack == 0)
                    {
                        data.TSSlaughterfishAttack = ts;
                    }
                }
            }
        }
        public DelugeTrackerData GetData(int id, bool createifnotfound = true)
        {
            if (Data.TryGetValue(id, out var ret))
                return ret;
            if (!createifnotfound) return null;
            ret = new DelugeTrackerData();
            Data[id] = ret;
            return ret;
        }
    }

}
