namespace KiotVietTool.Application.Interfaces;

public interface IDatabaseBackupService
{
    Task<string> BackupAsync(string reason, CancellationToken cancellationToken);
}
