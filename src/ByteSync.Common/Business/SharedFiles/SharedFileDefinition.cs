using System;
using System.Collections.Generic;
using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Helpers;

namespace ByteSync.Common.Business.SharedFiles;

public class SharedFileDefinition
{
    public SharedFileDefinition()
    {
        Id = Guid.NewGuid().ToString();

        ActionsGroupIds = new List<string>();
    }

    public string Id { get; set; }

    public string SessionId { get; set; }

    public string ClientInstanceId { get; set; }

    public SharedFileTypes SharedFileType { get; set; }

    public List<string>? ActionsGroupIds { get; set; }
        
    public string AdditionalName { get; set; }

    public byte[] IV { get; set; }

    public bool IsInventory
    {
        get
        {
            return SharedFileType.In(SharedFileTypes.BaseInventory, SharedFileTypes.FullInventory);
        }
    }

    public bool IsSynchronization
    {
        get
        {
            return SharedFileType.In(SharedFileTypes.FullSynchronization, SharedFileTypes.DeltaSynchronization);
        }
    }

    public bool IsDeltaSynchronization
    {
        get
        {
            return SharedFileType.In(SharedFileTypes.DeltaSynchronization);
        }
    }

    public bool IsSynchronizationStartData
    {
        get
        {
            return SharedFileType.In(SharedFileTypes.SynchronizationStartData);
        }
    }
        
    public bool IsProfileDetails
    {
        get
        {
            return SharedFileType.In(SharedFileTypes.ProfileDetails);
        }
    }

    public bool IsMultiFileZip { get; set; }
    
    public long UploadedFileLength { get; set; }

    protected bool Equals(SharedFileDefinition other)
    {
        return SessionId == other.SessionId && ClientInstanceId == other.ClientInstanceId && SharedFileType == other.SharedFileType && Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SharedFileDefinition) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (SessionId != null ? SessionId.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ClientInstanceId != null ? ClientInstanceId.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (int) SharedFileType;
            hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
            return hashCode;
        }
    }

    public string GetFileName(int partNumber)
    {
        switch (SharedFileType)
        {
            case SharedFileTypes.BaseInventory:
                return $"base_inventory_{AdditionalName}.part{partNumber}";
            case SharedFileTypes.FullInventory:
                return $"full_inventory_{AdditionalName}.part{partNumber}";
            case SharedFileTypes.FullSynchronization:
            case SharedFileTypes.DeltaSynchronization:
                return $"synchronization_{Id}.part{partNumber}";
            case SharedFileTypes.SynchronizationStartData:
                return $"synchronization_start_data.part{partNumber}";
            case SharedFileTypes.ProfileDetails:
                return $"profile_details_{AdditionalName}.part{partNumber}";
            default:
                throw new ArgumentOutOfRangeException(nameof(SharedFileType), "unable to handle such type here");
        }

        throw new ApplicationException($"ShareFileType unknown : {SharedFileType}");
    }

    public bool IsCreatedBy(ByteSyncEndpoint currentEndPoint)
    {
        return ClientInstanceId.Equals(currentEndPoint.ClientInstanceId);
    }
}