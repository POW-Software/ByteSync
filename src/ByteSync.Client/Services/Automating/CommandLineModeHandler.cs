using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Arguments;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Versions;
using ByteSync.Common.Helpers;
using ByteSync.Interfaces.Controls.Automating;
using ByteSync.Interfaces.Repositories;
using ByteSync.Interfaces.Updates;

namespace ByteSync.Services.Automating;

public class CommandLineModeHandler : ICommandLineModeHandler
{
    private readonly IProfileAutoRunner _profileAutoRunner;
    private readonly ISearchUpdateService _searchUpdateService;
    private readonly IUpdateService _updateService;
    private readonly IAvailableUpdateRepository _availableUpdateRepository;
    private readonly ILogger<CommandLineModeHandler> _logger;

    public CommandLineModeHandler(IProfileAutoRunner profileAutoRunner, ISearchUpdateService searchUpdateService, 
        IUpdateService updateService, IAvailableUpdateRepository availableUpdateRepository, ILogger<CommandLineModeHandler> logger)
    {
        _profileAutoRunner = profileAutoRunner;
        _searchUpdateService = searchUpdateService;
        _updateService = updateService;
        _availableUpdateRepository = availableUpdateRepository;
        _logger = logger;
        
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
                _logger.LogInformation("Automatic processing: Starting update process");
                result = await OperateUpdateArgument();
            }
            else if (IsAutoRunProfile() && Arguments.Contains(RegularArguments.NO_GUI))
            {
                _logger.LogInformation("Automatic processing: Starting the execution of a profile");
                result = await _profileAutoRunner.OperateRunProfile(ProfileToRunName, JoinLobbyMode);
            }
            else
            {
                _logger.LogError("Automatic processing: Unable to determine the action to be performed");
                result = -2;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Automatic processing: An unexpected error has occurred");

            result = -1;
        }

        if (result == 0)
        {
            _logger.LogInformation("Automatic process finished with global success (code: {code})", result);
        }
        else
        {
            _logger.LogError("Automatic process failed (code: {code})", result);
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
        _logger.LogInformation("Argument -update: trying to update automatically");
        
        await _searchUpdateService.SearchNextAvailableVersionsAsync();

        var softwareVersions = _availableUpdateRepository.Elements.ToList();

        SoftwareVersion? softwareVersion;
        if (Arguments.Any(a =>
                a.Equals(PriorityLevel.Minimal.ToString(), StringComparison.InvariantCultureIgnoreCase)))
        {
            softwareVersion = softwareVersions.FirstOrDefault(v => v.Level == PriorityLevel.Minimal);
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
            _logger.LogInformation("Argument -update: trying to update to version {Version}", softwareVersion.Version);

            var isOK = await _updateService.UpdateAsync(softwareVersion, CancellationToken.None);

            if (isOK)
            {
                _logger.LogInformation("Argument -update: Update to version {Version} is successful", softwareVersion.Version);
            }
            else
            {
                _logger.LogError("Argument -update: An error occurred while upgrading to version {Version}", softwareVersion.Version);
            }
        }
        else
        {
            _logger.LogInformation("Argument -update: no available update found");
            _logger.LogInformation("Argument -update: shutting down this application instance");
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