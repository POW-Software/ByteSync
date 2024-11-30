using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Arguments;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Automating;
using ByteSync.Interfaces.Updates;
using PowSoftware.Common.Business.Versions;
using Serilog;
using Splat;

namespace ByteSync.Services.Automating;

public class CommandLineModeHandler : ICommandLineModeHandler
{
    private readonly IProfileAutoRunner _profileAutoRunner;

    public CommandLineModeHandler(IProfileAutoRunner? profileAutoRunner = null)
    {
        _profileAutoRunner = profileAutoRunner ?? Locator.Current.GetService<IProfileAutoRunner>()!;
        
        Arguments = Environment.GetCommandLineArgs();
    }

    private string[] Arguments { get; set; }

    public async Task<int> Operate()
    {
        int result;

        try
        {
            if (Arguments.Contains(RegularArguments.UPDATE))
            {
                Log.Information("Automatic processing: Starting update process");
                result = await OperateUpdateArgument();
            }
            else if (IsAutoRunProfile() && Arguments.Contains(RegularArguments.NO_GUI))
            {
                Log.Information("Automatic processing: Starting the execution of a profile");
                result = await _profileAutoRunner.OperateRunProfile(ProfileToRunName, JoinLobbyMode);
            }
            else
            {
                Log.Error("Automatic processing: Unable to determine the action to be performed");
                result = -2;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Automatic processing: An unexpected error has occurred");

            result = -1;
        }

        if (result == 0)
        {
            Log.Information("Automatic process finished with global success (code: {code})", result);
        }
        else
        {
            Log.Error("Automatic process failed (code: {code})", result);
        }

        return result;
    }
    
    public bool IsAutoLogin()
    {
        bool isAutoLogin = Arguments.Contains(RegularArguments.JOIN) || Arguments.Contains(RegularArguments.INVENTORY)
                                                           || Arguments.Contains(RegularArguments.SYNCHRONIZE);

        return isAutoLogin;
    }

    public bool IsAutoRunProfile()
    {
        return IsAutoLogin();
    }

    public string? ProfileToRunName
    {
        get
        {
            string[] argumentsAfter;
            if (Arguments.Contains(RegularArguments.JOIN))
            {
                argumentsAfter = GetArgumentsAfter(RegularArguments.JOIN);
            }
            else if (Arguments.Contains(RegularArguments.INVENTORY))
            {
                argumentsAfter = GetArgumentsAfter(RegularArguments.INVENTORY);
            }
            else if (Arguments.Contains(RegularArguments.SYNCHRONIZE))
            {
                argumentsAfter = GetArgumentsAfter(RegularArguments.SYNCHRONIZE);
            }
            else
            {
                return null;
            }

            if (argumentsAfter.Length > 0)
            {
                var profileName = argumentsAfter.FirstOrDefault(a => !a.StartsWith("-"));

                return profileName;
            }

            return null;
        }
    }

    public JoinLobbyModes? JoinLobbyMode
    {
        get
        {
            if (Arguments.Contains(RegularArguments.JOIN))
            {
                return JoinLobbyModes.Join;
            }
            else if (Arguments.Contains(RegularArguments.INVENTORY))
            {
                return JoinLobbyModes.RunInventory;
            }
            else if (Arguments.Contains(RegularArguments.SYNCHRONIZE))
            {
                return JoinLobbyModes.RunSynchronization;
            }

            return null;
        }
    }

    private async Task<int> OperateUpdateArgument()
    {
        Log.Information("Argument -update: trying to update automatically");

        var updateManager = Locator.Current.GetService<IUpdateService>()!;
        await updateManager.SearchNextAvailableVersionsAsync();

        var softwareVersions = updateManager.NextVersions.Items.ToList();

        SoftwareVersion? softwareVersion;
        if (Arguments.Any(a =>
                a.Equals(PriorityLevel.Mandatory.ToString(), StringComparison.InvariantCultureIgnoreCase)))
        {
            softwareVersion = softwareVersions.FirstOrDefault(v => v.Level == PriorityLevel.Mandatory);
        }
        else if (Arguments.Any(a =>
                     a.Equals(PriorityLevel.Recommended.ToString(), StringComparison.InvariantCultureIgnoreCase)))
        {
            softwareVersion = softwareVersions.FirstOrDefault(v => v.Level == PriorityLevel.Recommended);
        }
        else if (Arguments.Any(a =>
                     a.Equals(PriorityLevel.Optional.ToString(), StringComparison.InvariantCultureIgnoreCase)))
        {
            softwareVersion = softwareVersions.FirstOrDefault(v => v.Level == PriorityLevel.Optional);
        }
        else
        {
            softwareVersion = softwareVersions.FirstOrDefault();
        }

        await DebugUtils.DebugTaskDelay(5);

        if (softwareVersion != null)
        {
            Log.Information("Argument -update: trying to update to version {Version}", softwareVersion.Version);

            var isOK = await updateManager.UpdateAsync(softwareVersion, CancellationToken.None);

            if (isOK)
            {
                Log.Information("Argument -update: Update to version {Version} is successful", softwareVersion.Version);
            }
            else
            {
                Log.Error("Argument -update: An error occurred while upgrading to version {Version}", softwareVersion.Version);
            }
        }
        else
        {
            Log.Information("Argument -update: no available update found");
            Log.Information("Argument -update: shutting down this application instance");
        }

        return 0;
    }

    private string[] GetArgumentsAfter(string argument)
    {
        var index = Array.IndexOf(Arguments, argument);

        var result = Arguments.Skip(index + 1).ToArray();

        return result;
    }
}