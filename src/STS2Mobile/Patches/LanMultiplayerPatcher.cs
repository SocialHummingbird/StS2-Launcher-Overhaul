using System.Reflection;
using Godot;

namespace STS2Mobile.Patches;

// Enables LAN multiplayer by replacing the Steam friends list with UDP broadcast
// discovery. Hosts advertise via a beacon on port 33770, clients discover them
// automatically or connect manually by IP address.
internal static partial class LanMultiplayerPatcher
{
    private const int BeaconPort = 33770;
    private const int BeaconChecksPerSendInterval = 20;
    private const int BeaconSendLoopSleepMs = 100;
    private const string BeaconPrefix = "STS2LAN";
    private const int ContainerSeparation = 10;
    private const int EntryFontSize = 28;
    private const int GamePort = 33771;
    private const ulong HostPlayerId = 1uL;
    private const string LastIpConfigPath = "user://lan_last_ip.cfg";
    private const string LastIpKey = "last_ip";
    private const string LastIpSection = "lan";
    private const int MaxPort = 65535;
    private const int MinPort = 1;
    private const ulong Player2Id = 1000uL;
    private const ulong Player3Id = 1001uL;
    private const ulong Player4Id = 1002uL;

    private static FieldInfo _buttonContainerField;
    private static FieldInfo _loadingOverlayField;
    private static FieldInfo _noFriendsLabelField;
    private static FieldInfo _loadingIndicatorField;
    private static MethodInfo _joinFriendButtonCreate;
    private static ConstructorInfo _eNetClientConnInitCtor;
    private static MethodInfo _joinGameAsyncMethod;
    private static MethodInfo _taskHelperRunSafely;
    private static MethodInfo _setTextAutoSize;
    private static PropertyInfo _activeScreenContextInstance;
    private static MethodInfo _activeScreenContextUpdate;

    private static LineEdit _ipLineEdit;
    private static LanDiscovery _discovery;
    private static LanBeacon _hostBeacon;
    private static bool _joinInProgress;
    private static bool _screenContextUpdateFailureLogged;
}
