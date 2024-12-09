using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ByteSync.Common.Business.Versions;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class SoftwareVersion
{
    public string ProductCode { get; set; } = null!;

    public string Version { get; set; } = null!;

    public PriorityLevel Level { get; set; }

    public List<SoftwareVersionFile> Files { get; set; } = new List<SoftwareVersionFile>();

    protected bool Equals(SoftwareVersion other)
    {
        return ProductCode == other.ProductCode && Version == other.Version && Level == other.Level;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SoftwareVersion)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ProductCode.GetHashCode();
            hashCode = (hashCode * 397) ^ Version.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Level;
            return hashCode;
        }
    }
}