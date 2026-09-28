namespace KiotVietTool.Desktop.Common.Navigation;

/// <summary>
/// Well-known screens, set once in the composition root so Common and features don't reference each other:
/// Login/ChangePassword are the auth guard targets, Home is where "go home" lands.
/// </summary>
public sealed record NavigationRoutes(Type Login, Type ChangePassword, Type Home);
