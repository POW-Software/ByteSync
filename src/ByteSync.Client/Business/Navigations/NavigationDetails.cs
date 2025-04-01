namespace ByteSync.Business.Navigations;

public class NavigationDetails
{
    public NavigationDetails()
    {

    }
        
    public NavigationPanel NavigationPanel { get; init; }
        
    public string? TitleLocalizationName { get; init; }

    public string? IconName { get; init; }
        
    public bool IsHome
    {
        get
        {
            return NavigationPanel == NavigationPanel.Home;
        }
    }

    public string ApplicableIconName
    {
        get
        {
            if (IconName.IsNullOrEmpty())
            {
                return "None";
            }
            else
            {
                return IconName!;
            }
        }
    }
}