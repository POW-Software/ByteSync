// using ByteSync.Interfaces;
// using ByteSyncCommon.Business.Sessions.Cloud;
// using ReactiveUI.Fody.Helpers;
// using Splat;
//
// namespace ByteSync.ViewModels.Sessions.Cloud.Managing;
//
// public class LinkingCaseViewModel
// {
//     private readonly ILocalizationService _localizationService;
//
//     public LinkingCaseViewModel(LinkingCases linkingCase, ILocalizationService? localizationService = null)
//     {
//         LinkingCase = linkingCase;
//         _localizationService = localizationService ?? Locator.Current.GetService<ILocalizationService>()!;
//
//         UpdateDescription();
//     }
//
//     [Reactive]
//     public LinkingCases LinkingCase { get; set; }
//     
//     [Reactive]
//     public string? Description { get; set; }
//     
//     internal void UpdateDescription()
//     {
//         Description = LinkingCase switch
//         {
//             LinkingCases.Indifferent => _localizationService["LinkingCases_Indifferent"],
//             LinkingCases.Same => _localizationService["LinkingCases_Same"],
//             _ => ""
//         };
//     }
// }