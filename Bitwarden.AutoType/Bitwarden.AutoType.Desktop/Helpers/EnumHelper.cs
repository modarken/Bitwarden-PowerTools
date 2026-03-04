using System;

namespace Bitwarden.AutoType.Desktop.Helpers;

public static class EnumHelper
{
    public static Array GetTargetTypesValues()
    {
        return Enum.GetValues(typeof(TargetTypes));
    }
}
