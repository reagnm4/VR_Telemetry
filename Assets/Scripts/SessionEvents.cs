using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>Ordered event rows; equal timestamps are valid, sequence breaks ties.</summary>
public sealed class SessionEvents
{
    private readonly List<string> rows = new List<string>();
    private double previousTime;
    public int Count => rows.Count;
    public void Clear() { rows.Clear(); previousTime = 0; }
    private static string Escape(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
    public void Add(double seconds, string eventType, int trial, string payloadJson)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < previousTime ||
            trial < 0 || string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Invalid event time, type, or trial.");
        previousTime = seconds;
        rows.Add(seconds.ToString("R", CultureInfo.InvariantCulture) + "," + rows.Count + "," +
                 Escape(eventType) + "," + trial + "," + Escape(payloadJson));
    }
    public string GetCsv() => "t_sec,sequence,event_type,trial_number,payload_json\n" + string.Join("\n", rows);
}
