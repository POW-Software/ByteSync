using Avalonia;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ByteSync.ViewModels.Sessions.Synchronizations;
using ReactiveUI;

namespace ByteSync.Views.Sessions.Synchronizations
{
    public class SynchronizationMainView : ReactiveUserControl<SynchronizationMainViewModel>
    {
        public SynchronizationMainView()
        {
            this.WhenActivated(disposables =>
            {
                
        #if DEBUG
            this.WhenAnyValue(x => x.Bounds)
                .Subscribe(bounds => BoundsChanged(bounds))
                .DisposeWith(disposables);
        #endif
            
            });
            
            InitializeComponent();
        }
        
    #if DEBUG
        private void BoundsChanged(Rect bounds)
        {
            // 08/04/2022: Permet de récupérer facilement la hauteur si jamais ce panneau venait à changer de taille
        }
    #endif

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}