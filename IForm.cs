using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESOLogs
{
    public interface IForm
    {
        void WriteOutText(string text);
        void WriteOutBoldText(string text);
        void WriteOutColoredText(string text, Color color);
        void WriteOutColoredBoldText(string text, Color color);
        string FormatDuration(double seconds);
        string FormatDMG(int dmg);
    }

    public interface IForm2
    {
        void WriteOutText(string text);
        void WriteOutBoldText(string text);
        void WriteOutColoredText(string text, Color color);
        void WriteOutColoredBoldText(string text, Color color);
    }

}
