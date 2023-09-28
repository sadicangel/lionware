using System.Runtime.CompilerServices;

namespace Lionware.dBase;
internal static class TempDirectoryExtensions
{
    public static void WithTempDirectory(this object instance, Action<string> action, [CallerMemberName] string memberName = "")
    {
        if (instance is null)
            ArgumentNullException.ThrowIfNull(instance);

        var directory = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), GetUniqueName(memberName)));
        try
        {
            action(directory.FullName);
        }
        finally
        {
            directory.Delete(recursive: true);
        }

        string GetUniqueName([CallerMemberName] string memberName = "") => $"{instance.GetType().Name}_{memberName}";
    }
}
