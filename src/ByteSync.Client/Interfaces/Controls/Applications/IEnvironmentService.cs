﻿using ByteSync.Business.Misc;
using ByteSync.Common.Business.Misc;

namespace ByteSync.Interfaces.Controls.Applications;

public interface IEnvironmentService
{
    public bool OperateCommandLine { get; }
    
    public OperationMode OperationMode { get; }
    
    public ExecutionMode ExecutionMode { get; }
    
    OSPlatforms OSPlatform { get; }
    
    string[] Arguments { get; set; }

    public bool IsAutoLogin();

    public bool IsAutoRunProfile();
        
    string AssemblyFullName { get; }

    string MachineName { get; }
    
    public bool IsPortableApplication { get; }
    
    Version CurrentVersion { get; }
    
    string ClientId { get; }
    
    string ClientInstanceId { get; }
}