namespace Aiursoft.MusicExam.Models.BackgroundJobs;

/// <summary>
/// Represents the status of a background job.
/// </summary>
public enum JobStatus
{
    Pending,
    Processing,
    Success,
    Failed,
    Cancelled
}
