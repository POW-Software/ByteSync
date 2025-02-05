﻿using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Services.Sessions.Connecting;

public interface ICheckCloudSessionPasswordExchangeKeyService
{
    Task Process(AskJoinCloudSessionParameters request);
}