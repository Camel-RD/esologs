using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Numerics;
using System.Drawing;

namespace ESOLogs
{
    public enum ELineType { NONE, BEGIN_LOG, ZONE_CHANGED, UNIT_ADDED, UNIT_REMOVED, 
        COMBAT_EVENT, BEGIN_COMBAT, END_COMBAT, EFFECT_CHANGED, PLAYER_INFO, ABILITY_INFO
    }
    public enum EUnitType { NONE, PLAYER, MONSTER, OBJECT }
    public enum ECombatActionResult { NONE, DAMAGE, CRITICAL_DAMAGE, DOT_TICK, 
        DOT_TICK_CRITICAL, DAMAGE_SHIELDED, DIED }
    public class LogData
    {
        public Dictionary<int, Unit> Units = new();
        public Unit LocalPlayer = null;
        public List<FightData> FightData = new List<FightData>();
        public DateTime LogStarted;
        public FightData CurrentFightData = null;
        public FightData LastEndedFightData = null;
        public string CurrentZone;
        public int CountSourceIdZero = 0;
        public int CountUnitNotFound = 0;

        public static Dictionary<int, string> AbilityNamesById = new();
        public static Dictionary<int, string> SetNamesById = new();

        public void ReadFile(string filename)
        {
            ReadDataFilesIfNeeded();

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
                var rtx = LogLine.ReadLineHeader(logfileline);
                CurrentFightData?.TestForDelugeEvent(logfileline);
                if (!rtx.rt) continue;
                var linetype = rtx.tp;

                LogLine logline = linetype switch
                {
                    ELineType.COMBAT_EVENT => new LineCombatEvent(),
                    ELineType.UNIT_ADDED => new LineUnitAdded(),
                    ELineType.UNIT_REMOVED => new LineUnitRemoved(),
                    ELineType.BEGIN_COMBAT => new LineBeginCombat(),
                    ELineType.END_COMBAT => new LineEndCombat(),
                    ELineType.ZONE_CHANGED => new LineZoneChanged(),
                    ELineType.BEGIN_LOG => new LineBeginLog(),
                    ELineType.PLAYER_INFO=> new LinePlayerInfo(),
                    ELineType.ABILITY_INFO => new LineAbilityInfo(),
                    _ => null
                };
                
                if(logline == null || !logline.Read(logfileline))
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
                            (logLineBeginCombat.TS - LastEndedFightData.EndedTS) < 5000)
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
                else if (logline is LineAbilityInfo logLineAbilityInfo)
                {
                    AbilityNamesById[logLineAbilityInfo.AbilityId] = logLineAbilityInfo.Name;
                }
                else if (logline is LinePlayerInfo linePlayerInfo)
                {
                    if (Units.TryGetValue(linePlayerInfo.UnitId, out var unit))
                    {
                        if (unit.PlayerInfoData.Count == 0)
                        {
                            unit.PlayerInfoData.Add(linePlayerInfo);
                        }
                        else
                        {
                            var last_pi = unit.PlayerInfoData[unit.PlayerInfoData.Count - 1];
                            if (linePlayerInfo.TS - last_pi.TS < 5000)
                            {
                                unit.PlayerInfoData[unit.PlayerInfoData.Count - 1] = linePlayerInfo;
                            }
                            else
                            {
                                unit.PlayerInfoData.Add(linePlayerInfo);
                            }
                        }
                    }
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

        void ReadDataFilesIfNeeded()
        {
            if (SetNamesById.Count > 0) return;
            SetNamesById = ReadData("data_sets.txt");

            Dictionary<int, string> ReadData(string file_name)
            {
                var lines = File.ReadAllLines(file_name);
                var ret = lines
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Split('='))
                    .Where(x => x.Length == 2)
                    .Select(x => (id: x[0].Trim(), name: x[1].Trim()))
                    .Where(x => !string.IsNullOrEmpty(x.id) && !string.IsNullOrEmpty(x.name))
                    .Where(x => int.TryParse(x.id, out _))
                    .Select(x => (id: int.Parse(x.id), x.name))
                    .ToDictionary(x => x.id, x => x.name);
                return ret;
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

        public void TestForDelugeEvent(string line)
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
                ClassId = string.IsNullOrWhiteSpace(line[8]) ? -1 : int.Parse(line[8]),
                Name = line[10].Replace("\"",""),
                DisplayName = line[11].Replace("\"", ""),
                CharacterId = line[12],
                Level = int.Parse(line[13]),
                championPoints = int.Parse(line[14]),
                OwnerUnitId = int.Parse(line[15]),
            };
            Unit.IsLocalPlayer = line[4] == "T" && Unit.UnitType == EUnitType.PLAYER;
            if (Unit.UnitType == EUnitType.PLAYER)
            {
                Unit.ClassName = Unit.GetClassName(Unit.ClassId);
            }
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

        public static (bool rt, long ts, ELineType tp) ReadLineHeader(string line)
        {
            if (line == null || line.Length < 5) return (false, 0, ELineType.NONE);

            int k1 = line.IndexOf(',');
            if (k1 == -1) return (false, 0, ELineType.NONE);
            int k2 = line.IndexOf(',', k1 + 1);
            if (k2 == -1) k2 = line.Length;
            if (k2 == k1 + 1) return (false, 0, ELineType.NONE);
            var ts_part = line.Substring(0, k1);
            var linetype_part = line.Substring(k1 + 1, k2 - k1 - 1);
            long ts = long.Parse(ts_part);
            if (!Enum.TryParse(linetype_part, out ELineType lineType))
                return (false, 0, ELineType.NONE);
            return (true, ts, lineType);
        }

        public virtual bool Read(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            var lineparts = line.Split(',');
            if (lineparts.Length < 2) return false;
            var rt = Read(lineparts);
            return rt;
        }

        public virtual bool Read(string[] line)
        {
            TS = long.Parse(line[0]);
            if (!Enum.TryParse(line[1], out ELineType lineType))
                return false;
            LineType = lineType;
            return true;
        }

        public static List<string> SplitBrackets(string line)
        {
            var ret = new List<string>();
            if (string.IsNullOrWhiteSpace(line)) return null;
            int k1 = -1, k2 = -1;
            int ct_open = 0;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '[')
                {
                    if (k1 == -1)
                    {
                        k1 = i;
                        ct_open = 1;
                    }
                    else
                    {
                        ct_open++;
                    }
                    continue;
                }
                if (c == ']')
                {
                    if (k1 == -1)
                    {
                        return null;
                    }
                    ct_open--;
                    if (ct_open == 0)
                    {
                        k2 = i;
                        var part = k2 == (k1 + 1) ? "" : line.Substring(k1 + 1, k2 - k1 - 1);
                        ret.Add(part);
                        k1 = k2 = -1;
                    }
                    continue;
                }
            }
            return ret;
        }
    }

    public class Unit
    {
        public static string[] ClassNames = 
            ["1", "Dragon Knight", "2", "Sorcerer", "3", "Nightblade",
            "4", "Warden", "5", "Necromancer", "6", "Templar", "117", "Arcanist"];

        public int Id { get; set; }
        public bool IsLocalPlayer { get; set; }
        public EUnitType UnitType { get; set; }
        public int PlayerPerSessionId { get; set; }
        public long MonsterId { get; set; }
        public bool IsBoss { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string CharacterId { get; set; }
        public int Level { get; set; }
        public int championPoints { get; set; }
        public int OwnerUnitId { get; set; }
        public Unit OwnerUnit { get; set; }

        public int DamageDone { get; set; }
        public int DamageReceived { get; set; }

        public List<LinePlayerInfo> PlayerInfoData = new();

        public string GetClassName(int classid)
        {
            var sid = classid.ToString();
            for (int i = 0; i < ClassNames.Length; i += 2)
            {
                if (ClassNames[i] == sid)
                {
                    return ClassNames[i + 1];
                }
            }
            return "unknown";
        }

        public void PreparePlayerInfoData1(out List<EquipmentInfo> equipmentsets, out List<AbilityBars> abilitybars)
        {
            equipmentsets = new();
            abilitybars = new();
            foreach (var data in PlayerInfoData)
            {
                var eq = equipmentsets.Find(x => x.Equals(data.EquipmentInfo));
                if (eq != null) continue;
                if (equipmentsets.Count == 2)
                {
                    var b = data.EquipmentInfo.Equals(equipmentsets[0]);
                    b = data.EquipmentInfo.Equals(equipmentsets[1]);
                }
                equipmentsets.Add(data.EquipmentInfo);
            }
            foreach (var data in PlayerInfoData)
            {
                var ab = abilitybars.Find(x => x.Equals(data.AbilityBars));
                if (ab != null) continue;
                abilitybars.Add(data.AbilityBars);
            }
        }

        public string MakeReportPart(IForm2 formtools)
        {
            List<EquipmentInfo> equipmentsets;
            List<AbilityBars> abilitybars;
            PreparePlayerInfoData1(out equipmentsets, out abilitybars);
            formtools.WriteOutColoredBoldText($"{DisplayName}, ", Color.Pink);
            formtools.WriteOutColoredBoldText($"{Name}, {ClassName}, lvl:{(championPoints == 0 ? Level : championPoints)}", Color.Beige);
            formtools.WriteOutText("\r\n");
            int k = 1;
            foreach (var data in equipmentsets)
            {
                data.MkaeReportPart(formtools, k);
                formtools.WriteOutText("\r\n");
                k++;
            }
            k = 1;
            foreach (var data in abilitybars)
            {
                data.MkaeReportPart(formtools, k);
                formtools.WriteOutText("\r\n");
                k++;
            }
            return "";
        }

    }

    public class LineAbilityInfo : LogLine
    {
        public int AbilityId { get; set; }
        public string Name { get; set; }

        public override bool Read(string[] line)
        {
            if (line == null || line.Length < 4) return false;
            if (!int.TryParse(line[2], out int id)) return false;
            AbilityId = id;
            Name = line[3];
            Name = Name.Substring(1, Name.Length - 2);
            return true;
        }

    }


    public enum EquipmentSlot
    {
        HEAD, CHEST, SHOULDERS, WAIST, LEGS, FEET, HAND,
        NECK, RING1, RING2, MAIN_HAND, BACKUP_MAIN
    }

    public class EquipmentPieceInfo
    {
        public static string[] EquipmentSlotNames = 
            ["Head", "Chest", "Shoulders", "Waist", "Legs", "Feet", "Hand",
            "Neck", "Ring1", "Ring2", "Main hand", "Backup hand"];

        public EquipmentSlot? Slot { get; set; }
        public string SlotName => Slot == null ? "none" : EquipmentSlotNames[(int)Slot];
        public int SetId { get; set; }
        public string SetName { get; set; }
        public string Trait { get; set; }
        public string EnchantType { get; set; }

        public bool Equals(EquipmentPieceInfo obj)
        {
            if (obj == null) return false;
            if (Slot != obj.Slot) return false;
            if (SetId != obj.SetId) return false;
            if (Trait != obj.Trait) return false;
            if (EnchantType != obj.EnchantType) return false;
            return true;
        }

        public bool Read(string[] parts)
        {
            if (!Enum.TryParse(parts[0], out EquipmentSlot slot))
                return false;
            Slot = slot;
            var trait = parts[4];
            int k = trait.IndexOf('_');
            if (k == -1) return false;
            Trait = trait.Substring(k + 1).ToLower();
            if (!int.TryParse(parts[6], out int setid)) return false;
            SetId = setid;
            if (!LogData.SetNamesById.TryGetValue(setid, out string set_name))
                SetName = "unknown";
            else
                SetName = set_name;
            var enchant = parts[7] ?? "none";
            EnchantType = enchant.ToLower();
            return true;
        }

        public void MkaeReportLine(IForm2 formtools)
        {
            formtools.WriteOutColoredText($"    {SlotName}, ", Color.Yellow);
            formtools.WriteOutColoredText($"{SetName}, ", Color.Beige);
            formtools.WriteOutText($"{Trait}, {EnchantType.Replace("_", " ")}\r\n");
        }
    }

    public class EquipmentInfo
    {
        public EquipmentPieceInfo[] EquipmentPieces { get; private set; } = new EquipmentPieceInfo[12];
        public EquipmentPieceInfo Head => EquipmentPieces[(int)EquipmentSlot.HEAD];
        public EquipmentPieceInfo Chest => EquipmentPieces[(int)EquipmentSlot.CHEST];
        public EquipmentPieceInfo Shoulders => EquipmentPieces[(int)EquipmentSlot.SHOULDERS];
        public EquipmentPieceInfo Waist => EquipmentPieces[(int)EquipmentSlot.WAIST];
        public EquipmentPieceInfo Legs => EquipmentPieces[(int)EquipmentSlot.LEGS];
        public EquipmentPieceInfo Feet => EquipmentPieces[(int)EquipmentSlot.FEET];
        public EquipmentPieceInfo Hand => EquipmentPieces[(int)EquipmentSlot.HAND];
        public EquipmentPieceInfo Neck => EquipmentPieces[(int)EquipmentSlot.NECK];
        public EquipmentPieceInfo Ring1 => EquipmentPieces[(int)EquipmentSlot.RING1];
        public EquipmentPieceInfo Ring2 => EquipmentPieces[(int)EquipmentSlot.RING2];
        public EquipmentPieceInfo MainHand => EquipmentPieces[(int)EquipmentSlot.MAIN_HAND];
        public EquipmentPieceInfo BackupHand => EquipmentPieces[(int)EquipmentSlot.BACKUP_MAIN];

        
        public EquipmentInfo()
        {
            for (int i = 0; i < EquipmentPieces.Length; i++)
            {
                var eqp = new EquipmentPieceInfo()
                {
                    Slot = (EquipmentSlot)i,
                    SetId = 0,
                    SetName = "none",
                    Trait = "",
                    EnchantType = ""
                };
                EquipmentPieces[i] = eqp;
            }
        }

        public bool Equals(EquipmentInfo obj)
        {
            if (obj == null) return false;
            for (int i = 0; i < EquipmentPieces.Length; i++)
            {
                var o1 = EquipmentPieces[i];
                var o2 = obj.EquipmentPieces[i];
                if (o1 == null && o2 != null) return false;
                if (o1 != null && o2 == null) return false;
                if (!o1.Equals(o2)) 
                    return false;
            }
            return true;
        }

        public bool Read(string eq_part)
        {
            if (string.IsNullOrWhiteSpace(eq_part)) return true;
            var slot_parts = LogLine.SplitBrackets(eq_part);
            if (slot_parts == null || slot_parts.Count == 0) return true;
            foreach (var slot_part in slot_parts)
            {
                var eq_parts = slot_part.Split(',')
                    .Select(x => x.Trim())
                    .ToArray();
                var eq_info = new EquipmentPieceInfo();
                var rt = eq_info.Read(eq_parts);
                if (!rt) continue;
                EquipmentPieces[(int)eq_info.Slot] = eq_info;
            }
            return true;
        }

        public void MkaeReportPart(IForm2 formtools, int k)
        {
            formtools.WriteOutColoredText($"Equipment set {k}:\r\n", Color.GreenYellow);
            int colw_slotname = EquipmentPieces.Select(x => x.SlotName.Length).Max() + 1;
            int colw_setname = EquipmentPieces.Select(x => x.SetName?.Length ?? 0).Max() + 1;
            int colw_traitname = EquipmentPieces.Select(x => x.Trait?.Length ?? 0).Max() + 1;
            int colw_enchname = EquipmentPieces.Select(x => x.EnchantType?.Length ?? 0).Max() + 1;
            foreach (var eq in EquipmentPieces)
            {
                formtools.WriteOutColoredText($"  {eq.SlotName?.PadRight(colw_slotname)}", Color.Yellow);
                formtools.WriteOutColoredText($"{eq.SetName?.PadRight(colw_setname)}", Color.Beige);
                formtools.WriteOutText($"{eq.Trait?.PadRight(colw_traitname)}{eq.EnchantType?.Replace("_", " ")?.PadRight(colw_enchname)}\r\n");
                //eq.MkaeReportLine(formtools);
            }
        }
    }

    public class AbilityBar
    {
        public List<int> AbilityIds = new();
        public List<string> AbilityNames = new();

        public bool Equals(AbilityBar obj)
        {
            if (obj == null) return false;
            if (AbilityIds.Count != obj.AbilityIds.Count) return false;
            if (AbilityNames.Count != obj.AbilityNames.Count) return false;
            for (int i = 0; i < AbilityIds.Count; i++)
            {
                if (AbilityIds[i] != obj.AbilityIds[i]) return false;
            }
            return true;
        }

        public bool Read(string ab_part)
        {
            if (string.IsNullOrWhiteSpace(ab_part)) return true;
            AbilityIds = ab_part.Split(',')
                .Select(x => x.Trim())
                .Select(x => int.TryParse(x, out int id) ? id : 0)
                .ToList();
            AbilityNames = AbilityIds
                .Select(x => LogData.AbilityNamesById.TryGetValue(x, out string name) ? name : "unknown")
                .ToList();
            return true;
        }

    }

    public class AbilityBars
    {
        public AbilityBar AbilityBar1 = new();
        public AbilityBar AbilityBar2 = new();
        
        public bool Equals(AbilityBars obj)
        {
            if (obj == null) return false;
            if (!AbilityBar1.Equals(obj.AbilityBar1)) return false;
            if (!AbilityBar2.Equals(obj.AbilityBar2)) return false;
            return true;
        }

        public void MkaeReportPart(IForm2 formtools, int k)
        {
            formtools.WriteOutColoredBoldText($"Skill bars {k}:\r\n", Color.GreenYellow);
            formtools.WriteOutColoredBoldText($"Front bar:\r\n", Color.Yellow);
            foreach (var ab in AbilityBar1.AbilityNames)
            {
                formtools.WriteOutText($"    {ab}\r\n");
            }
            formtools.WriteOutColoredBoldText($"Back bar:\r\n", Color.Yellow);
            foreach (var ab in AbilityBar2.AbilityNames)
            {
                formtools.WriteOutText($"    {ab}\r\n");
            }
        }
    }

    public class LinePlayerInfo : LogLine
    {
        public int UnitId { get; set; }
        public EquipmentInfo EquipmentInfo { get; set; } = new();
        public AbilityBars AbilityBars { get; set; } = new();

        public override bool Read(string line)
        {
            if (line == null || line.Length <= 20) return false;

            int k1 = line.IndexOf(',');
            if (k1 == -1) return false;
            int k2 = line.IndexOf(',', k1 + 1);
            if (k2 == -1 || k2 == k1 + 1) return false;
            int k3 = line.IndexOf(',', k2 + 1);
            if (k3 == -1 || k3 == k2 + 1) return false;
            var ts_part = line.Substring(0, k1);
            var linetype_part = line.Substring(k1 + 1, k2 - k1 - 1);
            var unitid_part = line.Substring(k2 + 1, k3 - k2 - 1);
            TS = long.Parse(ts_part);
            if (!Enum.TryParse(linetype_part, out ELineType lineType))
                return false;
            LineType = lineType;
            UnitId = int.Parse(unitid_part);

            var line_parts = SplitBrackets(line);
            if (line_parts.Count != 5) return false;

            var rt = EquipmentInfo.Read(line_parts[2]);
            if (!rt) return false;
            rt = AbilityBars.AbilityBar1.Read(line_parts[3]);
            if (!rt) return false;
            rt = AbilityBars.AbilityBar2.Read(line_parts[4]);
            if (!rt) return false;

            return true;
        }


    }

}
