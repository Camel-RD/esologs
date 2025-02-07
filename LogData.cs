using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;
using System.Drawing.Drawing2D;

namespace ESOLogs
{
    public enum ELineType { NONE, BEGIN_LOG, ZONE_CHANGED, UNIT_ADDED, UNIT_REMOVED, 
        COMBAT_EVENT, BEGIN_COMBAT, END_COMBAT}
    public enum EUnitType { NONE, PLAYER, MONSTER, OBJECT }
    public enum ECombatActionResult { NONE, DAMAGE, CRITICAL_DAMAGE, DOT_TICK, 
        DOT_TICK_CRITICAL, DAMAGE_SHIELDED, DIED }
    public class LogData
    {
        public Dictionary<int, Unit> Units = new Dictionary<int, Unit>();
        public Unit LocalPlayer = null;
        public List<FightData> FightData = new List<FightData>();
        public DateTime LogStarted;
        public FightData CurrentFightData = null;
        public FightData LastEndedFightData = null;
        public string CurrentZone;
        public int CountSourceIdZero = 0;
        public int CountUnitNotFound = 0;

        public void ReadFile(string filename)
        {
            StreamReader str;
            using (str = File.OpenText(filename))
            {
                ReadFile(ReadLines());
            }

            IEnumerable<string> ReadLines()
            {
                string s;
                while ((s = str.ReadLine()) != null)
                {
                    yield return s;
                }
            }
        }

        public void ReadFile(IEnumerable<string> logfilelines)
        {
            string lastline = null;
            LineUnitAdded.AnonymousCounter = 0;

            foreach (var logfileline in logfilelines)
            {
                if (string.IsNullOrEmpty(logfileline?.Trim()))
                    continue;
                lastline = logfileline;
                var lineparts = logfileline.Split(',', StringSplitOptions.RemoveEmptyEntries);

                CurrentFightData?.TestForDelugeEvent(lineparts);

                if (!Enum.TryParse(lineparts[1], out ELineType linetype))
                    continue;

                LogLine logline = linetype switch
                {
                    ELineType.COMBAT_EVENT => new LineCombatEvent(),
                    ELineType.UNIT_ADDED => new LineUnitAdded(),
                    ELineType.UNIT_REMOVED => new LineUnitRemoved(),
                    ELineType.BEGIN_COMBAT => new LineBeginCombat(),
                    ELineType.END_COMBAT => new LineEndCombat(),
                    ELineType.ZONE_CHANGED => new LineZoneChanged(),
                    ELineType.BEGIN_LOG => new LineBeginLog(),
                    _ => null
                };
                
                if(logline == null || !logline.Read(lineparts))
                    continue;

                if (logline is LineUnitAdded loglineUnitAdded)
                {
                    Units[loglineUnitAdded.UnitId] = loglineUnitAdded.Unit;
                    if (loglineUnitAdded.Unit.IsLocalPlayer)
                        LocalPlayer = loglineUnitAdded.Unit;
                    if (loglineUnitAdded.Unit.OwnerUnitId > 0)
                    {
                        if (Units.TryGetValue(loglineUnitAdded.Unit.OwnerUnitId, out var ownerunit))
                        {
                            loglineUnitAdded.Unit.OwnerUnit = ownerunit;
                        }
                    }
                }
                else if (logline is LineCombatEvent logLineCombatEvent)
                {
                    if (logLineCombatEvent.CombatActionResult == ECombatActionResult.DIED)
                    {
                        DoDeathEvent();
                    }
                    else
                    {
                        DoCombatEvent();
                    }

                    void DoDeathEvent()
                    {
                        if (CurrentFightData == null)
                        {
                            return;
                        }
                        if (logLineCombatEvent.SourceUnitId == 0)
                        {
                            CountSourceIdZero++;
                            return;
                        }
                        if (logLineCombatEvent.SourceUnitId == logLineCombatEvent.TargetUnitId)
                        {
                            return;
                        }
                        if (!Units.TryGetValue(logLineCombatEvent.TargetUnitId, out var target))
                        {
                            CountUnitNotFound++;
                            return;
                        }
                        if (target.UnitType != EUnitType.PLAYER)
                        {
                            return;
                        }
                        var fighteventdata = CurrentFightData.GetFightEventData(target);
                        fighteventdata.Deaths++;
                    }

                    void DoCombatEvent()
                    {
                        if (logLineCombatEvent.SourceUnitId == 0)
                        {
                            CountSourceIdZero++;
                            return;
                        }

                        if (logLineCombatEvent.HitValue == 0 ||
                            logLineCombatEvent.SourceUnitId == logLineCombatEvent.TargetUnitId)
                        {
                            return;
                        }

                        if (!Units.TryGetValue(logLineCombatEvent.SourceUnitId, out var source) ||
                            !Units.TryGetValue(logLineCombatEvent.TargetUnitId, out var target))
                        {
                            CountUnitNotFound++;
                            return;
                        }

                        if (!(source.UnitType == EUnitType.PLAYER ||
                            (source.UnitType == EUnitType.MONSTER &&
                            source.OwnerUnit?.UnitType == EUnitType.PLAYER)))
                        {
                            return;
                        }

                        if (CurrentFightData == null)
                        {
                            if (LastEndedFightData != null &&
                                (logLineCombatEvent.TS - LastEndedFightData.EndedTS) < 1000)
                            {
                                CurrentFightData = LastEndedFightData;
                            }
                            else
                            {
                                CurrentFightData = new FightData(LogStarted, logLineCombatEvent.TS, CurrentZone);
                                FightData.Add(CurrentFightData);
                            }
                        }

                        if (CurrentFightData.FirstDmgTS == 0)
                            CurrentFightData.FirstDmgTS = logLineCombatEvent.TS;
                        CurrentFightData.LastDmgTS = logLineCombatEvent.TS;

                        logLineCombatEvent.SourceUnit = source;
                        logLineCombatEvent.TargetUnit = target;
                        source.DamageDone += logLineCombatEvent.HitValue;
                        target.DamageReceived += logLineCombatEvent.HitValue;
                        var fighteventdataforsource = CurrentFightData.GetFightEventData(source);
                        var fighteventdatafortarget = CurrentFightData.GetFightEventData(target);
                        fighteventdataforsource.DamageDone += logLineCombatEvent.HitValue;
                        fighteventdatafortarget.DamageReceived += logLineCombatEvent.HitValue; ;
                        
                        if (source.OwnerUnit != null)
                        {
                            source.OwnerUnit.DamageDone += logLineCombatEvent.HitValue;
                            var fighteventdataforownedunit = CurrentFightData.GetFightEventData(source.OwnerUnit);
                            fighteventdataforownedunit.DamageDone += logLineCombatEvent.HitValue;
                        }
                    }
                }
                else if (logline is LineBeginCombat logLineBeginCombat)
                {
                    if (CurrentFightData == null)
                    {
                        if (LastEndedFightData != null &&
                            (logLineBeginCombat.TS - LastEndedFightData.EndedTS) < 1000)
                        {
                            CurrentFightData = LastEndedFightData;
                        }
                        else
                        {
                            CurrentFightData = new FightData(LogStarted, logLineBeginCombat.TS, CurrentZone);
                            FightData.Add(CurrentFightData);
                        }
                    }
                }
                else if (logline is LineEndCombat logLineEndCombat)
                {
                    if (CurrentFightData != null)
                    {
                        CurrentFightData.EndedTS = logLineEndCombat.TS;
                        CurrentFightData.Ended = LogStarted.AddMilliseconds(logLineEndCombat.TS);
                        CurrentFightData.Check();
                        LastEndedFightData = CurrentFightData;
                        CurrentFightData = null;
                    }
                }
                else if (logline is LineBeginLog logLineBeginLog)
                {
                    LogStarted = logLineBeginLog.StartDate;
                }
                else if (logline is LineZoneChanged logLineZoneChanged)
                {
                    CurrentZone = logLineZoneChanged.Name;
                    if (CurrentFightData != null)
                        CurrentFightData.ZoneName = CurrentZone;
                }
            }
            if (FightData.Count > 0 && FightData[FightData.Count - 1].Ended == DateTime.MinValue &&
                lastline != null)
            {
                int ts = int.Parse(lastline.Split(',')[0]);
                var fd = FightData[FightData.Count - 1];
                CurrentFightData.EndedTS = ts;
                fd.Ended = LogStarted.AddMilliseconds(ts);
                CurrentFightData.Check();
            }

        }
    }


    public class FightEventData
    {
        public int DamageDone;
        public int DamageReceived;
        public int Deaths;
    }

    public class FightData
    {
        public DateTime Started = DateTime.MinValue;
        public DateTime Ended = DateTime.MinValue;
        public DateTime LogStarted = DateTime.MinValue;
        public long StartedTS;
        public long EndedTS;
        public long FirstDmgTS;
        public long LastDmgTS;
        public bool IsBossFight;
        public string BossName;
        public string ZoneName;
        public Dictionary<Unit, FightEventData> FightEvents = new();
        public double DurationInSeconds { get; private set; }
        public DelugeTracker DelugeTracker = null;

        public FightEventData GetFightEventData(Unit unit)
        {
            if (!FightEvents.TryGetValue(unit, out var fighteventdata))
            {
                fighteventdata = new FightEventData();
                FightEvents[unit] = fighteventdata;
            }
            return fighteventdata;
        }

        public FightData(DateTime logstarted, long startedts, string zonename)
        {
            LogStarted = logstarted;
            StartedTS = startedts;
            Started = LogStarted.AddMilliseconds(startedts);
            ZoneName = zonename;
            if (zonename == "Dreadsail Reef")
            {
                DelugeTracker = new();
            }
        }

        public void Check()
        {
            if (FirstDmgTS > 0)
            {
                var sdt = LogStarted.AddMilliseconds(FirstDmgTS);
                if (sdt > Started)
                {
                    Started = sdt;
                    StartedTS = FirstDmgTS;
                }
            }
            if (LastDmgTS > 0)
            {
                var edt = LogStarted.AddMilliseconds(LastDmgTS);
                if (edt < Ended)
                {
                    Ended = edt;
                    EndedTS = LastDmgTS;
                }
            }
            DurationInSeconds = (Ended - Started).TotalSeconds;
            var boss = FightEvents
                .Where(x => x.Key.IsBoss && x.Value.DamageReceived > 0)
                .Select(x => x.Key)
                .FirstOrDefault();
            if (boss == null) return;
            IsBossFight = true;
            BossName = boss.Name;
        }

        public void TestForDelugeEvent(string[] line)
        {
            DelugeTracker?.TestEvent(line);
        }
    }

    public class LineBeginLog : LogLine
    {
        public long StartTS { get; set; }
        public DateTime StartDate { get; private set; }

        public override bool Read(string[] line)
        {
            if (line == null || line.Length <= 2) return false;
            base.Read(line);
            StartTS = long.Parse(line[2]);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(StartTS);
            StartDate = dt.ToLocalTime();
            return true;
        }
    }

    public class LineZoneChanged : LogLine
    {
        public string Name { get; set; }
        public override bool Read(string[] line)
        {
            if (line == null || line.Length <= 3) return false;
            base.Read(line);
            Name = line[3].Replace("\"","");
            return true;
        }
    }

    public class LineBeginCombat : LogLine
    {
    }

    public class LineEndCombat : LogLine
    {
    }

    public class LineUnitAdded : LogLine
    {
        public static int AnonymousCounter { get; set; }
        public int UnitId { get; set; }
        public Unit Unit { get; set; }
        public override bool Read(string[] line)
        {
            if (line == null || line.Length <= 15) return false;
            base.Read(line);
            UnitId = int.Parse(line[2]);
            if (!Enum.TryParse(line[3], out EUnitType unittype))
                return false;
            Unit = new Unit()
            {
                Id = UnitId,
                UnitType = unittype,
                PlayerPerSessionId = int.Parse(line[5]),
                MonsterId = long.Parse(line[6]),
                IsBoss = line[7] == "T",
                Name = line[10].Replace("\"",""),
                DisplayName = line[11].Replace("\"", ""),
                CharacterId = line[12],
                Level = int.Parse(line[13]),
                championPoints = int.Parse(line[14]),
                OwnerUnitId = int.Parse(line[15]),
            };
            Unit.IsLocalPlayer = line[4] == "T" && Unit.UnitType == EUnitType.PLAYER;
            if (Unit.Name == "")
            {
                AnonymousCounter++;
                if (Unit.Level < 50)
                    Unit.Name = $"?_{AnonymousCounter}_lvl{Unit.Level}";
                else
                    Unit.Name = $"?_{AnonymousCounter}_cp{Unit.championPoints}";
            }
            if (Unit.DisplayName == "")
            {
                Unit.DisplayName = Unit.Name;
            }
            return true;
        }
    }

    public class LineUnitRemoved : LogLine
    {
        public int UnitId { get; set; }
        public Unit Unit { get; set; }
        public override bool Read(string[] line)
        {
            if (line == null || line.Length <= 2) return false;
            base.Read(line);
            UnitId = int.Parse(line[2]);
            return true;
        }
    }

    public class LineCombatEvent : LogLine
    {
        public ECombatActionResult CombatActionResult { get; set; } = ECombatActionResult.NONE;
        public int SourceUnitId { get; set; }
        public int TargetUnitId { get; set; }
        public Unit SourceUnit { get; set; }
        public Unit TargetUnit { get; set; }
        public int HitValue { get; set; }
        public int Overflow { get; set; }
        public long AbilityId { get; set; }

        public override bool Read(string[] line)
        {
            if (line == null || line.Length <= 19) return false;
            base.Read(line);
            if (!Enum.TryParse(line[2], out ECombatActionResult act))
                return false;
            CombatActionResult = act;
            HitValue = int.Parse(line[5]);
            Overflow = int.Parse(line[6]);
            AbilityId = int.Parse(line[8]);
            SourceUnitId = int.Parse(line[9]);
            TargetUnitId = line[19] == "*" ? SourceUnitId : int.Parse(line[19]);
            return true;
        }
    }

    public class LogLine
    {
        public ELineType LineType { get; set; } = ELineType.NONE;
        public long TS { get; set; }

        public virtual bool Read(string[] line)
        {
            TS = long.Parse(line[0]);
            if (!Enum.TryParse(line[1], out ELineType lineType))
                return false;
            LineType = lineType;
            return true;
        }
    }

    public class Unit
    {
        public int Id { get; set; }
        public bool IsLocalPlayer { get; set; }
        public EUnitType UnitType { get; set; }
        public int PlayerPerSessionId { get; set; }
        public long MonsterId { get; set; }
        public bool IsBoss { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string CharacterId { get; set; }
        public int Level { get; set; }
        public int championPoints { get; set; }
        public int OwnerUnitId { get; set; }
        public Unit OwnerUnit { get; set; }

        public int DamageDone { get; set; }
        public int DamageReceived { get; set; }

    }

}
