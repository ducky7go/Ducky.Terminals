using System.Reflection;

namespace Ducky.DemoTerminalClientWithoutSdk.Contracts;

public class Helper
{
    private static string? _modId;

    /// <summary>
    ///  Get Mod Id from folder name
    /// </summary>
    /// <returns></returns>
    internal static ModId GetModId()
    {
        // check folder to get mod id
        // if folder name is all digits, prefix with steam (Workshop mod)
        // otherwise prefix with local (local mod)
        if (!string.IsNullOrEmpty(_modId))
        {
            return _modId!;
        }

        var asm = Assembly.GetExecutingAssembly();
        var location = asm.Location;
        var folderName = Path.GetFileName(Path.GetDirectoryName(location));

        if (!string.IsNullOrEmpty(folderName))
        {
            // Check if folder name is all digits (Steam Workshop mod)
            if (folderName.All(char.IsDigit))
            {
                _modId = "steam." + folderName;
            }
            else
            {
                _modId = "local." + folderName;
            }
        }
        else
        {
            throw new Exception("Unable to determine mod folder name for ModId.");
        }

        return _modId!;
    }
}
