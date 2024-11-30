namespace ByteSync.Business.Updates
{
    public class UpdateProgress
    {
        public UpdateProgress(UpdateProgressStatus status)
        {
            Status = status;
            Percentage = null;
        }
        
        public UpdateProgress(UpdateProgressStatus status, int percentage)
        {
            Status = status;
            Percentage = percentage;
        }
        
        public UpdateProgressStatus Status { get; }
        
        public int? Percentage { get; }
    }
}