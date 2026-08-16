using System.Globalization;
using System.Reflection;
using System.Text;

namespace Sts2CharacterModel;

internal static class CsvWriter
{
    public static void Write<T>(string path, IEnumerable<T> records)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        writer.WriteLine(string.Join(',', props.Select(p => Escape(p.Name))));
        foreach (var record in records)
        {
            writer.WriteLine(string.Join(',', props.Select(p => Escape(Format(p.GetValue(record))))));
        }
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        decimal d => d.ToString("0.######", CultureInfo.InvariantCulture),
        double d => d.ToString("0.######", CultureInfo.InvariantCulture),
        float f => f.ToString("0.######", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        Enum e => e.ToString(),
        MetricVector m => string.Join(";", new[]
        {
            Pair("damage", m.Damage), Pair("block", m.Block), Pair("draw", m.Draw),
            Pair("energy", m.Energy), Pair("hpCost", m.SelfHpCost), Pair("maxHp", m.MaxHpGain),
            Pair("dpe", m.DamagePerEnergy), Pair("bpe", m.BlockPerEnergy),
            Pair("hand", m.NetHandDelta), Pair("energyFit", m.EnergyFeasibility),
            Pair("str", m.StrengthSensitivity), Pair("targets", m.TargetSensitivity),
            Pair("slope", m.ScalingSlope), Pair("fail", m.ConditionalFailureRate)
        }.Where(x => x.Length > 0)),
        DateTime dt => dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static string Pair(string name, decimal? value) =>
        value.HasValue ? $"{name}={value.Value.ToString("0.######", CultureInfo.InvariantCulture)}" : string.Empty;

    private static string Escape(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return value;
        }
        return '"' + value.Replace("\"", "\"\"") + '"';
    }
}
