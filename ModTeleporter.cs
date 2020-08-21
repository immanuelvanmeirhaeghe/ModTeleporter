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
            Waterfall_Oasis = 7,
            Abandoned_Tribal_Village = 8,
            West_Native_Camp = 9,
            Waterhole = 10,
            Fishingdock = 11,
            Drugfacility = 12,
            Bamboo_Camp = 13,
            Scorpion_Cave = 14
        }

        private static ModTeleporter s_Instance;

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        private static Dictionary<int, MapLocation> m_MapLocations = new Dictionary<int, MapLocation>();
        private static Dictionary<MapLocation, Vector3> m_MapGpsCoordinates = new Dictionary<MapLocation, Vector3>();
        public static MapLocation m_CurrentMapLocation = MapLocation.Teleport_Start_Location;
        public static MapLocation m_NextMapLocationForTeleport = MapLocation.Teleport_Start_Location;
        public static MapLocation m_LastMapLocationTeleportedTo = MapLocation.Teleport_Start_Location;

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
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                InitData();
                InitMapLocations();
                TeleportToNextMapLocation();
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                InitData();
                PrintPlayerInfo();
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
                m_MapGpsCoordinates.Add(MapLocation.Bamboo_Bridge, new Vector3(37, 0, 18));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.East_Native_Camp))
            {
                m_MapGpsCoordinates.Add(MapLocation.East_Native_Camp, new Vector3(37, 0, 19));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Elevator_Cave))
            {
                m_MapGpsCoordinates.Add(MapLocation.Elevator_Cave, new Vector3(40, 0, 19));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Planecrash_Cave))
            {
                m_MapGpsCoordinates.Add(MapLocation.Planecrash_Cave, new Vector3(40, 0, 24));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Native_Passage))
            {
                m_MapGpsCoordinates.Add(MapLocation.Native_Passage, new Vector3(42, 0, 22));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Overturned_Jeep))
            {
                m_MapGpsCoordinates.Add(MapLocation.Overturned_Jeep, new Vector3(44, 0, 17));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Waterfall_Oasis))
            {
                m_MapGpsCoordinates.Add(MapLocation.Waterfall_Oasis, new Vector3(45, 0, 32));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Abandoned_Tribal_Village))
            {
                m_MapGpsCoordinates.Add(MapLocation.Abandoned_Tribal_Village, new Vector3(46, 0, 26));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.West_Native_Camp))
            {
                m_MapGpsCoordinates.Add(MapLocation.West_Native_Camp, new Vector3(47, 0, 17));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Waterhole))
            {
                m_MapGpsCoordinates.Add(MapLocation.Waterhole, new Vector3(50, 0, 24));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Fishingdock))
            {
                m_MapGpsCoordinates.Add(MapLocation.Fishingdock, new Vector3(51, 0, 19));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Drugfacility))
            {
                m_MapGpsCoordinates.Add(MapLocation.Drugfacility, new Vector3(51, 0, 27));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Bamboo_Camp))
            {
                m_MapGpsCoordinates.Add(MapLocation.Bamboo_Camp, new Vector3(52, 0, 17));
            }
            if (!m_MapGpsCoordinates.ContainsKey(MapLocation.Scorpion_Cave))
            {
                m_MapGpsCoordinates.Add(MapLocation.Scorpion_Cave, new Vector3(52, 0, 16));
            }
        }

        public void TeleportToNextMapLocation()
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    m_CurrentMapLocation = m_LastMapLocationTeleportedTo;
                    GameObject mapLocation = new GameObject(nameof(MapLocation));
                    int m_NextMapLocationKey = (int)m_LastMapLocationTeleportedTo + 1;

                    m_NextMapLocationForTeleport = m_MapLocations.GetValueOrDefault(m_NextMapLocationKey);
                    m_LastMapLocationTeleportedTo = m_NextMapLocationForTeleport;

                    Vector3 gpsCoordinates = m_MapGpsCoordinates.GetValueOrDefault(m_NextMapLocationForTeleport);
                    PrintPositionInfo(gpsCoordinates, $"gps coordinates {m_NextMapLocationForTeleport.ToString()}");

                    Vector3 teleportPosition = m_NextMapLocationForTeleport != MapLocation.Teleport_Start_Location ?
                                                                                                                            GetPositionByGPSCoordinates(gpsCoordinates.x, gpsCoordinates.z)
                                                                                                                            : gpsCoordinates;
                    PrintPositionInfo(teleportPosition, $"teleport to position {m_NextMapLocationForTeleport.ToString()}");

                    mapLocation.transform.position = teleportPosition;
                    player.Teleport(mapLocation, true);

                    ShowHUDBigInfo($"Teleported to {m_NextMapLocationForTeleport.ToString().Replace('_', ' ')} at {gpsCoordinates.x} S {gpsCoordinates.z} W", "ModTeleport Info", HUDInfoLogTextureType.Count.ToString());
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

        public static string PrintPositionInfo(Vector3 position, string name = "name")
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

        public static Vector3 GetPositionByGPSCoordinates(float gps_lat, float gps_long)
        {
            Vector3 inversed = Vector3.zero;
            try
            {
                Vector3 position2 = MapTab.Get().m_WorldZeroDummy.position;
                Vector3 position3 = MapTab.Get().m_WorldOneDummy.position;
                float num = position3.x - position2.x;
                float num2 = position3.z - position2.z;
                float num3 = num / 35f;
                float num4 = num2 / 27f;

                inversed.x = (gps_lat * num3) - 20;
                inversed.z = (gps_long * num4) - 14;

                return inversed;
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModTeleporter)}.{nameof(ModTeleporter)}:{nameof(GetPositionByGPSCoordinates)}] throws exception: {exc.Message}");
                return inversed;
            }
        }
    }
}
