using static Kuriatnyk.UI.K;

namespace Kuriatnyk.UI;

public static partial class Overlays
{
    public static void OpenMultiplayer(MainView mv) => mv.Toast(T("Многопользовательская игра"));
}
