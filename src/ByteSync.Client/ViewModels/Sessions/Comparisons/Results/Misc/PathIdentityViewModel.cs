// using ByteSync.Business.Inventories;
// using ByteSync.Business.Sessions;
// using ByteSyncCommon.Business.Inventories;
// using ByteSyncCommon.Business.Sessions;
// using ByteSyncCommon.Business.Sessions.Cloud;
//
// namespace ByteSync.ViewModels.Sessions.Comparisons.Results.Misc;
//
// class PathIdentityViewModel
// {
//     public PathIdentityViewModel(PathIdentity pathIdentity, SessionSettings sessionSettings)
//     {
//         PathIdentity = pathIdentity;
//         SessionSettings = sessionSettings;
//     }
//
//     public PathIdentity PathIdentity { get; }
//     
//     public SessionSettings SessionSettings { get; }
//
//     public string FileName
//     {
//         get
//         {
//             return PathIdentity.FileName;
//         }
//     }
//
//     public string Path
//     {
//         get
//         {
//             return PathIdentity.LinkingKeyValue;
//             
//             // if (CloudSessionSettings.LinkingKey == LinkingKeys.FullPath)
//             // {
//             //     return PathIdentity.LinkingKeyValue;
//             // }
//             // else
//             // {
//             //     return PathIdentity.FileName;
//             // }
//         }
//     }
// }