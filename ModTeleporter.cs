﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ModTeleporter
{
    /// <summary>
    /// ModTeleporter is a mod for Green Hell
    /// that allows a player to teleport to in-game map locations.
    /// (only in single player mode - Use ModManager for multiplayer).
    /// Enable the mod UI by pressing Home.
    /// </summary>
    public class ModTeleporter : MonoBehaviour
    {
        public static Rect ConfirmScreen = new Rect(Screen.width / 2f, Screen.height / 2f, 450f, 150f);

        public static Rect MapsScreen = new Rect(Screen.width / 4f, Screen.height / 40f, 750f, 150f);

        private bool ShowUI = false;
        private bool ShowMapsUI = false;
        private static readonly string LogPath = $"{Application.dataPath.Replace("GH_Data", "Logs")}/{nameof(ModTeleporter)}.log";

        public enum MapLocation
        {
            Teleport_Start_Location = 0,
            Bamboo_Bridge = 1,
            East_Native_Camp = 2,
            Elevator_Cave = 3,
            Planecrash_Cave = 4,
            Native_Passage = 5,
            Overturned_Jeep = 6,
            Abandoned_Tribal_Village = 7,
            West_Native_Camp = 8,
            Puddle = 9,
            Harbor = 10,
            Drug_Facility = 11,
            Bamboo_Camp = 12,
            Scorpion_Cartel_Cave = 13,
            Airport = 14,
            Jake_and_Mia_Camp = 15,
            Omega_Camp = 16,
            Main_Tribal_Village = 17,
            Anaconda_Island = 18,
            Pond = 19,
			Story_Start_Oasis = 20
        }

        private static ModTeleporter s_Instance;

        private static readonly string ModName = nameof(ModTeleporter);

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        private static Dictionary<int, MapLocation> MapLocations = new Dictionary<int, MapLocation>();

        private static Dictionary<MapLocation, Vector3> MapGpsCoordinates = new Dictionary<MapLocation, Vector3>();

        private static MapLocation CurrentMapLocation = MapLocation.Teleport_Start_Location;

        private static MapLocation NextMapLocation = MapLocation.Teleport_Start_Location;

        private static int NextMapLocationID = (int)NextMapLocation;

        private static MapLocation LastMapLocationTeleportedTo = MapLocation.Teleport_Start_Location;

        private static string SelectedMapLocation = string.Empty;

        private static int SelectedMapLocationIndex = 0;

        public static string[] GetMapLocations()
        {
            string[] locationNames = Enum.GetNames(typeof(MapLocation));

            for (int i = 0; i < locationNames.Length; i++)
            {
                string locationName = locationNames[i];
                locationNames[i] = locationName.Replace("_", " ");
            }

            return locationNames;
        }

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        private static string HUDBigInfoMessage(string message) => $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.red)}>System</color>\n{message}";

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            IsModActiveForMultiplayer = optionValue;
            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.green)}>Permission to use mods for multiplayer was granted!</color>")
                            : HUDBigInfoMessage($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.yellow)}>Permission to use mods for multiplayer was revoked!</color>")),
                           $"{ModName} Info",
                           HUDInfoLogTextureType.Count.ToString());
        }

        public ModTeleporter()
        {
            useGUILayout = true;
            s_Instance = this;
        }

        public ModTeleporter Get()
        {
            return s_Instance;
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer, false);

            if (blockPlayer)
            {
                player.BlockMoves();
                player.BlockRotation();
                player.BlockInspection();
            }
            else
            {
                player.UnblockMoves();
                player.UnblockRotation();
                player.UnblockInspection();
            }
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)hUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        public void ShowHUDBigInfo(string text, string header, string textureName)
        {
            HUDBigInfo hudBigInfo = (HUDBigInfo)hUDManager.GetHUD(typeof(HUDBigInfo));
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
                ConfirmScreen = GUILayout.Window(GetHashCode(), ConfirmScreen, AskConfirm, $"{ModName} Info", GUI.skin.window);
            }

            if (ShowMapsUI)
            {
                MapsScreen = GUILayout.Window(GetHashCode(), MapsScreen, ShowMapsScreen, $"{ModName} Info", GUI.skin.window);
            }
        }

        private void ShowMapsScreen(int windowID)
        {
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (GUI.Button(new Rect(730f, 0f, 20f, 20f), "X", GUI.skin.button))
                {
                    CloseWindow();
                }

                using (var currentScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Last map location: {LastMapLocationTeleportedTo.ToString().Replace("_", " ")}", GUI.skin.label);
                }

                using (var selectScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Select next map location to teleport to. Then click teleport", GUI.skin.label);
                    SelectedMapLocationIndex = GUILayout.SelectionGrid(SelectedMapLocationIndex, GetMapLocations(), 3, GUI.skin.button);

                    if (GUILayout.Button("Teleport", GUI.skin.button))
                    {
                        TeleportToNextMapLocation();
                        CloseWindow();
                    }
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void AskConfirm(int windowID)
        {
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (GUI.Button(new Rect(430f, 0f, 20f, 20f), "X", GUI.skin.button))
                {
                    CloseWindow();
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Fast travel from {LastMapLocationTeleportedTo.ToString().Replace("_", " ")} to {NextMapLocation.ToString().Replace("_", " ")}?", GUI.skin.label);
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    if (GUILayout.Button("Yes", GUI.skin.button))
                    {
                        TeleportToNextMapLocation();
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

        private void CloseWindow()
        {
            ShowUI = false;
            ShowMapsUI = false;
            EnableCursor(false);
        }

        private void InitData()
        {
            hUDManager = HUDManager.Get();
            itemsManager = ItemsManager.Get();
            player = Player.Get();
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
                MapGpsCoordinates.Add(MapLocation.Teleport_Start_Location, player.transform.position);
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

        public void TeleportToNextMapLocation()
        {
            try
            {
                if (ShowMapsUI)
                {
                    string[] mapLocations = GetMapLocations();
                    SelectedMapLocation = mapLocations[SelectedMapLocationIndex].Replace(" ", "_");
                    NextMapLocationID = (int)EnumUtils<MapLocation>.GetValue(SelectedMapLocation);
                    NextMapLocation = MapLocations.GetValueOrDefault(NextMapLocationID);
                }

                GameObject mapLocation = new GameObject(nameof(MapLocation));
                Vector3 gpsCoordinates = MapGpsCoordinates.GetValueOrDefault(NextMapLocation);
                mapLocation.transform.position = gpsCoordinates;
                player.Teleport(mapLocation, true);

                LastMapLocationTeleportedTo = NextMapLocation;
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(TeleportToNextMapLocation)}] throws exception: {exc.Message}");
            }
        }

        public void PrintPlayerInfo()
        {
            try
            {
                Vector3 playerPosition = player.GetWorldPosition();
                string info = PrintPositionInfo(playerPosition, $"PLAYER WORLD POSITION");
                ShowHUDBigInfo(
                   HUDBigInfoMessage($"{info}\nlogged to {LogPath}."),
                    $"{ModName} Info",
                    HUDInfoLogTextureType.Count.ToString());
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(PrintPlayerInfo)}] throws exception: {exc.Message}");
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
                ShowHUDBigInfo(
                    HUDBigInfoMessage($"{info}\nlogged to {LogPath}."),
                     $"{ModName} Info",
                     HUDInfoLogTextureType.Count.ToString());
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(PrintDebugSpawnerInfo)}] throws exception: {exc.Message}");
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
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(PrintPositionInfo)}] throws exception: {exc.Message}");
                return string.Empty;
            }
        }

    }
}
