using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Xml.Linq;

namespace ESOLogs
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Defaultfont = tbOut.Font;
            Boldfont = new Font(Defaultfont, FontStyle.Bold);
            Config = Config.ReadXml();
            if (string.IsNullOrEmpty(Config.LogFileName?.Trim()))
            {
                var pt = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                pt = Path.Combine(pt, "Elder Scrolls Online", "live", "Logs", "Encounter.log");
                Config.LogFileName = pt;
                Config.WriteXml();
            }
            tbFileName.Text = Config.LogFileName;
        }

        Config Config = null;
        Font Defaultfont;
        Font Boldfont;

        void WriteOutBoldText(string text)
        {
            tbOut.SelectionFont = Boldfont;
            tbOut.AppendText(text);
            tbOut.SelectionFont = Defaultfont;
        }

        void WriteOutColoredText(string text, Color color)
        {
            var c = tbOut.SelectionColor;
            tbOut.SelectionColor = color;
            tbOut.AppendText(text);
            tbOut.SelectionColor = c;
        }

        void WriteOutColoredBoldText(string text, Color color)
        {
            var c = tbOut.SelectionColor;
            tbOut.SelectionColor = color;
            tbOut.SelectionFont = Boldfont;
            tbOut.AppendText(text);
            tbOut.SelectionColor = c;
            tbOut.SelectionFont = Defaultfont;
        }

        string FormatDuration(double seconds)
        {
            var ts = new TimeSpan(0, 0, (int)seconds);
            if (ts.Hours > 1)
                return $"{ts.Hours:00}{ts.Minutes:00}:{ts.Seconds:00}";
            return $"{ts.Minutes:00}:{ts.Seconds:00}";
        }

        public static string FormatDMG(int dmg)
        {
            var sdmg = dmg > 1000000 ?
                $"{(dmg / 1000000d):N2}M" :
                $"{(dmg / 1000d):N2}K";
            return sdmg;
        }

        void WriteReportFightHeader(string zonename, string bossname, int totaldmg, double duration)
        {
            WriteOutColoredBoldText(zonename, Color.Pink);
            tbOut.AppendText(", ");
            WriteOutColoredBoldText(bossname + "\r\n", Color.Yellow);
        }

        void WriteReportFightStartEndTime(DateTime started, DateTime ended, double duration)
        {
            WriteOutColoredBoldText("starded: ", Color.PeachPuff);
            WriteOutBoldText($"{started}");
            WriteOutColoredBoldText(", lasted: ", Color.PeachPuff);
            WriteOutBoldText($"{FormatDuration(duration)}\r\n");
        }

        void WriteReportLines(List<ReportLine> lines, int totaldmg, double duration)
        {
            var totaldeathcount = lines.Sum(x => x.Deaths);
            var rowtitle = new ReportLine()
            {
                Name = "Player ID",
                SDmg = "DMG",
                SDps = "DPS",
                SPercent = "(%)",
                SDeaths = "DEATHS"
            };
            var rowtotal = new ReportLine("TOTAL", totaldmg, totaldmg, duration, totaldeathcount);
            var lines2 = lines.Concat([rowtotal, rowtitle]).ToList();
            int namecolumnwidth = lines2.Select(x => x.Name.Length).Max();
            int totaldmgcolumnwidth = lines2.Select(x => x.SDmg.Length).Max() + 2;
            int dpscolumnwidth = lines2.Select(x => x.SDps.Length).Max() + 2;
            int percentcolumnwidth = lines2.Select(x => x.SPercent.Length).Max() + 2;
            int deathsolumnwidth = lines2.Select(x => x.SDeaths.Length).Max() + 2;

            WriteOutBoldText(rowtitle.Name.PadRight(namecolumnwidth));
            WriteOutBoldText(rowtitle.SDmg.PadLeft(totaldmgcolumnwidth));
            WriteOutBoldText(rowtitle.SDps.PadLeft(dpscolumnwidth));
            WriteOutBoldText(rowtitle.SPercent.PadLeft(percentcolumnwidth));
            WriteOutBoldText(rowtitle.SDeaths.PadLeft(deathsolumnwidth) + "\r\n");

            foreach (var line in lines)
            {
                WriteOutColoredBoldText(line.Name.PadRight(namecolumnwidth), Color.Beige);
                tbOut.AppendText(line.SDmg.PadLeft(totaldmgcolumnwidth));
                tbOut.AppendText(line.SDps.PadLeft(dpscolumnwidth));
                tbOut.AppendText(line.SPercent.PadLeft(percentcolumnwidth));
                tbOut.AppendText(line.SDeaths.PadLeft(deathsolumnwidth) + "\r\n");
            }

            WriteOutColoredBoldText("TOTAL".PadRight(namecolumnwidth), Color.Beige);
            WriteOutBoldText(rowtotal.SDmg.PadLeft(totaldmgcolumnwidth));
            WriteOutBoldText(rowtotal.SDps.PadLeft(dpscolumnwidth));
            WriteOutBoldText("100.00%".PadLeft(percentcolumnwidth));
            WriteOutBoldText(rowtotal.SDeaths.PadLeft(deathsolumnwidth) + "\r\n");
        }

        void WriteDelugeReport(FightData fightdata)
        {
            if (fightdata?.DelugeTracker == null || fightdata?.DelugeTracker.Data.Count == 0)
                return;
            var bombers = fightdata.DelugeTracker.Data
                .Where(x => x.Value.CountWasNotInWater > 0)
                .Select(x => (unit: GetById(x.Key), count: x.Value.CountWasNotInWater))
                .Where(x => x.unit != null)
                .OrderByDescending(x => x.count)
                .Select(x => (name: x.unit.DisplayName, scount: x.count.ToString()))
                .ToList();

            if (bombers.Count == 0) return;

            int namecolumnwidth = bombers.Select(x => x.name.Length).Max();
            int countcolumnwidth = bombers.Select(x => x.scount.Length).Max() + 2;

            WriteOutColoredBoldText(fightdata.ZoneName, Color.Pink);
            tbOut.AppendText(", ");
            WriteOutColoredBoldText("Deluge bombers\r\n", Color.Yellow);

            foreach (var bomber in bombers)
            {
                WriteOutColoredBoldText(bomber.name.PadRight(namecolumnwidth), Color.Beige);
                tbOut.AppendText(bomber.scount.PadLeft(countcolumnwidth) + "\r\n");
            }
            tbOut.AppendText("\r\n");

            Unit GetById(int id)
            {
                return fightdata.FightEvents.Keys.Where(x => x.Id == id).FirstOrDefault();
            }
        }

        class ReportLine
        {
            public string Name;
            public int Dmg;
            public double Percent;
            public int Deaths;
            public string SDmg;
            public string SDps;
            public string SPercent;
            public string SDeaths;

            public ReportLine() { }
            public ReportLine(string name, int dmg, int totaldmg, double duration, int deaths)
            {
                Name = name;
                Dmg = dmg;
                var dps = Math.Round((double)Dmg / duration / 1000d, 1);
                var perc = Math.Round((double)Dmg / (double)totaldmg * 100d, 2);
                SDmg = FormatDMG(Dmg);
                SDps = $"{dps:F1}k";
                SPercent = $"{perc:F2}%";
                Deaths = deaths;
                SDeaths = Deaths.ToString();
            }
        }

        void ReadLogFile(string logfilename)
        {
            var logdata = new LogData();
            logdata.ReadFile(logfilename);
            tbOut.Clear();
            if (logdata.FightData.Count == 0) return;

            foreach (var grzone in logdata.FightData.GroupBy(x => x.ZoneName))
            {
                var bossfights = grzone.Where(x => x.IsBossFight).ToList();
                var trashfights = grzone.Where(x => !x.IsBossFight).ToList();
                int totaldmg;

                foreach (var fight in bossfights)
                {
                    totaldmg = fight.FightEvents
                        .Where(x => x.Key.UnitType == EUnitType.PLAYER)
                        .Sum(x => x.Value.DamageDone);
                    var bfrows = fight.FightEvents
                        .Where(x => x.Key.UnitType == EUnitType.PLAYER)
                        .OrderByDescending(x => x.Value.DamageDone)
                        .Select(x => new ReportLine(x.Key.DisplayName, x.Value.DamageDone, 
                            totaldmg, fight.DurationInSeconds, x.Value.Deaths))
                        .ToList();
                    WriteReportFightHeader(fight.ZoneName, fight.BossName, totaldmg, fight.DurationInSeconds);
                    WriteReportFightStartEndTime(fight.Started, fight.Ended, fight.DurationInSeconds);
                    WriteReportLines(bfrows, totaldmg, fight.DurationInSeconds);
                    tbOut.AppendText("\r\n");

                    if (fight.DelugeTracker != null)
                        WriteDelugeReport(fight);
                }

                totaldmg = trashfights
                    .SelectMany(x => x.FightEvents)
                    .Where(x => x.Key.UnitType == EUnitType.PLAYER)
                    .Sum(x => x.Value.DamageDone);
                var totalduration = trashfights
                    .Sum(x => x.DurationInSeconds);
                var trrows = trashfights.SelectMany(x => x.FightEvents)
                    .Where(x => x.Key.UnitType == EUnitType.PLAYER)
                    .GroupBy(x => x.Key)
                    .Select(x => (
                        unit: x.Key, 
                        dmg: x.Sum(x => x.Value.DamageDone),
                        deaths: x.Sum(x => x.Value.Deaths)))
                    .OrderByDescending(x => x.dmg)
                    .Select(x => new ReportLine(x.unit.DisplayName, x.dmg, totaldmg, 
                        totalduration, x.deaths))
                    .ToList();
                WriteReportFightHeader(grzone.Key, "TRASH", totaldmg, totalduration);
                WriteReportLines(trrows, totaldmg, totalduration);
                tbOut.AppendText("\r\n");

                totaldmg = grzone
                    .SelectMany(x => x.FightEvents)
                    .Where(x => x.Key.UnitType == EUnitType.PLAYER)
                    .Sum(x => x.Value.DamageDone);
                totalduration = grzone
                    .Sum(x => x.DurationInSeconds);
                var trows = grzone
                    .SelectMany(x => x.FightEvents)
                    .Where(x => x.Key.UnitType == EUnitType.PLAYER)
                    .GroupBy(x => x.Key)
                    .Select(x => (
                        unit: x.Key, 
                        dmg: x.Sum(x => x.Value.DamageDone),
                        deaths: x.Sum(x => x.Value.Deaths)))
                    .OrderByDescending(x => x.dmg)
                    .Select(x => new ReportLine(x.unit.DisplayName, x.dmg, totaldmg, 
                        totalduration, x.deaths))
                    .ToList();
                WriteReportFightHeader(grzone.Key, "TOTAL", totaldmg, totalduration);
                WriteReportLines(trows, totaldmg, totalduration);
                tbOut.AppendText("\r\n");
            }
        }

        private void tsiRead_Click(object sender, EventArgs e)
        {
            var logfilename = Config?.LogFileName;
            if (string.IsNullOrEmpty(logfilename))
            {
                MessageBox.Show("Log file name not set.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!File.Exists(logfilename))
            {
                MessageBox.Show("Log file not found.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if ((new FileInfo(logfilename)).Length == 0)
            {
                MessageBox.Show("Log file is empty.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                ReadLogFile(logfilename);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to read Log file.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tsiSelectFile_Click(object sender, EventArgs e)
        {
            var pt1 = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var pt2 = Path.Combine(pt1, "Elder Scrolls Online", "live", "Logs");
            if (!Directory.Exists(pt2))
                pt2 = pt1;
            var fd = new OpenFileDialog()
            {
                InitialDirectory = pt2,
                Filter = "XML files (*.log)|*.log",
                Title = "Select encounter log file"
            };
            var rt = fd.ShowDialog(this);
            if (rt != DialogResult.OK) return;
            tbFileName.Text = fd.FileName;
            Config.LogFileName = fd.FileName;
            Config.WriteXml();
        }

        private void tbFileName_Leave(object sender, EventArgs e)
        {
            if (tbFileName.Text == Config?.LogFileName) return;
            Config.LogFileName = tbFileName.Text;
            Config.WriteXml();
        }

        private void tsiClearFile_Click(object sender, EventArgs e)
        {
            var logfilename = Config?.LogFileName;
            if (string.IsNullOrEmpty(logfilename))
            {
                MessageBox.Show("Log file name not set.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!File.Exists(logfilename))
            {
                MessageBox.Show("Log file not found.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                File.WriteAllText(logfilename, string.Empty);
                MessageBox.Show("Done!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to clear Log file.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}

