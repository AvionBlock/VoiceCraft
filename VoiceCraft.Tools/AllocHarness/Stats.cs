internal static class Stats
{
    public static NumericStats BuildLongStats(long[] values)
    {
        Array.Sort(values);
        var min = values[0];
        var max = values[^1];
        var average = values.Average();
        var median = CalculateMedian(values.Select(x => (double)x).ToArray());
        return new NumericStats(min, median, average, max);
    }

    public static NumericStats BuildDoubleStats(double[] values)
    {
        Array.Sort(values);
        var min = values[0];
        var max = values[^1];
        var average = values.Average();
        var median = CalculateMedian(values);
        return new NumericStats(min, median, average, max);
    }

    private static double CalculateMedian(double[] values)
    {
        var middle = values.Length / 2;
        return values.Length % 2 == 0
            ? (values[middle - 1] + values[middle]) / 2d
            : values[middle];
    }
}
