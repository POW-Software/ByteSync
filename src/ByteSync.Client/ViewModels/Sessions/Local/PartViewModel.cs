// using System.Reactive;
// using System.Threading.Tasks;
// using ByteSync.Assets.Resources;
// using ByteSync.Business.PathItems;
// using ByteSync.Interfaces;
// using ByteSyncCommon.Business.Inventories;
// using ReactiveUI;
// using ReactiveUI.Fody.Helpers;
// using Splat;
//
// namespace ByteSync.ViewModels.Sessions.Local;
//
// public class PartViewModel : ViewModelBase
// {
//     public PartViewModel()
//     {
//         RemovePartCommand = ReactiveCommand.Create(RemovePart);
//     }
//
//     public PartViewModel(LocalSessionPartsViewModel localSessionPartsViewModel, PathItem pathItem,
//         ILocalizationService localizationService) : this()
//     {
//         LocalSessionPartsViewModel = localSessionPartsViewModel;
//         PathItem = pathItem;
//
//         UpdateElementType(localizationService);
//     }
//     
//     public ReactiveCommand<Unit, Unit> RemovePartCommand { get; set; }
//     
//     public LocalSessionPartsViewModel LocalSessionPartsViewModel { get; } = null!;
//     
//     public PathItem PathItem { get; } = null!;
//
//     [Reactive]
//     public string ElementType { get; set; }
//
//     private void RemovePart()
//     {
//         LocalSessionPartsViewModel.RemovePart(this);
//     }
//
//     public void OnLocaleChanged(ILocalizationService localizationService)
//     {
//         UpdateElementType(localizationService);
//     }
//
//     private void UpdateElementType(ILocalizationService localizationService)
//     {
//         if (PathItem.Type == FileSystemTypes.Directory)
//         {
//             ElementType = localizationService[nameof(Resources.General_Directory)];
//         }
//         else
//         {
//             ElementType = localizationService[nameof(Resources.General_File)];
//         }
//     }
// }