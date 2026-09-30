namespace KiotVietTool.Application.Interfaces;

public interface ISecretProtectorService
{
    string Protect(string secret);
    string? Unprotect(string protectedSecret);
}
