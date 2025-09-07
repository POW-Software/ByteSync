namespace ByteSync.Business.Arguments;

public static class DebugArguments
{
    internal const string SHOW_DEMO_DATA = "--show-demo-data";
        
    internal const string COPY_SESSION_CREDENTIALS_TO_CLIPBOARD = "--copy-cred";

    internal const string ADD_DATASOURCE_TESTA = "--add-datasource-testA";
    internal const string ADD_DATASOURCE_TESTA1 = "--add-datasource-testA1";
    internal const string ADD_DATASOURCE_TESTB = "--add-datasource-testB";
    internal const string ADD_DATASOURCE_TESTB1 = "--add-datasource-testB1";
    internal const string ADD_DATASOURCE_TESTC = "--add-datasource-testC";
    internal const string ADD_DATASOURCE_TESTC1 = "--add-datasource-testC1";
    internal const string ADD_DATASOURCE_TESTD = "--add-datasource-testD";
    internal const string ADD_DATASOURCE_TESTD1 = "--add-datasource-testD1";
    internal const string ADD_DATASOURCE_TESTE = "--add-datasource-testE";
    internal const string ADD_DATASOURCE_TESTE1 = "--add-datasource-testE1";

    internal const string FORCE_SLOW = "--force-slow";

    internal const string LADM_USE_STANDARD_APPDATA = "--ladm-use-standard-appdata";
    internal const string LADM_USE_APPDATA = "--ladm-use-appdata=";
    
    internal const string SET_APPLICATION_VERSION = "--set-application-version=";

    internal const string UPLOAD_FORCE_UPSCALE = "--upload-force-upscale";
    internal const string UPLOAD_FORCE_DOWNSCALE = "--upload-force-downscale";
    
    public static bool UploadForceUpscale => Environment.GetCommandLineArgs().Contains(UPLOAD_FORCE_UPSCALE);
    
    public static bool UploadForceDownscale => Environment.GetCommandLineArgs().Contains(UPLOAD_FORCE_DOWNSCALE);

    public static bool ForceSlow => Environment.GetCommandLineArgs().Contains(FORCE_SLOW);
}