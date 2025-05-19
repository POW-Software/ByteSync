using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace ByteSync.Views.Misc;

public class TagEditor : TemplatedControl
{
    // Collection de tags
    public static readonly StyledProperty<ObservableCollection<string>> TagsProperty =
        AvaloniaProperty.Register<TagEditor, ObservableCollection<string>>(
            nameof(Tags),
            defaultValue: new ObservableCollection<string>());

    // Séparateur de tags (espace par défaut)
    public static readonly StyledProperty<char> TagSeparatorProperty =
        AvaloniaProperty.Register<TagEditor, char>(
            nameof(TagSeparator),
            defaultValue: ' ');

    // Propriété pour stocker le texte en cours de saisie
    public static readonly StyledProperty<string> CurrentTextProperty =
        AvaloniaProperty.Register<TagEditor, string>(
            nameof(CurrentText),
            defaultValue: string.Empty);

    // Placeholder à afficher quand le contrôle est vide
    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<TagEditor, string>(
            nameof(Watermark),
            defaultValue: "Entrez des tags...");

    // Couleur d'arrière-plan des tags
    public static readonly StyledProperty<IBrush> TagBackgroundProperty =
        AvaloniaProperty.Register<TagEditor, IBrush>(
            nameof(TagBackground),
            defaultValue: Brushes.LightBlue);

    // Couleur de texte des tags
    public static readonly StyledProperty<IBrush> TagForegroundProperty =
        AvaloniaProperty.Register<TagEditor, IBrush>(
            nameof(TagForeground),
            defaultValue: Brushes.Black);

    // Événement quand un tag est ajouté
    public static readonly RoutedEvent<RoutedEventArgs> TagAddedEvent =
        RoutedEvent.Register<TagEditor, RoutedEventArgs>(
            nameof(TagAdded),
            RoutingStrategies.Bubble);

    // Événement quand un tag est supprimé
    public static readonly RoutedEvent<RoutedEventArgs> TagRemovedEvent =
        RoutedEvent.Register<TagEditor, RoutedEventArgs>(
            nameof(TagRemoved),
            RoutingStrategies.Bubble);
                
    // Événement quand la collection de tags change
    public static readonly RoutedEvent<RoutedEventArgs> TagsChangedEvent =
        RoutedEvent.Register<TagEditor, RoutedEventArgs>(
            nameof(TagsChanged),
            RoutingStrategies.Bubble);

    // Délai après lequel le texte actuel est considéré comme un tag (en ms)
    public static readonly StyledProperty<int> AutoCommitDelayProperty =
        AvaloniaProperty.Register<TagEditor, int>(
            nameof(AutoCommitDelay),
            defaultValue: 800);

    // Propriété pour filtrer les tags (accepter ou rejeter)
    public static readonly StyledProperty<Func<string, bool>> TagFilterProperty =
        AvaloniaProperty.Register<TagEditor, Func<string, bool>>(
            nameof(TagFilter),
            defaultValue: (tag) => true);

    // Propriétés accessibles depuis XAML
    public ObservableCollection<string> Tags
    {
        get => GetValue(TagsProperty);
        set
        {
            if (GetValue(TagsProperty) != value)
            {
                if (GetValue(TagsProperty) is ObservableCollection<string> oldCollection)
                {
                    oldCollection.CollectionChanged -= Tags_CollectionChanged;
                }

                SetValue(TagsProperty, value);

                if (value != null)
                {
                    value.CollectionChanged += Tags_CollectionChanged;
                }
            }
        }
    }

    public char TagSeparator
    {
        get => GetValue(TagSeparatorProperty);
        set => SetValue(TagSeparatorProperty, value);
    }

    public string CurrentText
    {
        get => GetValue(CurrentTextProperty);
        set => SetValue(CurrentTextProperty, value);
    }

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public IBrush TagBackground
    {
        get => GetValue(TagBackgroundProperty);
        set => SetValue(TagBackgroundProperty, value);
    }

    public IBrush TagForeground
    {
        get => GetValue(TagForegroundProperty);
        set => SetValue(TagForegroundProperty, value);
    }
        
    public int AutoCommitDelay
    {
        get => GetValue(AutoCommitDelayProperty);
        set => SetValue(AutoCommitDelayProperty, value);
    }
        
    public Func<string, bool> TagFilter
    {
        get => GetValue(TagFilterProperty);
        set => SetValue(TagFilterProperty, value);
    }

    // Événements
    public event EventHandler<RoutedEventArgs> TagAdded
    {
        add => AddHandler(TagAddedEvent, value);
        remove => RemoveHandler(TagAddedEvent, value);
    }

    public event EventHandler<RoutedEventArgs> TagRemoved
    {
        add => AddHandler(TagRemovedEvent, value);
        remove => RemoveHandler(TagRemovedEvent, value);
    }
        
    public event EventHandler<RoutedEventArgs> TagsChanged
    {
        add => AddHandler(TagsChangedEvent, value);
        remove => RemoveHandler(TagsChangedEvent, value);
    }

    // Parties du template
    private TextBox _textBox;
    private ItemsControl _tagsPanel;
    private DispatcherTimer _commitTimer;

    // Constructeur
    public TagEditor()
    {
        Tags = new ObservableCollection<string>();
        _commitTimer = new DispatcherTimer 
        { 
            Interval = TimeSpan.FromMilliseconds(AutoCommitDelay) 
        };
        _commitTimer.Tick += CommitTimer_Tick;
            
        Tags.CollectionChanged += Tags_CollectionChanged;
    }

    private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(TagsChangedEvent, this));
    }

    private void CommitTimer_Tick(object sender, EventArgs e)
    {
        _commitTimer.Stop();
        CommitCurrentText();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
        _tagsPanel = e.NameScope.Find<ItemsControl>("PART_TagsPanel");

        if (_textBox != null)
        {
            _textBox.KeyDown += TextBox_KeyDown;
            _textBox.TextInput += TextBox_TextChanged;
            _textBox.LostFocus += TextBox_LostFocus;
        }
    }

    private void TextBox_TextChanged(object sender, TextInputEventArgs e)
    {
        if (_textBox == null) return;
            
        string text = _textBox.Text;
        CurrentText = text;
            
        // Si le texte contient un séparateur, on crée un tag
        if (text.Contains(TagSeparator))
        {
            string[] parts = text.Split(TagSeparator);
                
            // Traiter chaque partie sauf la dernière (qui sera le nouveau texte)
            for (int i = 0; i < parts.Length - 1; i++)
            {
                AddTag(parts[i]);
            }
                
            // Mettre à jour le texte courant
            _textBox.Text = parts[parts.Length - 1];
            _textBox.CaretIndex = _textBox.Text.Length;
        }
        else
        {
            // Réinitialiser le timer pour créer un tag automatiquement après le délai
            if (!string.IsNullOrWhiteSpace(text) && AutoCommitDelay > 0)
            {
                _commitTimer.Stop();
                _commitTimer.Interval = TimeSpan.FromMilliseconds(AutoCommitDelay);
                _commitTimer.Start();
            }
        }
            
        CurrentText = _textBox.Text;
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (_textBox == null) return;
            
        // Si on appuie sur Backspace avec un textBox vide, supprimer le dernier tag
        if (e.Key == Key.Back && string.IsNullOrEmpty(_textBox.Text) && Tags.Count > 0)
        {
            RemoveLastTag();
            e.Handled = true;
        }
        // Si on appuie sur Entrée, créer un tag avec le texte actuel
        else if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(_textBox.Text))
        {
            CommitCurrentText();
            e.Handled = true;
        }
        // Si on appuie sur Virgule, créer un tag avec le texte actuel
        else if ((e.Key == Key.OemComma) && !string.IsNullOrWhiteSpace(_textBox.Text))
        {
            CommitCurrentText();
            e.Handled = true;
        }
        // Si on appuie sur Tab, essaie de créer un tag avec le texte actuel
        else if (e.Key == Key.Tab && !string.IsNullOrWhiteSpace(_textBox.Text))
        {
            CommitCurrentText();
            // Ne pas capturer l'événement pour permettre au Tab de fonctionner normalement
        }
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitCurrentText();
    }
        
    private void CommitCurrentText()
    {
        if (_textBox != null && !string.IsNullOrWhiteSpace(_textBox.Text))
        {
            AddTag(_textBox.Text);
            _textBox.Text = string.Empty;
        }
    }
        
    private bool AddTag(string tagText)
    {
        tagText = tagText.Trim();
            
        if (string.IsNullOrWhiteSpace(tagText))
            return false;
                
        if (!TagFilter(tagText))
            return false;
                
        if (!Tags.Contains(tagText))
        {
            Tags.Add(tagText);
            RaiseEvent(new RoutedEventArgs(TagAddedEvent, this));
            return true;
        }
            
        return false;
    }
        
    private void RemoveLastTag()
    {
        if (Tags.Count > 0)
        {
            Tags.RemoveAt(Tags.Count - 1);
            RaiseEvent(new RoutedEventArgs(TagRemovedEvent, this));
        }
    }

    // Méthode publique pour supprimer un tag spécifique
    public void RemoveTag(string tag)
    {
        if (Tags.Contains(tag))
        {
            Tags.Remove(tag);
            RaiseEvent(new RoutedEventArgs(TagRemovedEvent, this));
        }
    }
        
    // Méthode publique pour ajouter un tag
    public bool AddTagManually(string tag)
    {
        return AddTag(tag);
    }
        
    // Méthode publique pour effacer tous les tags
    public void ClearTags()
    {
        Tags.Clear();
        RaiseEvent(new RoutedEventArgs(TagRemovedEvent, this));
    }
        
    // Retourne une chaîne contenant tous les tags séparés par le séparateur
    public string GetTagsString()
    {
        return string.Join(TagSeparator.ToString(), Tags);
    }
}