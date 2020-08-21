using System;
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
            Scorpion_Cave = 13,
            Airport = 14,
            Jake_His_Camp = 15,
            Omega_Camp = 16,
            Main_Village = 17,
            Island = 18,
            Pond = 19,
            Refugee_Island = 20
        }

        private static ModTeleporter s_Instance;

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        private static Dictionary<int, MapLocation> m_MapLocations = new Dictionary<int, MapLocation>();
        private static Dictionary<MapLocation, Vector3> m_MapGpsCoordinates = new Dictionary<MapLocation, Vector3>();
        private static MapLocation m_CurrentMapLocation = MapLocation.Teleport_Start_Location;
        private static MapLocation m_NextMapLocation = MapLocation.Teleport_Start_Location;
        private static MapLocation m_LastMapLocationTeleportedTo = MapLocation.Teleport_Start_Location;

        /// <summary>
        /// ModAPI required security check to enable this mod feature for multiplayer.
        /// See <see cref="ModManager"/> for implementation.
        /// Based on request in chat: use  !requestMods in chat as client to request the host to activate mods for them.
        /// </summary>
        /// <returns>true if enabled, else false</returns>
        public bool IsModActiveForMultiplayer => FindObjectOfType(typeof(ModManager.ModManager)) != null ? ModManager.ModManager.AllowModsForMultiplayer : false;

        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public ModTeleporter()
        {
            s_Instance = this;
        }

        public static ModTeleporter Get()
        {
            return s_Instance;
        }

        public static void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)hUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        public static void ShowHUDBigInfo(string text, string header, string textureName)
        {
            HUDManager hUDManager = HUDManager.Get();

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
            if (m_CurrentMapLocation != m_LastMapLocationTeleportedTo)
            {
                ShowHUDBigInfo($"Teleported to {m_CurrentMapLocation.ToString().Replace('_', ' ')}", "ModTeleport Info", HUDInfoLogTextureType.Count.ToString());
                m_LastMapLocationTeleportedTo = m_CurrentMapLocation;
            }

            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                InitData();
                InitMapLocations();
                TeleportToNextMapLocation();
            }
        }

        private static void InitData()
        {
            hUDManager = HUDManager.Get();
            itemsManager = ItemsManager.Get();
            player = Player.Get();
        }

        private static void InitMapLocations()
        {
            InitMapKeys();
            InitMapPositions();
        }

        private static void InitMapKeys()
        {
            foreach (MapLocation location in Enum.GetValues(typeof(MapLocation)))
            {
                if (!m_MapLocations.ContainsKey((int)location))
                {
                    m_MapLocations.Add((int)location, location);
                }
            }
        }

        private static void InitMapPositions()
        {
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Teleport_Start_Location))
            {
                m_MapGpsCoordinates.Add(MapLocation.Teleport_Start_Location, player.transform.position);
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Bamboo_Bridge))
            {
                m_MapGpsCoordinates.Add(MapLocation.Bamboo_Bridge, new Vector3(831.159f, 138.608f, 1620.014f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Refugee_Island))
            {
                m_MapGpsCoordinates.Add(MapLocation.Refugee_Island, new Vector3(899.3753f, 136.208f, 1424.16f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Island))
            {
                m_MapGpsCoordinates.Add(MapLocation.Island, new Vector3(898.0696f, 136.465f, 1425.064f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.East_Native_Camp))
            {
                m_MapGpsCoordinates.Add(MapLocation.East_Native_Camp, new Vector3(802.765f, 129.871f, 1675.741f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Elevator_Cave))
            {
                m_MapGpsCoordinates.Add(MapLocation.Elevator_Cave, new Vector3(688.0139f, 113.0132f, 1704.087f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Planecrash_Cave))
            {
                m_MapGpsCoordinates.Add(MapLocation.Planecrash_Cave, new Vector3(695.7698f, 123.5581f, 1488.888f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Native_Passage))
            {
                m_MapGpsCoordinates.Add(MapLocation.Native_Passage, new Vector3(653.4912f, 138.7564f, 1416.553f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Overturned_Jeep))
            {
                m_MapGpsCoordinates.Add(MapLocation.Overturned_Jeep, new Vector3(530.194f, 127.8356f, 1753.261f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Abandoned_Tribal_Village))
            {
                m_MapGpsCoordinates.Add(MapLocation.Abandoned_Tribal_Village, new Vector3(465.1541f, 106.5126f, 1408.053f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.West_Native_Camp))
            {
                m_MapGpsCoordinates.Add(MapLocation.West_Native_Camp, new Vector3(412.365f, 98.77797f, 1704.949f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Pond))
            {
                m_MapGpsCoordinates.Add(MapLocation.Pond, new Vector3(278.0788f, 101.3528f, 1510.454f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Puddle))
            {
                m_MapGpsCoordinates.Add(MapLocation.Puddle, new Vector3(265.83f, 96.967f, 1500.19f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Harbor))
            {
                m_MapGpsCoordinates.Add(MapLocation.Harbor, new Vector3(237.4533f, 89.79554f, 1659.221f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Drug_Facility))
            {
                m_MapGpsCoordinates.Add(MapLocation.Drug_Facility, new Vector3(290.9244f, 102.471f, 1377.707f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Bamboo_Camp))
            {
                m_MapGpsCoordinates.Add(MapLocation.Bamboo_Camp, new Vector3(976.809f, 155.6489f, 1309.329f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Scorpion_Cave))
            {
                m_MapGpsCoordinates.Add(MapLocation.Scorpion_Cave, new Vector3(180.9195f, 121.599f, 1276.029f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Airport))
            {
                m_MapGpsCoordinates.Add(MapLocation.Airport, new Vector3(1166.69f, 179.99f, 1536.7f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Main_Village))
            {
                m_MapGpsCoordinates.Add(MapLocation.Main_Village, new Vector3(1066.53f, 93.01f, 1060.56f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Omega_Camp))
            {
                m_MapGpsCoordinates.Add(MapLocation.Omega_Camp, new Vector3(1288.869f, 92.616f, 1124.57f));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Jake_His_Camp))
            {
                m_MapGpsCoordinates.Add(MapLocation.Jake_His_Camp, new Vector3(1198.763f, 98.715f, 1122.541f));
            }
        }

        public void TeleportToNextMapLocation()
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    GameObject mapLocation = new GameObject(nameof(MapLocation));
                    int m_NextMapLocationID = (int)m_LastMapLocationTeleportedTo + 1;
                    m_NextMapLocation = m_MapLocations.GetValueOrDefault(m_NextMapLocationID);

                    Vector3 gpsCoordinates = m_MapGpsCoordinates.GetValueOrDefault(m_NextMapLocation);
                    mapLocation.transform.position = gpsCoordinates;
                    player.Teleport(mapLocation, true);

                    m_CurrentMapLocation = m_NextMapLocation;
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModTeleporter)}.{nameof(ModTeleporter)}:{nameof(TeleportToNextMapLocation)}] throws exception: {exc.Message}");
            }
        }

        public void PrintPlayerInfo()
        {
            try
            {
                Vector3 playerPosition = player.GetWorldPosition();
                string info = PrintPositionInfo(playerPosition, $"player world position");
                ShowHUDBigInfo($"{info}", "ModTeleport Info", HUDInfoLogTextureType.Count.ToString());
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModTeleporter)}.{nameof(ModTeleporter)}:{nameof(PrintPlayerInfo)}] throws exception: {exc.Message}");
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
                ShowHUDBigInfo($"{info}", "ModTeleport Info", HUDInfoLogTextureType.Count.ToString());
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModTeleporter)}.{nameof(ModTeleporter)}:{nameof(PrintDebugSpawnerInfo)}] throws exception: {exc.Message}");
            }
        }

        public string PrintPositionInfo(Vector3 position, string name = "name")
        {
            try
            {
                StringBuilder info = new StringBuilder($"\n{name.ToUpper()}");
                info.AppendLine($"\nx: {position.x}, y: {position.y} z: {position.z} ");
                ModAPI.Log.Write(info.ToString());
                return info.ToString();
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModTeleporter)}.{nameof(ModTeleporter)}:{nameof(PrintPositionInfo)}] throws exception: {exc.Message}");
                return string.Empty;
            }
        }

    }
}
