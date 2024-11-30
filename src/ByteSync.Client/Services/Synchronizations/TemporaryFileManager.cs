using System.IO;
using ByteSync.Interfaces.Controls.Synchronizations;

namespace ByteSync.Services.Synchronizations;

public class TemporaryFileManager : ITemporaryFileManager
{
    private readonly ILogger<TemporaryFileManager> _logger;

    public TemporaryFileManager(string destinationFullName, ILogger<TemporaryFileManager> logger)
    {
        _logger = logger;
        
        Reset(destinationFullName);
    }
    
    private void Reset(string destinationFullName)
    {
        DestinationFullName = destinationFullName;

        DestinationTemporaryPath = null;
        PreviousFileTemporaryPath = null;

        HasValidationStarted = false;
        PreviousFileExistsBeforeValidation = false;
        WasPreviousFileMovedToTemp = false;
        WasNewFileMovedFromTemp = false;
        WasPreviousFileDeleted = false;
    }

    public string DestinationFullName { get; set; } = null!;
    
    public string? DestinationTemporaryPath { get; set; }
    
    public string? PreviousFileTemporaryPath { get; set; }
    
    public bool HasValidationStarted { get; set; }
    
    public bool PreviousFileExistsBeforeValidation { get; set; }

    public bool WasPreviousFileMovedToTemp { get; set; }

    public bool WasNewFileMovedFromTemp { get; set; }

    public bool WasPreviousFileDeleted { get; set; }

    public string GetDestinationTemporaryPath()
    {
        DestinationTemporaryPath = GetTemporaryPath();

        return DestinationTemporaryPath;
    }

    private string GetTemporaryPath()
    {
        var cpt = 0;
        var temporaryPath = DestinationFullName + $".powtemp_t{cpt}$";
        
        while (File.Exists(temporaryPath))
        {
            cpt += 1;
            temporaryPath = DestinationFullName + $".powtemp_t{cpt}$";
        }

        return temporaryPath;
    }

    public void ValidateTemporaryFile()
    {
        HasValidationStarted = true;
        
        // On renomme le fichier existant en ".powtemp_t$"
        if (File.Exists(DestinationFullName))
        {
            PreviousFileExistsBeforeValidation = true;
            
            PreviousFileTemporaryPath = GetTemporaryPath();
            File.Move(DestinationFullName, PreviousFileTemporaryPath);
            
            WasPreviousFileMovedToTemp = true;
        }

        // On renomme le fichier fusionné dans son nom final
        File.Move(DestinationTemporaryPath!, DestinationFullName);
        
        WasNewFileMovedFromTemp = true;

        if (PreviousFileExistsBeforeValidation)
        {
            // On supprime l'ancien fichier existant
            File.Delete(PreviousFileTemporaryPath!);
        
            WasPreviousFileDeleted = true;
        }
    }

    public void TryRevertOnError(Exception exception)
    {
        try
        {
            _logger.LogWarning("An exception has occurred during validation operations on file {file}: {type} - {message}. Trying to revert operation", 
                DestinationFullName, exception.GetType().Name, exception.Message);
            if (!HasValidationStarted)
            {
                // On essaie de supprimer le fichier DestinationTemporaryPath s'il existe
                if (File.Exists(DestinationTemporaryPath))
                {
                    _logger.LogWarning("Deleting {file}", DestinationTemporaryPath);
                    File.Delete(DestinationTemporaryPath);
                }
                else
                {
                    _logger.LogWarning("{file} does not exist, nothing to do", DestinationTemporaryPath);
                }
            }
            else if (!PreviousFileExistsBeforeValidation)
            {
                if (!WasNewFileMovedFromTemp)
                {
                    _logger.LogWarning("The new version of the file may still exist at location {file}, " +
                                       "and it should be deleted manually", DestinationTemporaryPath);
                }
                else
                {
                    _logger.LogWarning("nothing seems to be undone here");
                }
            }
            else 
            {
                // On remonte de la fin jusqu'au début
                if (WasPreviousFileDeleted)
                {
                    _logger.LogWarning("Validation process has finish, nothing can be done on {file}", DestinationFullName);
                }
                else if (WasNewFileMovedFromTemp)
                {
                    _logger.LogWarning("The previous version of the file may still exist at location {file}, " +
                                       "and it should be deleted manually", PreviousFileTemporaryPath);
                }
                else if (WasPreviousFileMovedToTemp)
                {
                    _logger.LogWarning("The previous version of the file may still exist at location {previous}, " +
                                       "while the new version of the file may be found at location {new}", 
                        PreviousFileTemporaryPath, DestinationTemporaryPath);
                }
                else
                {
                    _logger.LogWarning("The previous version of the file was not successfully moved from {previous}. " +
                                       "The new temporary file will be deleted at {new}", DestinationFullName, DestinationTemporaryPath);

                    // On essaie de supprimer le fichier DestinationTemporaryPath s'il existe
                    if (File.Exists(DestinationTemporaryPath))
                    {
                        _logger.LogWarning("Deleting {file}", DestinationTemporaryPath);
                        File.Delete(DestinationTemporaryPath);
                    }
                    else
                    {
                        _logger.LogWarning("{file} does not exist, nothing to do", DestinationTemporaryPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A new exception has occurred, while trying to undo the effects on file {file}", DestinationFullName);
        }
    }
}