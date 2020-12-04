using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ModTeleporter
{
    /// <summary>
    /// ModTeleporter is a mod for Green Hell
    /// that allows a player to teleport to in-game map locations.
    /// Enable the mod UI by pressing Home.
    /// </summary>
    public class ModTeleporter : MonoBehaviour
    {
        private static ModTeleporter Instance;
        private static readonly string LogPath = $"{Application.dataPath.Replace("GH_Data", "Logs")}/{nameof(ModTeleporter)}.log";
        private static readonly string ModName = nameof(ModTeleporter);
        private static readonly float ModScreenTotalWidth = 850f;
        private static readonly float ModScreenTotalHeight = 500f;
        private static readonly float ModScreenMinWidth = 800f;
        private static readonly float ModScreenMaxWidth = 850f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 550f;
        private static float ModScreenStartPositionX { get; set; } = Screen.width - ModScreenMaxWidth / 2f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height - ModScreenMaxHeight / 2f;
        private static bool IsMinimized { get; set; } = false;
        private bool ShowUI = false;
        private bool ShowMapsUI = false;

        private static ItemsManager LocalItemsManager;
        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;

        public static Rect ModTeleporterScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
        public static Rect ModConfirmFastTravelDialogWindow = new Rect(Screen.width / 2f, Screen.height / 2f, 450f, 150f);

        public static Dictionary<int, MapLocation> MapLocations = new Dictionary<int, MapLocation>();
        public static Dictionary<MapLocation, Vector3> MapGpsCoordinates = new Dictionary<MapLocation, Vector3>();

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public static GameObject MapLocationObject = new GameObject(nameof(MapLocation));
        public static Vector3 GpsCoordinates = Vector3.zero;
        public static MapLocation CurrentMapLocation = MapLocation.Teleport_Start_Location;
        public static MapLocation NextMapLocation = MapLocation.Teleport_Start_Location;
        public static MapLocation LastMapLocationTeleportedTo = MapLocation.Teleport_Start_Location;
        public static string SelectedMapLocationName = string.Empty;
        public static int NextMapLocationID = (int)NextMapLocation;
        public static int SelectedMapLocationIndex = 0;
        public static string[] GetMapLocationNames()
        {
            string[] locationNames = Enum.GetNames(typeof(MapLocation));

            for (int i = 0; i < locationNames.Length; i++)
            {
                string locationName = locationNames[i];
                locationNames[i] = locationName.Replace("_", " ");
            }

            return locationNames;
        }

        public static string OnlyForSinglePlayerOrHostMessage() => $"Only available for single player or when host. Host can activate using ModManager.";
        public static string PermissionChangedMessage(string permission) => $"Permission to use mods and cheats in multiplayer was {permission}";
        public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc.Message}";
            ModAPI.Log.Write(info);
            ShowHUDBigInfo(HUDBigInfoMessage(info, MessageType.Error, Color.red));
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            IsModActiveForMultiplayer = optionValue;
            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked"), MessageType.Info, Color.yellow))
                            );
        }

        private void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalItemsManager = ItemsManager.Get();
            LocalPlayer = Player.Get();
        }

        private void InitMapLocations()
        {
            InitMapKeys();
            InitMapPositions();
        }

        private void InitMapKeys()
        {
            foreach (MapLocation location in Enum.GetValues(typeof(MapLocation)))
            {
                if (!MapLocations.ContainsKey((int)location))
                {
                    MapLocations.Add((int)location, location);
                }
            }
        }

        private void InitMapPositions()
        {
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Teleport_Start_Location))
            {
                MapGpsCoordinates.Add(MapLocation.Teleport_Start_Location, LocalPlayer.transform.position);
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Bamboo_Bridge))
            {
                MapGpsCoordinates.Add(MapLocation.Bamboo_Bridge, new Vector3(831.159f, 138.608f, 1620.014f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Anaconda_Island))
            {
                MapGpsCoordinates.Add(MapLocation.Anaconda_Island, new Vector3(898.0696f, 136.465f, 1425.064f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.East_Native_Camp))
            {
                MapGpsCoordinates.Add(MapLocation.East_Native_Camp, new Vector3(802.765f, 129.871f, 1675.741f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Elevator_Cave))
            {
                MapGpsCoordinates.Add(MapLocation.Elevator_Cave, new Vector3(688.0139f, 113.0132f, 1704.087f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Planecrash_Cave))
            {
                MapGpsCoordinates.Add(MapLocation.Planecrash_Cave, new Vector3(695.7698f, 123.5581f, 1488.888f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Native_Passage))
            {
                MapGpsCoordinates.Add(MapLocation.Native_Passage, new Vector3(653.4912f, 138.7564f, 1416.553f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Overturned_Jeep))
            {
                MapGpsCoordinates.Add(MapLocation.Overturned_Jeep, new Vector3(530.194f, 127.8356f, 1753.261f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Abandoned_Tribal_Village))
            {
                MapGpsCoordinates.Add(MapLocation.Abandoned_Tribal_Village, new Vector3(465.1541f, 106.5126f, 1408.053f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.West_Native_Camp))
            {
                MapGpsCoordinates.Add(MapLocation.West_Native_Camp, new Vector3(412.365f, 98.77797f, 1704.949f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Pond))
            {
                MapGpsCoordinates.Add(MapLocation.Pond, new Vector3(278.0788f, 101.3528f, 1510.454f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Puddle))
            {
                MapGpsCoordinates.Add(MapLocation.Puddle, new Vector3(265.83f, 96.967f, 1500.19f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Harbor))
            {
                MapGpsCoordinates.Add(MapLocation.Harbor, new Vector3(237.4533f, 89.79554f, 1659.221f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Drug_Facility))
            {
                MapGpsCoordinates.Add(MapLocation.Drug_Facility, new Vector3(290.9244f, 102.471f, 1377.707f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Bamboo_Camp))
            {
                MapGpsCoordinates.Add(MapLocation.Bamboo_Camp, new Vector3(976.809f, 155.6489f, 1309.329f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Scorpion_Cartel_Cave))
            {
                MapGpsCoordinates.Add(MapLocation.Scorpion_Cartel_Cave, new Vector3(180.9195f, 121.599f, 1276.029f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Airport))
            {
                MapGpsCoordinates.Add(MapLocation.Airport, new Vector3(1166.69f, 179.99f, 1536.7f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Main_Tribal_Village))
            {
                MapGpsCoordinates.Add(MapLocation.Main_Tribal_Village, new Vector3(1066.53f, 93.01f, 1060.56f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Omega_Camp))
            {
                MapGpsCoordinates.Add(MapLocation.Omega_Camp, new Vector3(1288.869f, 92.616f, 1124.57f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Jake_and_Mia_Camp))
            {
                MapGpsCoordinates.Add(MapLocation.Jake_and_Mia_Camp, new Vector3(1198.763f, 98.715f, 1122.541f));
            }
            if (!MapGpsCoordinates.ContainsKey(MapLocation.Story_Start_Oasis))
            {
                MapGpsCoordinates.Add(MapLocation.Story_Start_Oasis, new Vector3(496.5056f, 99.32371f, 1202.032f));
            }
        }

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
        }

        public ModTeleporter()
        {
            useGUILayout = true;
            Instance = this;
        }

        public ModTeleporter Get()
        {
            return Instance;
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer, false);

            if (blockPlayer)
            {
                LocalPlayer.BlockMoves();
                LocalPlayer.BlockRotation();
                LocalPlayer.BlockInspection();
            }
            else
            {
                LocalPlayer.UnblockMoves();
                LocalPlayer.UnblockRotation();
                LocalPlayer.UnblockInspection();
            }
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        public void ShowHUDBigInfo(string text)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();
            HUDBigInfo hudBigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = 2f;
            HUDBigInfoData hudBigInfoData = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            hudBigInfo.AddInfo(hudBigInfoData);
            hudBigInfo.Show(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                if (!ShowUI)
                {
                    InitData();
                    InitMapLocations();
                    EnableCursor(true);
                }
                ToggleShowUI(0);
                if (!ShowUI)
                {
                    EnableCursor(false);
                }
            }

            if (Input.GetKeyDown(KeyCode.Home))
            {
                if (!ShowMapsUI)
                {
                    InitData();
                    InitMapLocations();
                    EnableCursor(true);
                }
                ToggleShowUI(1);
                if (!ShowMapsUI)
                {
                    EnableCursor(false);
                }
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                InitData();
                PrintPlayerInfo();
            }
        }

        private void ToggleShowUI(int level)
        {
            switch (level)
            {
                case 0:
                    ShowUI = !ShowUI;
                    break;
                case 1:
                    ShowMapsUI = !ShowMapsUI;
                    break;
                default:
                    ShowUI = !ShowUI;
                    ShowMapsUI = !ShowMapsUI;
                    break;
            }
        }

        private void OnGUI()
        {
            if (ShowUI || ShowMapsUI)
            {
                InitData();
                InitMapLocations();
                InitSkinUI();
                InitWindow();
            }
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitWindow()
        {
            CurrentMapLocation = LastMapLocationTeleportedTo;

            if (ShowUI)
            {
                NextMapLocationID = (int)LastMapLocationTeleportedTo + 1;
                NextMapLocation = MapLocations.GetValueOrDefault(NextMapLocationID);
                ModConfirmFastTravelDialogWindow = GUILayout.Window(GetHashCode(), ModConfirmFastTravelDialogWindow, InitModConfirmFastTravelDialogWindow, "Fast travel?", GUI.skin.window);
            }

            if (ShowMapsUI)
            {
                ModTeleporterScreen = GUILayout.Window(GetHashCode(), ModTeleporterScreen, InitMapsScreen, ModName,
                                                                                                          GUI.skin.window,
                                                                                                          GUILayout.ExpandWidth(true),
                                                                                                          GUILayout.MinWidth(ModScreenMinWidth),
                                                                                                          GUILayout.MaxWidth(ModScreenMaxWidth),
                                                                                                          GUILayout.ExpandHeight(true),
                                                                                                          GUILayout.MinHeight(ModScreenMinHeight),
                                                                                                          GUILayout.MaxHeight(ModScreenMaxHeight));
            }
        }

        private void ScreenMenuBox()
        {
            if (GUI.Button(new Rect(ModTeleporterScreen.width - 40f, 0f, 20f, 20f), "-", GUI.skin.button))
            {
                CollapseWindow();
            }

            if (GUI.Button(new Rect(ModTeleporterScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void CollapseWindow()
        {
            if (!IsMinimized)
            {
                ModTeleporterScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenMinHeight);
                IsMinimized = true;
            }
            else
            {
                ModTeleporterScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
                IsMinimized = false;
            }
            InitWindow();
        }

        private void CloseWindow()
        {
            ShowUI = false;
            ShowMapsUI = false;
            EnableCursor(false);
        }

        private void InitMapsScreen(int windowID)
        {
            ModScreenStartPositionX = ModTeleporterScreen.x;
            ModScreenStartPositionY = ModTeleporterScreen.y;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox();
                if (!IsMinimized)
                {
                    MapLocationsBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void MapLocationsBox()
        {
            using (var selectScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                string[] mapLocationNames = GetMapLocationNames();
                if (mapLocationNames != null)
                {
                    GUI.color = Color.cyan;
                    GUILayout.Label($"Last map location: {LastMapLocationTeleportedTo.ToString().Replace("_", " ")}", GUI.skin.label);
                    GUI.color = Color.white;
                    GUILayout.Label("Select next map location to teleport to. Then click teleport", GUI.skin.label);
                    SelectedMapLocationIndex = GUILayout.SelectionGrid(SelectedMapLocationIndex, mapLocationNames, 3, GUI.skin.button);
                    if (GUILayout.Button("Teleport", GUI.skin.button))
                    {
                        OnClickTeleport();
                        CloseWindow();
                    }
                }
            }
        }

        private void InitModConfirmFastTravelDialogWindow(int windowID)
        {
            using (var dialogScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.color = Color.cyan;
                GUILayout.Label($"Fast travel from {LastMapLocationTeleportedTo.ToString().Replace("_", " ")} to {NextMapLocation.ToString().Replace("_", " ")}?", GUI.skin.label);
                GUI.color = Color.white;
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    if (GUILayout.Button("Yes", GUI.skin.button))
                    {
                        OnClickTeleport();
                        CloseWindow();
                    }
                    if (GUILayout.Button("No", GUI.skin.button))
                    {
                        CloseWindow();
                    }
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        public void OnClickTeleport()
        {
            try
            {
                if (ShowMapsUI || ShowUI)
                {
                    string[] mapLocationNames = GetMapLocationNames();
                    if (mapLocationNames != null)
                    {
                        SelectedMapLocationName = mapLocationNames[SelectedMapLocationIndex].Replace(" ", "_");
                        if (!string.IsNullOrEmpty(SelectedMapLocationName))
                        {
                            NextMapLocationID = (int)EnumUtils<MapLocation>.GetValue(SelectedMapLocationName);
                            NextMapLocation = MapLocations.GetValueOrDefault(NextMapLocationID);
                            GpsCoordinates = MapGpsCoordinates.GetValueOrDefault(NextMapLocation);
                            if (GpsCoordinates != Vector3.zero)
                            {
                                TeleportToLocation();
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickTeleport));
            }
        }

        public void TeleportToLocation()
        {
            try
            {
                if (GpsCoordinates == Vector3.zero)
                {
                    TeleportToLocation();
                }
                MapLocationObject.transform.position = GpsCoordinates;
                LocalPlayer.Teleport(MapLocationObject, true);
                LastMapLocationTeleportedTo = NextMapLocation;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(TeleportToLocation));
            }
        }

        public void PrintPlayerInfo()
        {
            try
            {
                Vector3 playerPosition = LocalPlayer.GetWorldPosition();
                string info = PrintPositionInfo(playerPosition, $"PLAYER WORLD POSITION");
                ShowHUDBigInfo(HUDBigInfoMessage($"{info}\nlogged to {LogPath}.", MessageType.Info, Color.green));
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(PrintPlayerInfo));
            }
        }

        public void PrintDebugSpawnerInfo()
        {
            try
            {
                string info = string.Empty;
                DebugSpawner[] array = FindObjectsOfType<DebugSpawner>();
                for (int i = 0; i < array.Length; i++)
                {
                    info += PrintPositionInfo(array[i].gameObject.transform.position, array[i].gameObject.name);
                }
                ShowHUDBigInfo(HUDBigInfoMessage($"{info}\nlogged to {LogPath}.", MessageType.Info, Color.green));
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(PrintDebugSpawnerInfo));
            }
        }

        public string PrintPositionInfo(Vector3 position, string header = "header")
        {
            try
            {
                StringBuilder info = new StringBuilder($"\n<color=#{ColorUtility.ToHtmlStringRGBA(Color.green)}>{header.ToUpper()}</color>");
                info.AppendLine($"\nx: {position.x}, y: {position.y} z: {position.z} ");
                ModAPI.Log.Write(info.ToString());
                return info.ToString();
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(PrintPositionInfo));
                return string.Empty;
            }
        }
    }
}
