using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Aiursoft.MusicExam.Services;

public class NaturalSortComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var xParts = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
        var yParts = Regex.Split(y.Replace(" ", ""), "([0-9]+)");

        for (var i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
        {
            if (xParts[i] != yParts[i])
            {
                if (long.TryParse(xParts[i], out var xVal) && long.TryParse(yParts[i], out var yVal))
                {
                    return xVal.CompareTo(yVal);
                }

                return string.Compare(xParts[i], yParts[i], StringComparison.OrdinalIgnoreCase);
            }
        }

        return x.Length.CompareTo(y.Length);
    }
}
