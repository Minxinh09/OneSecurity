using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Ensures all DateTime values are treated as UTC when serializing to JSON.
/// SQLite stores DateTime without timezone info; this converter appends 'Z'
/// so browsers interpret timestamps correctly (UTC, not local time).
/// </summary>
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            return DateTime.UtcNow;

        // Parse the datetime and force it to UTC
        var dt = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        return dt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : dt.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Always write as UTC with 'Z' suffix: "2026-07-13T04:50:35.123Z"
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

/// <summary>
/// Nullable DateTime converter — same UTC logic as UtcDateTimeJsonConverter.
/// </summary>
public class UtcNullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value)) return null;

        var dt = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        return dt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : dt.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null) { writer.WriteNullValue(); return; }

        var utc = value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();

        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}
