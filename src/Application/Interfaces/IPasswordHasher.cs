namespace KiotVietTool.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>False for a wrong password or an unrecognised hash format; never throws.</summary>
    bool Verify(string password, string passwordHash);

    /// <summary>
    /// True when a valid hash is weaker than what <see cref="Hash"/> produces today (legacy format, older version,
    /// fewer iterations). Call after a successful <see cref="Verify"/> and re-hash the password. False for
    /// unrecognised formats (they never verify). Never throws.
    /// </summary>
    bool NeedsRehash(string passwordHash);
}
