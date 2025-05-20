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
    // Tags collection
    public static readonly StyledProperty<ObservableCollection<string>> TagsProperty =
        AvaloniaProperty.Register<TagEditor, ObservableCollection<string>>(
            nameof(Tags),
            defaultValue: new ObservableCollection<string>());

    // Tag separator (space by default)
    public static readonly StyledProperty<char> TagSeparatorProperty =
        AvaloniaProperty.Register<TagEditor, char>(
            nameof(TagSeparator),
            defaultValue: ' ');

    // Property to store the text being typed
    public static readonly StyledProperty<string> CurrentTextProperty =
        AvaloniaProperty.Register<TagEditor, string>(
            nameof(CurrentText),
            defaultValue: string.Empty);

    // Placeholder to display when control is empty
    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<TagEditor, string>(
            nameof(Watermark),
            defaultValue: "Enter tags...");

    // Background color for tags
    public static readonly StyledProperty<IBrush> TagBackgroundProperty =
        AvaloniaProperty.Register<TagEditor, IBrush>(
            nameof(TagBackground),
            defaultValue: Brushes.LightBlue);

    // Text color for tags
    public static readonly StyledProperty<IBrush> TagForegroundProperty =
        AvaloniaProperty.Register<TagEditor, IBrush>(
            nameof(TagForeground),
            defaultValue: Brushes.Black);

    // Event when a tag is added
    public static readonly RoutedEvent<RoutedEventArgs> TagAddedEvent =
        RoutedEvent.Register<TagEditor, RoutedEventArgs>(
            nameof(TagAdded),
            RoutingStrategies.Bubble);

    // Event when a tag is removed
    public static readonly RoutedEvent<RoutedEventArgs> TagRemovedEvent =
        RoutedEvent.Register<TagEditor, RoutedEventArgs>(
            nameof(TagRemoved),
            RoutingStrategies.Bubble);
                
    // Event when the tags collection changes
    public static readonly RoutedEvent<RoutedEventArgs> TagsChangedEvent =
        RoutedEvent.Register<TagEditor, RoutedEventArgs>(
            nameof(TagsChanged),
            RoutingStrategies.Bubble);

    // Delay after which current text is considered as a tag (in ms)
    public static readonly StyledProperty<int> AutoCommitDelayProperty =
        AvaloniaProperty.Register<TagEditor, int>(
            nameof(AutoCommitDelay),
            defaultValue: 800);

    // Property to filter tags (accept or reject)
    public static readonly StyledProperty<Func<string, bool>> TagFilterProperty =
        AvaloniaProperty.Register<TagEditor, Func<string, bool>>(
            nameof(TagFilter),
            defaultValue: (tag) => true);

    // Properties accessible from XAML
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

    // Events
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

    // Template parts
    private TextBox? _textBox;
    private ItemsControl? _tagsPanel;
    private ScrollViewer? _tagsScroll;
    
    private DispatcherTimer _commitTimer;

    // Constructor
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
        _tagsScroll = e.NameScope.Find<ScrollViewer>("PART_TagsScroll");
        

        if (_tagsPanel != null)
        {
            // Dynamically update max width
            this.GetObservable(BoundsProperty).Subscribe(bounds =>
            {
                var totalWidth = bounds.Width;
                var maxTagsWidth = Math.Max(0, totalWidth - 80);
                _tagsPanel.MaxWidth = maxTagsWidth;
            });
        }

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
            
        // If text contains a separator, create a tag
        if (text.Contains(TagSeparator))
        {
            string[] parts = text.Split(TagSeparator);
                
            // Process each part except the last one (which will be the new text)
            for (int i = 0; i < parts.Length - 1; i++)
            {
                AddTag(parts[i]);
            }
                
            // Update current text
            _textBox.Text = parts[parts.Length - 1];
            _textBox.CaretIndex = _textBox.Text.Length;
        }
        else
        {
            // Reset timer to automatically create a tag after delay
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
        if (_textBox == null)
        {
            return;
        }
            
        // If Backspace is pressed with empty textBox, delete last tag
        if (e.Key == Key.Back && string.IsNullOrEmpty(_textBox.Text) && Tags.Count > 0)
        {
            RemoveLastTag();
            e.Handled = true;
        }
        // If Enter is pressed, create a tag with current text
        else if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(_textBox.Text))
        {
            CommitCurrentText();
            e.Handled = true;
        }
        // If comma is pressed, create a tag with current text
        else if ((e.Key == Key.OemComma) && !string.IsNullOrWhiteSpace(_textBox.Text))
        {
            CommitCurrentText();
            e.Handled = true;
        }
        // If Tab is pressed, try to create a tag with current text
        else if (e.Key == Key.Tab && !string.IsNullOrWhiteSpace(_textBox.Text))
        {
            CommitCurrentText();
            e.Handled = true;
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

    // Public method to remove a specific tag
    public void RemoveTag(string tag)
    {
        if (Tags.Contains(tag))
        {
            Tags.Remove(tag);
            RaiseEvent(new RoutedEventArgs(TagRemovedEvent, this));
        }
    }
        
    // Public method to add a tag
    public bool AddTagManually(string tag)
    {
        return AddTag(tag);
    }
        
    // Public method to clear all tags
    public void ClearTags()
    {
        Tags.Clear();
        RaiseEvent(new RoutedEventArgs(TagRemovedEvent, this));
    }
        
    // Returns a string containing all tags separated by the separator
    public string GetTagsString()
    {
        return string.Join(TagSeparator.ToString(), Tags);
    }
    
    public void RemoveTag(object? parameter)
    {
        if (parameter is string tag)
        {
            RemoveTag(tag);
        }
    }
}
