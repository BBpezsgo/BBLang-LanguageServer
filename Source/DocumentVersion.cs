namespace LanguageServer;

public readonly struct DocumentVersion : IEquatable<DocumentVersion>
{
    public readonly int Version;
    public readonly DateTimeOffset Time;

    public DocumentVersion(int version, DateTimeOffset time)
    {
        Version = version;
        Time = time;
    }

    public static DocumentVersion Now(int version = 0) => new(version, DateTimeOffset.UtcNow);
    public static DocumentVersion Zero(int version = 0) => new(version, default);

    public ulong ToULong() => (ulong)Time.Ticks + (ulong)Version;
    public override string ToString() => Time == default ? Version.ToString() : $"{Version} ({Time})";

    public override bool Equals(object? obj) => obj is DocumentVersion other && Equals(other);
    public bool Equals(DocumentVersion other) => Version == other.Version && Time.Equals(other.Time);
    public override int GetHashCode() => HashCode.Combine(Version, Time);

    public static bool operator ==(DocumentVersion left, DocumentVersion right) => left.Equals(right);
    public static bool operator !=(DocumentVersion left, DocumentVersion right) => !left.Equals(right);
}
