namespace ByteSync.Business.Arguments;

public static class DebugArguments
{
    internal const string SHOW_DEMO_DATA = "--show-demo-data";
        
    internal const string COPY_SESSION_CREDENTIALS_TO_CLIPBOARD = "--copy-cred";

    internal const string ADD_PATHITEM_TESTA = "--add-pathItem-testA";
    internal const string ADD_PATHITEM_TESTA1 = "--add-pathItem-testA1";
    internal const string ADD_PATHITEM_TESTB = "--add-pathItem-testB";
    internal const string ADD_PATHITEM_TESTB1 = "--add-pathItem-testB1";
    internal const string ADD_PATHITEM_TESTC = "--add-pathItem-testC";
    internal const string ADD_PATHITEM_TESTC1 = "--add-pathItem-testC1";
    internal const string ADD_PATHITEM_TESTD = "--add-pathItem-testD";
    internal const string ADD_PATHITEM_TESTD1 = "--add-pathItem-testD1";
    internal const string ADD_PATHITEM_TESTE = "--add-pathItem-testE";
    internal const string ADD_PATHITEM_TESTE1 = "--add-pathItem-testE1";

    internal const string FORCE_SLOW = "--force-slow";

    internal const string LADM_USE_STANDARD_APPDATA = "--ladm-use-standard-appdata";
    internal const string LADM_USE_APPDATA = "--ladm-use-appdata=";
    
    internal const string SET_APPLICATION_VERSION = "--set-application-version=";


    public static bool ForceSlow => Environment.GetCommandLineArgs().Contains(FORCE_SLOW);
}