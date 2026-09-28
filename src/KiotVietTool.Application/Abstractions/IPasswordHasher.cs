namespace KiotVietTool.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>False for a wrong password or an unrecognised hash format; never throws.</summary>
    bool Verify(string password, string passwordHash);
}
