using System.Globalization;
using System.Text.RegularExpressions;

namespace FolderFlow.Core;

// Robocopy output is localized and interleaved when /MT is enabled. We only
// expose a footer count after recognizing the numeric table structure. We do
// not count filenames or treat per-file percentages as whole-job progress.
internal sealed class RobocopyOutputParser(bool allowFilePercent)
{
    private static readonly Regex NumericRow = new(
        @"^\s*[^:：\r\n]+[:：]\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex FileRecord = new(
        @"^\s*.*?\s+\d+\s+(?<path>[A-Za-z]:\\.*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Percent = new(@"^\s*(\d{1,3}(?:[.,]\d+)?)%\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private int numericRows;

    public FileSummary? Files { get; private set; }
    public string? FilePath { get; private set; }
    public double? FilePercent { get; private set; }

    public void Accept(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length >= 10 && trimmed.All(c => c == '-'))
        {
            numericRows = 0;
            return;
        }
        Match table = NumericRow.Match(line);
        if (table.Success)
        {
            long[] numbers = new long[6];
            for (int i = 0; i < numbers.Length; i++)
                if (!long.TryParse(table.Groups[i + 1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i])) return;
            numericRows++;
            if (numericRows == 2)
                Files = new FileSummary(numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5]);
            return;
        }

        Match file = FileRecord.Match(line);
        if (file.Success)
        {
            FilePath = file.Groups["path"].Value.Trim();
            FilePercent = null;
        }
        if (allowFilePercent)
        {
            Match percent = Percent.Match(line);
            if (percent.Success && double.TryParse(percent.Groups[1].Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out double value) && value <= 100)
                FilePercent = value;
        }
    }
}
