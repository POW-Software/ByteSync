using ByteSync.ServerCommon.Business.Repositories;

namespace ByteSync.ServerCommon.Tests.Helpers;

internal static class UpdateResultBuilder
{
    internal static UpdateEntityResult<T> BuildUpdateResult<T>(bool funcResult, T element, bool isTransaction)
    {
        if (funcResult)
        {
            if (isTransaction)
            {
                return new UpdateEntityResult<T>(element, UpdateEntityStatus.WaitingForTransaction);
            }
            else
            {
                return new UpdateEntityResult<T>(element, UpdateEntityStatus.Saved);
            }
        }
        else
        {
            return new UpdateEntityResult<T>(element, UpdateEntityStatus.NoOperation);
        }
    }
}