namespace ByteSync.Interfaces.Controls.Bootstrapping;

public interface IBootstrapper
{
    public void Start();
    
    public Action? AttachConsole { get; set; }
    
    public void AfterFrameworkInitializationCompleted();
}