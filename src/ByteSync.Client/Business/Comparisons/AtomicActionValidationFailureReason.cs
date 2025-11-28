namespace ByteSync.Business.Comparisons;

public enum AtomicActionValidationFailureReason
{
    // Basic Consistency - Operation Type Compatibility
    SynchronizeOperationOnDirectoryNotAllowed = 1,
    CreateOperationOnFileNotAllowed = 2,
    
    // Basic Consistency - Source Requirements  
    SourceRequiredForSynchronizeOperation = 10,
    SourceNotAllowedForDeleteOperation = 11,
    SourceNotAllowedForCreateOperation = 12,
    
    // Basic Consistency - Destination Requirements
    DestinationRequiredForSynchronizeOperation = 20,
    DestinationRequiredForDeleteOperation = 21,
    DestinationRequiredForCreateOperation = 22,
    
    // Advanced Consistency - Source Issues
    InvalidSourceCount = 30,
    SourceHasAnalysisError = 31,
    SourceNotAccessible = 32,
    
    // Advanced Consistency - Target Issues
    TargetFileNotPresent = 40,
    AtLeastOneTargetsHasAnalysisError = 41,
    TargetRequiredForSynchronizeDateOrDelete = 42,
    CreateOperationRequiresDirectoryTarget = 43,
    TargetAlreadyExistsForCreateOperation = 44,
    AtLeastOneTargetsNotAccessible = 45,
    
    // Advanced Consistency - Content Analysis
    NothingToCopyContentAndDateIdentical = 50,
    NothingToCopyContentIdentical = 51,
    
    // Consistency Against Already Set Actions - Rule Conflicts
    NonTargetedActionNotAllowedWithExistingDoNothingAction = 60,
    
    // Consistency Against Already Set Actions - Source/Destination Conflicts
    SourceCannotBeDestinationOfAnotherAction = 70,
    DestinationCannotBeSourceOfAnotherAction = 71,
    DestinationAlreadyUsedByNonComplementaryAction = 72,
    
    // Consistency Against Already Set Actions - Delete Conflicts
    CannotDeleteItemAlreadyUsedInAnotherAction = 80,
    CannotOperateOnItemBeingDeleted = 81,
    
    // Consistency Against Already Set Actions - Duplicate Actions
    DuplicateActionNotAllowed = 90
}
