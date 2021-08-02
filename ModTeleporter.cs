using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace ModTeleporter
{
    /// <summary>
    /// ModTeleporter is a mod for Green Hell that allows a player to teleport to custom-bind or key map locations in sequence or on selection.
    /// Pess 7 (default) or the key configurable in ModAPI to open the mod screen.
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

        private static readonly float MapLocationIconSize = 25f;
        private string MapLocationTextureUrl = "https://modapi.survivetheforest.net/uploads/objects/9/marker.png";
        private Texture2D MapLocationTexture;

        private static float ModScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static bool IsMinimized { get; set; } = false;
        private bool ShowUI = false;
        private bool ShowMapsUI = false;

        private static ItemsManager LocalItemsManager;
        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;

        public static Rect ModTeleporterScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
        public static Rect ModConfirmFastTravelDialogWindow = new Rect(Screen.width / 2f, Screen.height / 2f, 450f, 100f);

        public static Dictionary<int, MapLocation> MapLocations = new Dictionary<int, MapLocation>();
        public static Dictionary<MapLocation, Vector3> MapGpsCoordinates = new Dictionary<MapLocation, Vector3>();

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public static GameObject MapLocationObject = new GameObject(nameof(MapLocation));
        public static string CustomX { get; set; } = string.Empty;
        public static string CustomY { get; set; } = string.Empty;
        public static string CustomZ { get; set; } = string.Empty;

        public static Vector3 CustomGpsCoordinates = Vector3.zero;
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
        public static string PermissionChangedMessage(string permission, string reason) => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
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
            string reason = optionValue ? "the game host allowed usage" : "the game host did not allow usage";
            IsModActiveForMultiplayer = optionValue;

            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted", $"{reason}"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked", $"{reason}"), MessageType.Info, Color.yellow))
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
                if (!MapGpsCoordinates.ContainsKey(location))
                {
                    MapGpsCoordinates.Add(location, Vector3.zero);
                }
            }
        }

        private void InitMapPositions()
        {
            MapGpsCoordinates[MapLocation.Teleport_Start_Location] = LocalPlayer.transform.position;
            MapGpsCoordinates[MapLocation.Bamboo_Bridge] = new Vector3(831.159f, 138.608f, 1620.014f);
            MapGpsCoordinates[MapLocation.Anaconda_Island] = new Vector3(898.0696f, 136.465f, 1425.064f);
            MapGpsCoordinates[MapLocation.East_Native_Camp] = new Vector3(802.765f, 129.871f, 1675.741f);
            MapGpsCoordinates[MapLocation.Elevator_Cave] = new Vector3(688.0139f, 113.0132f, 1704.087f);
            MapGpsCoordinates[MapLocation.Planecrash_Cave] = new Vector3(695.7698f, 123.5581f, 1488.888f);
            MapGpsCoordinates[MapLocation.Native_Passage] = new Vector3(653.4912f, 138.7564f, 1416.553f);
            MapGpsCoordinates[MapLocation.Overturned_Jeep] = new Vector3(530.194f, 127.8356f, 1753.261f);
            MapGpsCoordinates[MapLocation.Abandoned_Tribal_Village] = new Vector3(465.1541f, 106.5126f, 1408.053f);
            MapGpsCoordinates[MapLocation.West_Native_Camp] = new Vector3(412.365f, 98.77797f, 1704.949f);
            MapGpsCoordinates[MapLocation.Pond] = new Vector3(278.0788f, 101.3528f, 1510.454f);
            MapGpsCoordinates[MapLocation.Puddle] = new Vector3(265.83f, 96.967f, 1500.19f);
            MapGpsCoordinates[MapLocation.Harbor] = new Vector3(237.4533f, 89.79554f, 1659.221f);
            MapGpsCoordinates[MapLocation.Drug_Facility] = new Vector3(290.9244f, 102.471f, 1377.707f);
            MapGpsCoordinates[MapLocation.Bamboo_Camp] = new Vector3(976.809f, 155.6489f, 1309.329f);
            MapGpsCoordinates[MapLocation.Scorpion_Cartel_Cave] = new Vector3(180.9195f, 121.599f, 1276.029f);
            MapGpsCoordinates[MapLocation.Airport] = new Vector3(1166.69f, 179.99f, 1536.7f);
            MapGpsCoordinates[MapLocation.Main_Tribal_Village] = new Vector3(1066.53f, 93.01f, 1060.56f);
            MapGpsCoordinates[MapLocation.Omega_Camp] = new Vector3(1288.869f, 92.616f, 1124.57f);
            MapGpsCoordinates[MapLocation.Tutorial_Camp] = new Vector3(1198.763f, 98.715f, 1122.541f);
            MapGpsCoordinates[MapLocation.Story_Start_Oasis] = new Vector3(496.5056f, 99.32371f, 1202.032f);
            MapGpsCoordinates[MapLocation.Custom] = CustomGpsCoordinates;

            MapDebugSpawnPositions();
        }

        private void MapDebugSpawnPositions()
        {
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_02] = new Vector3(522.948f, 119.284f, 1499.94f);
            MapGpsCoordinates[MapLocation.BadWater_05] = new Vector3(813.9901f, 117.4782f, 1060.074f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_08] = new Vector3(211.7264f, 95.02083f, 1634.325f);
            MapGpsCoordinates[MapLocation.food_ration_17_liane] = new Vector3(660.285f, 118.8519f, 1015.001f);
            MapGpsCoordinates[MapLocation.Patrol_12_Totem_01] = new Vector3(362.2289f, 121.9925f, 1057.084f);
            MapGpsCoordinates[MapLocation.A04S01_b] = new Vector3(676.2888f, 152.0546f, 1329.49f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_09] = new Vector3(692.1146f, 129.0137f, 1585.105f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner3] = new Vector3(787.1014f, 125.4043f, 1680.41f);
            MapGpsCoordinates[MapLocation.PT_A03S03_TribeVillage] = new Vector3(1067.149f, 93.396f, 1061.35f);
            MapGpsCoordinates[MapLocation.A01S12_WhaCaveCamp] = new Vector3(978.8785f, 155.7854f, 1307.038f);
            MapGpsCoordinates[MapLocation.food_ration_01_mound] = new Vector3(790.4476f, 96.26326f, 945.369f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BadWaterCaveSpawner] = new Vector3(758.4679f, 117.5116f, 1124.531f);
            MapGpsCoordinates[MapLocation.A04S01_c_Barrel1] = new Vector3(752.6547f, 118.647f, 1128.953f);
            MapGpsCoordinates[MapLocation.food_ration_22_liane] = new Vector3(387.4198f, 118.9094f, 1111.187f);
            MapGpsCoordinates[MapLocation.PT_A01S05_River] = new Vector3(172.7413f, 91.34228f, 1525.934f);
            MapGpsCoordinates[MapLocation.PT_A01S06_Jungle] = new Vector3(243.3692f, 122.7701f, 1251.824f);
            MapGpsCoordinates[MapLocation.A04_S02_c_hot_river] = new Vector3(381.0886f, 123.9664f, 839.0045f);
            MapGpsCoordinates[MapLocation.A01S07_Village] = new Vector3(461.637f, 106.742f, 1400.653f);
            MapGpsCoordinates[MapLocation.A04_S01_f_hub_village] = new Vector3(677.5998f, 118.8233f, 1086.573f);
            MapGpsCoordinates[MapLocation.Bike_01] = new Vector3(470.3037f, 106.1938f, 1002.907f);
            MapGpsCoordinates[MapLocation.food_ration_04_mound] = new Vector3(307.2319f, 122.3167f, 1032.725f);
            MapGpsCoordinates[MapLocation.PT_A01S08_PlaneCrash] = new Vector3(695.7698f, 123.5581f, 1488.888f);
            MapGpsCoordinates[MapLocation.PoisonedWater10] = new Vector3(386.1729f, 84.91829f, 1786.645f);
            MapGpsCoordinates[MapLocation.PVE2_Wounded_05_cave] = new Vector3(432.5529f, 106.36f, 1269.952f);
            MapGpsCoordinates[MapLocation.WomanInCage_02] = new Vector3(391.5238f, 131.5998f, 1138.18f);
            MapGpsCoordinates[MapLocation.PoisonedWater06] = new Vector3(459.6075f, 100.9277f, 1581.537f);
            MapGpsCoordinates[MapLocation.A04_S01_g_POI_on_hills] = new Vector3(383.5723f, 131.5995f, 1143.288f);
            MapGpsCoordinates[MapLocation.PT_A02S02_Lake] = new Vector3(1332.721f, 141.407f, 1564.753f);
            MapGpsCoordinates[MapLocation.Bike_07] = new Vector3(668.6342f, 132.9498f, 1273.304f);
            MapGpsCoordinates[MapLocation.A04_S01_g_passage_to_A01_S06] = new Vector3(351.4653f, 135.2879f, 1229.527f);
            MapGpsCoordinates[MapLocation.BadWater_01] = new Vector3(682.3644f, 104.0403f, 949.6216f);
            MapGpsCoordinates[MapLocation.PVE2_Wounded_04] = new Vector3(389.45f, 89.649f, 1827.353f);
            MapGpsCoordinates[MapLocation.PT_A03S01_WHACamp] = new Vector3(1285.241f, 92.50415f, 1121.313f);
            MapGpsCoordinates[MapLocation.A01S06_Cartel_Cave] = new Vector3(180.9195f, 121.599f, 1276.029f);
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_18] = new Vector3(303.2386f, 91.37659f, 1783.672f);
            MapGpsCoordinates[MapLocation.food_ration_24_liane] = new Vector3(587.2971f, 128.1964f, 1235.133f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_axe] = new Vector3(163.431f, 110.334f, 1777.722f);
            MapGpsCoordinates[MapLocation.PT_A01S01_TribeCamp] = new Vector3(412.365f, 98.77797f, 1704.949f);
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_14] = new Vector3(810.2097f, 132.729f, 1680.386f);
            MapGpsCoordinates[MapLocation.food_ration_11_liane] = new Vector3(697.466f, 117.237f, 954.044f);
            MapGpsCoordinates[MapLocation.Cartel_Antena] = new Vector3(275.5441f, 106.631f, 1358.847f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_PantherSpawnerOutside] = new Vector3(441.4531f, 108.6852f, 1805.859f);
            MapGpsCoordinates[MapLocation.Canoe_01] = new Vector3(256.271f, 94.90238f, 1488.79f);
            MapGpsCoordinates[MapLocation.PoisonedWater05] = new Vector3(355.6259f, 100.0621f, 1359.098f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner6] = new Vector3(267.8595f, 116.748f, 1269.525f);
            MapGpsCoordinates[MapLocation.Drum_05] = new Vector3(574.2976f, 121.7476f, 1366.772f);
            MapGpsCoordinates[MapLocation.A04_S01_c_waterfall_caves] = new Vector3(755.915f, 119.0372f, 1128.727f);
            MapGpsCoordinates[MapLocation.PT_A01S01_CaveCamp] = new Vector3(203.7717f, 107.2649f, 1742.105f);
            MapGpsCoordinates[MapLocation.PVE2_Kid_02] = new Vector3(664.438f, 123.43f, 1600.454f);
            MapGpsCoordinates[MapLocation.A04S01_g] = new Vector3(459.1981f, 120.9511f, 1122.591f);
            MapGpsCoordinates[MapLocation.Patrol_11_Totem_01] = new Vector3(377.6392f, 130.9585f, 1189.377f);
            MapGpsCoordinates[MapLocation.SmallAICamp_05] = new Vector3(423.0704f, 116.412f, 1021.444f);
            MapGpsCoordinates[MapLocation.PT_A01S08_Jungle] = new Vector3(638.9672f, 128.6383f, 1615.468f);
            MapGpsCoordinates[MapLocation.HangedBody_08] = new Vector3(803.2322f, 114.541f, 1048.142f);
            MapGpsCoordinates[MapLocation.Bike_02] = new Vector3(336.4749f, 130.8466f, 1034.524f);
            MapGpsCoordinates[MapLocation.PT_A01S10_TribeCamp] = new Vector3(802.765f, 129.871f, 1675.741f);
            MapGpsCoordinates[MapLocation.PT_A01S09_Elevator] = new Vector3(688.0139f, 113.0132f, 1704.087f);
            MapGpsCoordinates[MapLocation.SmallAICamp_01] = new Vector3(803.2377f, 112.4031f, 1020.303f);
            MapGpsCoordinates[MapLocation.A04_S01_e_muddy_gorges] = new Vector3(544.3622f, 126.1366f, 1076.951f);
            MapGpsCoordinates[MapLocation.A04_S01_e_giant_cave] = new Vector3(458.5592f, 105.8317f, 1042.345f);
            MapGpsCoordinates[MapLocation.A04_S01_c_mangrove_border] = new Vector3(879.2043f, 97.11124f, 888.9434f);
            MapGpsCoordinates[MapLocation.Caves_7] = new Vector3(584.8429f, 128.7122f, 1241.692f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner2] = new Vector3(750.488f, 123.4451f, 1591.867f);
            MapGpsCoordinates[MapLocation.food_ration_05_mound] = new Vector3(433.4283f, 126.0453f, 1177.692f);
            MapGpsCoordinates[MapLocation.A01S07_RockToCartel] = new Vector3(461.1687f, 110.8092f, 1481.069f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner7] = new Vector3(785.7932f, 134.0396f, 1560.431f);
            MapGpsCoordinates[MapLocation.PVE2_Wounded_04_cave] = new Vector3(381.1151f, 85.40839f, 1790.079f);
            MapGpsCoordinates[MapLocation.Patrol_09_Totem_02] = new Vector3(760.786f, 118.3547f, 1129.719f);
            MapGpsCoordinates[MapLocation.PT_A02S01_Airport] = new Vector3(1163.051f, 179.8508f, 1534.859f);
            MapGpsCoordinates[MapLocation.HangedBody_04] = new Vector3(608.5613f, 119.6014f, 1138.393f);
            MapGpsCoordinates[MapLocation.Caves_1] = new Vector3(703.708f, 95.87952f, 933.8008f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner4] = new Vector3(549.2175f, 126.8764f, 1750.412f);
            MapGpsCoordinates[MapLocation.A04S01_d] = new Vector3(692.6444f, 93.865f, 906.1113f);
            MapGpsCoordinates[MapLocation.HangedBody_11] = new Vector3(625.6741f, 134.1326f, 1241.724f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_08] = new Vector3(580.632f, 143.34f, 1542.519f);
            MapGpsCoordinates[MapLocation.Panther_04] = new Vector3(690.197f, 113.1189f, 1705.656f);
            MapGpsCoordinates[MapLocation.PT_A01S06_Cartel] = new Vector3(284.9425f, 107.07f, 1356.103f);
            MapGpsCoordinates[MapLocation.Caves_12] = new Vector3(418.3984f, 118.4585f, 1025.398f);
            MapGpsCoordinates[MapLocation.PVE2_Wounded_03] = new Vector3(707.7161f, 123.889f, 1479.558f);
            MapGpsCoordinates[MapLocation.A02S01_Cenot] = new Vector3(1201.263f, 161.8474f, 1465.778f);
            MapGpsCoordinates[MapLocation.A01S07_GrabbingHook] = new Vector3(625.3212f, 132.5684f, 1429.54f);
            MapGpsCoordinates[MapLocation.A04S02_c] = new Vector3(539.9725f, 124.804f, 745.6565f);
            MapGpsCoordinates[MapLocation.Patrol_06_Totem_02] = new Vector3(739.7904f, 128.055f, 1251.812f);
            MapGpsCoordinates[MapLocation.PT_A03S02_Tutorial] = new Vector3(1199.705f, 98.51162f, 1131.864f);
            MapGpsCoordinates[MapLocation.Patrol_10_Totem_01] = new Vector3(419.8765f, 124.898f, 1153.789f);
            MapGpsCoordinates[MapLocation.Drum_08] = new Vector3(684.3138f, 125.2835f, 1490.746f);
            MapGpsCoordinates[MapLocation.Bike_06] = new Vector3(431.0923f, 126.6285f, 1179.814f);
            MapGpsCoordinates[MapLocation.Canoe_03] = new Vector3(311.121f, 90.985f, 1589.89f);
            MapGpsCoordinates[MapLocation.Panther_01] = new Vector3(757.5928f, 121.3835f, 1761.791f);
            MapGpsCoordinates[MapLocation.Albino_01] = new Vector3(472.9952f, 122.0745f, 1085.081f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_10] = new Vector3(350.8103f, 97.0634f, 1614.13f);
            MapGpsCoordinates[MapLocation.A04_S01_b_big_enemy_camp] = new Vector3(861.9202f, 144.5093f, 1214.127f);
            MapGpsCoordinates[MapLocation.Panther_03] = new Vector3(469.9473f, 100.9449f, 1564.911f);
            MapGpsCoordinates[MapLocation.PVE2_Wounded_03_cave] = new Vector3(696.2001f, 123.889f, 1487.78f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_07] = new Vector3(205.072f, 105.769f, 1442.941f);
            MapGpsCoordinates[MapLocation.Statue_07] = new Vector3(885.9794f, 142.4441f, 1239.152f);
            MapGpsCoordinates[MapLocation.PT_A03S01_Rozlewiska] = new Vector3(1357.723f, 91.72222f, 1260.933f);
            MapGpsCoordinates[MapLocation.Caves_10] = new Vector3(512.0596f, 113.136f, 988.2018f);
            MapGpsCoordinates[MapLocation.PVE2_BigAICamp_05] = new Vector3(781.6992f, 134.0867f, 1558.147f);
            MapGpsCoordinates[MapLocation.PVE2_BigAICamp_04] = new Vector3(601.2031f, 131.8043f, 1786.49f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_05] = new Vector3(492.3003f, 116.0034f, 1629.679f);
            MapGpsCoordinates[MapLocation.PT_A02S01_Cenote] = new Vector3(1214.44f, 157.0768f, 1471.841f);
            MapGpsCoordinates[MapLocation.A03S01_StoneRing] = new Vector3(1397.625f, 93.70249f, 1139.184f);
            MapGpsCoordinates[MapLocation.SmallAICamp_08] = new Vector3(614.4554f, 121.1317f, 1036.058f);
            MapGpsCoordinates[MapLocation.WomanInCage_03] = new Vector3(906.6865f, 141.776f, 1227.885f);
            MapGpsCoordinates[MapLocation.A04_S01_f_shaman_passage] = new Vector3(579.5886f, 119.3307f, 883.2385f);
            MapGpsCoordinates[MapLocation.Patrol_02_Totem_01] = new Vector3(766.6841f, 105.8912f, 1008.141f);
            MapGpsCoordinates[MapLocation.Panther_07] = new Vector3(590.287f, 145.891f, 1555.174f);
            MapGpsCoordinates[MapLocation.PT_A01S07_Jungle] = new Vector3(464.6006f, 100.8703f, 1283.359f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner8] = new Vector3(486.9749f, 107.2307f, 1440.981f);
            MapGpsCoordinates[MapLocation.PoisonedWater07] = new Vector3(298.3656f, 91.24091f, 1605.632f);
            MapGpsCoordinates[MapLocation.Caves_4] = new Vector3(850.405f, 144.5191f, 1197.708f);
            MapGpsCoordinates[MapLocation.Patrol_13_Totem_02] = new Vector3(504.2822f, 115.2109f, 1021.302f);
            MapGpsCoordinates[MapLocation.Drum_01] = new Vector3(569.0981f, 113.2871f, 1417.02f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner5] = new Vector3(519.1549f, 124.8329f, 1745.209f);
            MapGpsCoordinates[MapLocation.food_ration_13_mound] = new Vector3(890.9215f, 114.2396f, 1011.991f);
            MapGpsCoordinates[MapLocation.HangedBody_07] = new Vector3(791.2347f, 142.8185f, 1254.485f);
            MapGpsCoordinates[MapLocation.A03S01_Camp] = new Vector3(1288.869f, 92.616f, 1124.57f);
            MapGpsCoordinates[MapLocation.Patrol_05_Totem_01] = new Vector3(615.3488f, 117.8456f, 1165.385f);
            MapGpsCoordinates[MapLocation.food_ration_09_liane] = new Vector3(692.1797f, 118.3558f, 1198.036f);
            MapGpsCoordinates[MapLocation.Albino_03] = new Vector3(637.0143f, 114.126f, 903.7427f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_AlbinoSpawner] = new Vector3(874.5867f, 92.24718f, 867.4756f);
            MapGpsCoordinates[MapLocation.PoisonedWater09] = new Vector3(256.3673f, 95.61769f, 1497.594f);
            MapGpsCoordinates[MapLocation.Wounded_04] = new Vector3(592.7279f, 127.6568f, 1239.862f);
            MapGpsCoordinates[MapLocation.Statue_02] = new Vector3(850.3285f, 159.7452f, 1300.885f);
            MapGpsCoordinates[MapLocation.food_ration_19_liane] = new Vector3(596.986f, 120.228f, 1133.384f);
            MapGpsCoordinates[MapLocation.A01S10_BambooBridge] = new Vector3(831.159f, 138.608f, 1620.014f);
            MapGpsCoordinates[MapLocation.ChallangeSP_MightyCamp] = new Vector3(404.336f, 103.705f, 1589.994f);
            MapGpsCoordinates[MapLocation.Patrol_04_Totem_02] = new Vector3(640.2123f, 114.3334f, 1063.411f);
            MapGpsCoordinates[MapLocation.PoisonedWater01] = new Vector3(630.5267f, 125.5038f, 1470.106f);
            MapGpsCoordinates[MapLocation.Albino_04] = new Vector3(739.8661f, 105.1263f, 1023.71f);
            MapGpsCoordinates[MapLocation.Albino_06] = new Vector3(811.7853f, 98.88179f, 944.3593f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner1] = new Vector3(688.9691f, 123.2495f, 1512.93f);
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_16] = new Vector3(289.8511f, 114.7836f, 1276.267f);
            MapGpsCoordinates[MapLocation.PVE_Boat] = new Vector3(710.8521f, 92.144f, 882.7271f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_06] = new Vector3(391.0233f, 111.39f, 1341.038f);
            MapGpsCoordinates[MapLocation.A04S01_e_Barrel3] = new Vector3(533.7843f, 114.296f, 1036.738f);
            MapGpsCoordinates[MapLocation.PoisonedWater03] = new Vector3(511.2852f, 106.0121f, 1387.515f);
            MapGpsCoordinates[MapLocation.PT_A01S03_Jungle] = new Vector3(561.4174f, 132.9026f, 1648.427f);
            MapGpsCoordinates[MapLocation.PoisonedWater04] = new Vector3(481.489f, 101.3328f, 1322.913f);
            MapGpsCoordinates[MapLocation.A01S07_StoneRings] = new Vector3(653.4912f, 138.7564f, 1416.553f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_07] = new Vector3(527.3976f, 98.689f, 1189.155f);
            MapGpsCoordinates[MapLocation.A04S02_b] = new Vector3(413.6495f, 122.87f, 869.0811f);
            MapGpsCoordinates[MapLocation.Patrol_10_Totem_03] = new Vector3(558.1787f, 117.5632f, 1133.064f);
            MapGpsCoordinates[MapLocation.SmallAICamp_03] = new Vector3(592.4236f, 120.0635f, 1116.235f);
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_13] = new Vector3(583.5927f, 138.519f, 1678.677f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_06] = new Vector3(511.412f, 128.096f, 1788.46f);
            MapGpsCoordinates[MapLocation.QA_Test] = new Vector3(1048.858f, 178.05f, 1815.702f);
            MapGpsCoordinates[MapLocation.PT_A01S03_Jeep] = new Vector3(530.194f, 127.8356f, 1753.261f);
            MapGpsCoordinates[MapLocation.PoisonedWater08] = new Vector3(497.1485f, 98.68053f, 1199.397f);
            MapGpsCoordinates[MapLocation.food_ration_23_mound] = new Vector3(323.3276f, 134.9875f, 1150.45f);
            MapGpsCoordinates[MapLocation.Patrol_09_Totem_01] = new Vector3(680.0205f, 118.6463f, 1157.818f);
            MapGpsCoordinates[MapLocation.SmallAICamp_02] = new Vector3(764.2464f, 154.4589f, 1277.015f);
            MapGpsCoordinates[MapLocation.A04S01_c] = new Vector3(832.6035f, 125.5567f, 1037.173f);
            MapGpsCoordinates[MapLocation.A04S01_c_Destroy] = new Vector3(774.5425f, 105.3434f, 999.1068f);
            MapGpsCoordinates[MapLocation.Statue_04] = new Vector3(773.095f, 150.9389f, 1291.473f);
            MapGpsCoordinates[MapLocation.BadWater_07] = new Vector3(334.6281f, 130.498f, 1217.956f);
            MapGpsCoordinates[MapLocation.A01S09_Elevator] = new Vector3(687.057f, 113.0616f, 1702.976f);
            MapGpsCoordinates[MapLocation.A03S01_CaveAyuhaska] = new Vector3(1323.433f, 92.68927f, 1218.526f);
            MapGpsCoordinates[MapLocation.A04_S01_c_waterfalls_island] = new Vector3(760.7376f, 105.8637f, 1008.879f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_PantherSpawner] = new Vector3(418.2625f, 99.71472f, 1851.349f);
            MapGpsCoordinates[MapLocation.A04_S01_f_passage_to_A01_S04] = new Vector3(574.887f, 116.7671f, 1213.017f);
            MapGpsCoordinates[MapLocation.SmallAICamp_09] = new Vector3(813.6691f, 122.3933f, 1167.047f);
            MapGpsCoordinates[MapLocation.Kid_02] = new Vector3(535.14f, 125.63f, 1069.71f);
            MapGpsCoordinates[MapLocation.food_ration_10_mound] = new Vector3(793.3749f, 155.0388f, 1307.313f);
            MapGpsCoordinates[MapLocation.Patrol_14_Totem_02] = new Vector3(861.7703f, 144.6385f, 1225.617f);
            MapGpsCoordinates[MapLocation.SmallAICamp_06] = new Vector3(361.7151f, 135.079f, 1207.261f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_11] = new Vector3(681.452f, 116.383f, 1685.972f);
            MapGpsCoordinates[MapLocation.A04_S01_c_passage_to_A03_S03] = new Vector3(868.2277f, 118.4762f, 1003.092f);
            MapGpsCoordinates[MapLocation.Caves_6] = new Vector3(768.959f, 150.413f, 1309.557f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_13] = new Vector3(566.628f, 136.635f, 1735.834f);
            MapGpsCoordinates[MapLocation.Panther_06] = new Vector3(399.0686f, 96.45319f, 1803.948f);
            MapGpsCoordinates[MapLocation.food_ration_03_mound] = new Vector3(450.9729f, 111.6943f, 1054.663f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_01] = new Vector3(169.542f, 98.355f, 1631.49f);
            MapGpsCoordinates[MapLocation.A01S11_SQPlace] = new Vector3(1016.496f, 145.27f, 1645.914f);
            MapGpsCoordinates[MapLocation.A04_S01_d_pve_boat] = new Vector3(696.5009f, 93.12092f, 905.0714f);
            MapGpsCoordinates[MapLocation.Statue_01] = new Vector3(539.4466f, 115.0281f, 1126.414f);
            MapGpsCoordinates[MapLocation.PVE2_Kid_04] = new Vector3(518.5273f, 105.8104f, 1385.195f);
            MapGpsCoordinates[MapLocation.Caves_15] = new Vector3(424.0854f, 125.8335f, 1171.411f);
            MapGpsCoordinates[MapLocation.SmallAICamp_01_outer] = new Vector3(792.3371f, 101.0916f, 959.8417f);
            MapGpsCoordinates[MapLocation.PT_A01S11_JungleBamboo] = new Vector3(961.0281f, 134.4039f, 1629.057f);
            MapGpsCoordinates[MapLocation.food_ration_25_mound] = new Vector3(718.207f, 126.047f, 1256.917f);
            MapGpsCoordinates[MapLocation.HangedBody_03] = new Vector3(564.0624f, 121.6178f, 997.6534f);
            MapGpsCoordinates[MapLocation.HangedBody_13] = new Vector3(709.1241f, 118.3389f, 1163.439f);
            MapGpsCoordinates[MapLocation.A04_S01_b_passage_to_A01_S07] = new Vector3(673.0984f, 151.4464f, 1326.6f);
            MapGpsCoordinates[MapLocation.Hub_Village_Outside] = new Vector3(687.7271f, 117.946f, 1101.839f);
            MapGpsCoordinates[MapLocation.Debug] = new Vector3(2044.52f, 94.9765f, 3.54f);
            MapGpsCoordinates[MapLocation.Statue_05] = new Vector3(869.5416f, 123.1762f, 1118.95f);
            MapGpsCoordinates[MapLocation.Panther_05] = new Vector3(588.8813f, 142.2167f, 1717.966f);
            MapGpsCoordinates[MapLocation.PT_A01S07_Jungle2] = new Vector3(531.0518f, 119.1025f, 1497.803f);
            MapGpsCoordinates[MapLocation.A04_S02_e_river_canyons] = new Vector3(502.4138f, 115.7081f, 658.8477f);
            MapGpsCoordinates[MapLocation.ChallangeSP_Combat_WT] = new Vector3(417.2121f, 104.01f, 1722.729f);
            MapGpsCoordinates[MapLocation.food_ration_06_liane] = new Vector3(529.017f, 114.4495f, 1111.192f);
            MapGpsCoordinates[MapLocation.HangedBody_06] = new Vector3(384.9974f, 132.6187f, 1195.144f);
            MapGpsCoordinates[MapLocation.PVE2_Kid_01] = new Vector3(173.5f, 93.241f, 1542.29f);
            MapGpsCoordinates[MapLocation.A02S02_Pond] = new Vector3(1333.651f, 138.657f, 1525.374f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_09] = new Vector3(710.628f, 124.056f, 1475.585f);
            MapGpsCoordinates[MapLocation.Patrol_08_Totem_02] = new Vector3(855.1447f, 141.0819f, 1183.817f);
            MapGpsCoordinates[MapLocation.Canoe_05] = new Vector3(164.0597f, 109.9217f, 1764.809f);
            MapGpsCoordinates[MapLocation.PVE2_Wounded_02_cave] = new Vector3(217.757f, 127.2224f, 1258.352f);
            MapGpsCoordinates[MapLocation.PVE2_BigAICamp_03] = new Vector3(375.2101f, 100.0055f, 1474.17f);
            MapGpsCoordinates[MapLocation.PT_A01S01_Harbor] = new Vector3(237.4533f, 89.79554f, 1659.221f);
            MapGpsCoordinates[MapLocation.Albino_02] = new Vector3(500.098f, 124.081f, 1001.522f);
            MapGpsCoordinates[MapLocation.HangedBody_09] = new Vector3(787.1943f, 93.673f, 917.2256f);
            MapGpsCoordinates[MapLocation.Wounded_02] = new Vector3(454.0277f, 108.3823f, 1046.644f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_05] = new Vector3(209.42f, 128.232f, 1260.999f);
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_20] = new Vector3(190.0014f, 107.0012f, 1438.801f);
            MapGpsCoordinates[MapLocation.Caves_8] = new Vector3(528.8697f, 114.4794f, 1110.941f);
            MapGpsCoordinates[MapLocation.A01S09_GoldMine] = new Vector3(699.233f, 54.684f, 1817.462f);
            MapGpsCoordinates[MapLocation.A01S02] = new Vector3(420.5364f, 104.306f, 1609.757f);
            MapGpsCoordinates[MapLocation.SmallAICamp_10] = new Vector3(904.7721f, 141.4325f, 1226.235f);
            MapGpsCoordinates[MapLocation.WomanInCage_01] = new Vector3(858.1819f, 101.1809f, 934.8375f);
            MapGpsCoordinates[MapLocation.HangedBody_10] = new Vector3(718.6204f, 117.7737f, 1102.757f);
            MapGpsCoordinates[MapLocation.A04S01_a] = new Vector3(659.0945f, 120.2171f, 1209.892f);
            MapGpsCoordinates[MapLocation.food_ration_07_mound] = new Vector3(560.7239f, 121.1232f, 1193.243f);
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_11] = new Vector3(191.676f, 94.978f, 1642.514f);
            MapGpsCoordinates[MapLocation.PVE2_Wounded_01] = new Vector3(178.1107f, 108.6276f, 1762.573f);
            MapGpsCoordinates[MapLocation.A01S12_GHtoAirport] = new Vector3(1037.189f, 159.753f, 1441.974f);
            MapGpsCoordinates[MapLocation.Drum_03] = new Vector3(376.4304f, 106.3924f, 1356.631f);
            MapGpsCoordinates[MapLocation.A01S01_CaveCamp] = new Vector3(203.627f, 107.5152f, 1750.827f);
            MapGpsCoordinates[MapLocation.HangedBody_02] = new Vector3(614.7563f, 126.8996f, 1048.983f);
            MapGpsCoordinates[MapLocation.BadWater_06] = new Vector3(879.2523f, 101.1071f, 915.4965f);
            MapGpsCoordinates[MapLocation.MantisSpawner] = new Vector3(583.1886f, 122.8405f, 1002.815f);
            MapGpsCoordinates[MapLocation.PT_A01S01_Jungle] = new Vector3(347.9294f, 98.055f, 1795.069f);
            MapGpsCoordinates[MapLocation.A01S03_Jeep] = new Vector3(528.6524f, 128.076f, 1740.249f);
            MapGpsCoordinates[MapLocation.Village_02_outside] = new Vector3(440.941f, 107.909f, 1374.49f);
            MapGpsCoordinates[MapLocation.Canoe_04] = new Vector3(148.7191f, 89.653f, 1561.06f);
            MapGpsCoordinates[MapLocation.PT_A02S02_Cenote] = new Vector3(1283.802f, 127.8863f, 1431.199f);
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_17] = new Vector3(401.6366f, 103.7196f, 1589.763f);
            MapGpsCoordinates[MapLocation.Caves_11] = new Vector3(489.2458f, 123.7201f, 1086.87f);
            MapGpsCoordinates[MapLocation.A04_S01_b_big_caves] = new Vector3(672.5791f, 118.5276f, 1205.922f);
            MapGpsCoordinates[MapLocation.Kid_04] = new Vector3(759.445f, 99.52f, 956.284f);
            MapGpsCoordinates[MapLocation.Village_02_inside] = new Vector3(465.58f, 106.642f, 1401.01f);
            MapGpsCoordinates[MapLocation.food_ration_08_mound] = new Vector3(646.879f, 120.041f, 1212.501f);
            MapGpsCoordinates[MapLocation.Bike_05] = new Vector3(582.6481f, 128.297f, 1179.854f);
            MapGpsCoordinates[MapLocation.Patrol_04_Totem_01] = new Vector3(631.4865f, 113.2527f, 1027.193f);
            MapGpsCoordinates[MapLocation.food_ration_02_mound] = new Vector3(682.576f, 100.128f, 935.058f);
            MapGpsCoordinates[MapLocation.A04_S01_d_steamboat] = new Vector3(827.8829f, 92.22733f, 853.9062f);
            MapGpsCoordinates[MapLocation.Caves_17] = new Vector3(785.2197f, 119.9109f, 1109.238f);
            MapGpsCoordinates[MapLocation.Canoe_06] = new Vector3(222.568f, 88.427f, 1822.171f);
            MapGpsCoordinates[MapLocation.A04_S02_b_stone_bridge] = new Vector3(469.8594f, 123.8525f, 888.3677f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_10] = new Vector3(760.207f, 117.448f, 1728.752f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_CanoeBoatSpawner] = new Vector3(288.0341f, 88.239f, 1698.035f);
            MapGpsCoordinates[MapLocation.PT_A01S09_GoldMine] = new Vector3(706.1062f, 51.27618f, 1811.088f);
            MapGpsCoordinates[MapLocation.BigAICamp_03] = new Vector3(343.2068f, 124.0894f, 1084.375f);
            MapGpsCoordinates[MapLocation.Caves_14] = new Vector3(356.6201f, 135.51f, 1225.28f);
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_12] = new Vector3(417.0481f, 104.0103f, 1726.759f);
            MapGpsCoordinates[MapLocation.Wounded_01] = new Vector3(795.0052f, 116.959f, 1099.321f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_14] = new Vector3(449.701f, 110.414f, 1682.915f);
            MapGpsCoordinates[MapLocation.food_ration_18_mound] = new Vector3(654.9873f, 117.5151f, 1124.657f);
            MapGpsCoordinates[MapLocation.ChallangeSP_Combat_Battery] = new Vector3(811.5066f, 132.839f, 1683.343f);
            MapGpsCoordinates[MapLocation.Patrol_03_Totem_01] = new Vector3(604.604f, 113.9525f, 991.9693f);
            MapGpsCoordinates[MapLocation.Caves_5] = new Vector3(867.5211f, 159.7484f, 1283.711f);
            MapGpsCoordinates[MapLocation.Caves_3] = new Vector3(879.0111f, 127.0168f, 1108.361f);
            MapGpsCoordinates[MapLocation.Patrol_07_Totem_01] = new Vector3(834.3834f, 142.1804f, 1243.138f);
            MapGpsCoordinates[MapLocation.A01S01_EvilTribeCamp] = new Vector3(410.3247f, 102.0926f, 1715.478f);
            MapGpsCoordinates[MapLocation.BigAICamp_02] = new Vector3(823.118f, 144.7068f, 1267.17f);
            MapGpsCoordinates[MapLocation.PT_A01S07_TribeVillage] = new Vector3(465.1541f, 106.5126f, 1408.053f);
            MapGpsCoordinates[MapLocation.Caves_9] = new Vector3(574.9811f, 114.0972f, 1013.144f);
            MapGpsCoordinates[MapLocation.Panther_02] = new Vector3(477.0969f, 121.1326f, 1746.74f);
            MapGpsCoordinates[MapLocation.A01S06_Cartel] = new Vector3(290.9244f, 102.471f, 1377.707f);
            MapGpsCoordinates[MapLocation.A01S04] = new Vector3(484.0724f, 106.6023f, 1216.35f);
            MapGpsCoordinates[MapLocation.Kid_03] = new Vector3(662.3152f, 128.3752f, 1250.005f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_03] = new Vector3(310.05f, 90.995f, 1588.995f);
            MapGpsCoordinates[MapLocation.BadWater_03] = new Vector3(604.7839f, 130.648f, 1262.504f);
            MapGpsCoordinates[MapLocation.food_ration_14_mound] = new Vector3(738.068f, 126.931f, 1245.324f);
            MapGpsCoordinates[MapLocation.A01S11] = new Vector3(900.389f, 136.99f, 1568.039f);
            MapGpsCoordinates[MapLocation.Kid_01] = new Vector3(656.765f, 118.477f, 1065.82f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_12] = new Vector3(571.504f, 122.567f, 1357.785f);
            MapGpsCoordinates[MapLocation.A04_S02_d_river_canyons_cave] = new Vector3(576.4888f, 113.6037f, 549.1804f);
            MapGpsCoordinates[MapLocation.A01S12_Island] = new Vector3(898.0696f, 136.465f, 1425.064f);
            MapGpsCoordinates[MapLocation.A04_S02_e_big_swamps] = new Vector3(381.8928f, 111.3795f, 713.2042f);
            MapGpsCoordinates[MapLocation.A04S02_e] = new Vector3(346.4843f, 110.55f, 694.1174f);
            MapGpsCoordinates[MapLocation.A01S05_Puddle] = new Vector3(265.83f, 96.967f, 1500.19f);
            MapGpsCoordinates[MapLocation.food_ration_15_liane] = new Vector3(842.9304f, 126.7472f, 1037.069f);
            MapGpsCoordinates[MapLocation.A01S11_EvilTribeCamp] = new Vector3(921.454f, 141.487f, 1657.557f);
            MapGpsCoordinates[MapLocation.A02S01_Airport] = new Vector3(1166.69f, 179.99f, 1536.7f);
            MapGpsCoordinates[MapLocation.HangedBody_01] = new Vector3(662.1343f, 116.9716f, 965.6454f);
            MapGpsCoordinates[MapLocation.food_ration_20_liane] = new Vector3(561.5294f, 114.1614f, 1035.071f);
            MapGpsCoordinates[MapLocation.Patrol_08_Totem_01] = new Vector3(839.8876f, 128.0847f, 1159.375f);
            MapGpsCoordinates[MapLocation.Spikes_Tree_Review_Spawner] = new Vector3(722.8529f, 116.6592f, 1114.485f);
            MapGpsCoordinates[MapLocation.Statue_03] = new Vector3(908.8634f, 127.5488f, 1028.659f);
            MapGpsCoordinates[MapLocation.PT_A01S05_Pond] = new Vector3(278.0788f, 101.3528f, 1510.454f);
            MapGpsCoordinates[MapLocation.HangedBody_05] = new Vector3(485.199f, 126.4417f, 1102.653f);
            MapGpsCoordinates[MapLocation.PT_A01S12_WHACamp] = new Vector3(976.809f, 155.6489f, 1309.329f);
            MapGpsCoordinates[MapLocation.PVE_Map_Crate] = new Vector3(849.7507f, 100.8946f, 831.588f);
            MapGpsCoordinates[MapLocation.HangedBody_12] = new Vector3(388.6857f, 121.256f, 1055.43f);
            MapGpsCoordinates[MapLocation.A03S01_GrabbHookHigh] = new Vector3(1245.456f, 91.85075f, 1206.792f);
            MapGpsCoordinates[MapLocation.PT_A03S03_Jungle] = new Vector3(963.957f, 102.942f, 1040.11f);
            MapGpsCoordinates[MapLocation.A01S10_EvilTribeCamp] = new Vector3(809.3134f, 132.7433f, 1679.564f);
            MapGpsCoordinates[MapLocation.food_ration_21_mound] = new Vector3(432.9763f, 121.0922f, 1081.221f);
            MapGpsCoordinates[MapLocation.Patrol_05_Totem_02] = new Vector3(624.4211f, 119.2852f, 1210.625f);
            MapGpsCoordinates[MapLocation.PT_A01S02_Jungle] = new Vector3(378.9291f, 95.36174f, 1626.851f);
            MapGpsCoordinates[MapLocation.PT_A01S11_Jungle] = new Vector3(884.2253f, 140.969f, 1530.652f);
            MapGpsCoordinates[MapLocation.Statue_06] = new Vector3(758.8989f, 121.1708f, 1219.338f);
            MapGpsCoordinates[MapLocation.Canoe_02] = new Vector3(193.7189f, 125.4563f, 1260.203f);
            MapGpsCoordinates[MapLocation.Drum_07] = new Vector3(782.2725f, 123.9664f, 1597.695f);
            MapGpsCoordinates[MapLocation.Bike_03] = new Vector3(406.6446f, 130.72f, 998.4277f);
            MapGpsCoordinates[MapLocation.PVE2_Wounded_02] = new Vector3(191.3521f, 125f, 1259.16f);
            MapGpsCoordinates[MapLocation.A04_S02_b_sacred_ruins] = new Vector3(412.2589f, 122.8633f, 867.1254f);
            MapGpsCoordinates[MapLocation.A01S01_Harbor] = new Vector3(251.4893f, 89.74746f, 1660.208f);
            MapGpsCoordinates[MapLocation.PT_A02S01_Jungle] = new Vector3(1075.127f, 168.4598f, 1443.548f);
            MapGpsCoordinates[MapLocation.ChallangeSP_Raft] = new Vector3(299.1657f, 88.25962f, 1735.181f);
            MapGpsCoordinates[MapLocation.A03S03_TribeVillage] = new Vector3(1066.53f, 93.01f, 1060.56f);
            MapGpsCoordinates[MapLocation.PVE_StartDebugSpawner] = new Vector3(698.695f, 116.135f, 936.107f);
            MapGpsCoordinates[MapLocation.Caves_13] = new Vector3(306.5822f, 123.5904f, 1051.654f);
            MapGpsCoordinates[MapLocation.A04_S01_b_passage_to_A01_S12] = new Vector3(830.315f, 161.8449f, 1315.05f);
            MapGpsCoordinates[MapLocation.Patrol_12_Totem_02] = new Vector3(357.7899f, 121.5589f, 1019.546f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BadWaterAltarSpawner] = new Vector3(874.16f, 116.0853f, 1142.339f);
            MapGpsCoordinates[MapLocation.A04_S02_c_dead_bodies_water_cave] = new Vector3(531.2287f, 124.7171f, 750.1702f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_Bike] = new Vector3(547.567f, 126.5762f, 1119.016f);
            MapGpsCoordinates[MapLocation.Drum_06] = new Vector3(765.4966f, 134.468f, 1536.677f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_02] = new Vector3(407.422f, 98.8f, 1784.038f);
            MapGpsCoordinates[MapLocation.A04S01_f] = new Vector3(676.9086f, 118.4762f, 1185.95f);
            MapGpsCoordinates[MapLocation.Wounded_05] = new Vector3(871.437f, 159.1508f, 1264.011f);
            MapGpsCoordinates[MapLocation.Drum_02] = new Vector3(646.8541f, 125.5757f, 1453.645f);
            MapGpsCoordinates[MapLocation.Caves_18] = new Vector3(797.3568f, 94.09662f, 935.3817f);
            MapGpsCoordinates[MapLocation.Bike_04] = new Vector3(386.7533f, 131.5995f, 1131.177f);
            MapGpsCoordinates[MapLocation.PT_A01S12_RefugeeIsland] = new Vector3(899.3753f, 136.208f, 1424.16f);
            MapGpsCoordinates[MapLocation.Caves_2] = new Vector3(861.5999f, 118.4731f, 999.8885f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_AlbinoSpawner_Outside] = new Vector3(842.0014f, 91.95774f, 896.8022f);
            MapGpsCoordinates[MapLocation.Kid_05] = new Vector3(363.0977f, 122.402f, 1052f);
            MapGpsCoordinates[MapLocation.ChallangeSP_FireCamp] = new Vector3(677.4979f, 129.5853f, 1566.251f);
            MapGpsCoordinates[MapLocation.food_ration_16_mound] = new Vector3(823.552f, 104.811f, 960.18f);
            MapGpsCoordinates[MapLocation.BadWater_04] = new Vector3(675.0228f, 115.4239f, 964.4216f);
            MapGpsCoordinates[MapLocation.LegendaryQuest_BadWaterBoatSpawner] = new Vector3(856.955f, 93.867f, 837.702f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_03] = new Vector3(774.035f, 129.143f, 1651.353f);
            MapGpsCoordinates[MapLocation.Patrol_10_Totem_02] = new Vector3(481.6177f, 118.0104f, 1129.094f);
            MapGpsCoordinates[MapLocation.A04S01_e] = new Vector3(447.2341f, 111.1141f, 1045.866f);
            MapGpsCoordinates[MapLocation.Patrol_02_Totem_02] = new Vector3(678.3525f, 118.2707f, 990.8765f);
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_15] = new Vector3(245.7734f, 108.9239f, 1448.935f);
            MapGpsCoordinates[MapLocation.PVE2_Wounded_05] = new Vector3(414.9395f, 108.978f, 1254.39f);
            MapGpsCoordinates[MapLocation.A03S01_OutofCenot] = new Vector3(1290.175f, 92.15056f, 1380.169f);
            MapGpsCoordinates[MapLocation.Patrol_06_Totem_01] = new Vector3(697.1671f, 118.4968f, 1190.747f);
            MapGpsCoordinates[MapLocation.BadWater_02] = new Vector3(654.35f, 121.1463f, 1214.704f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_04] = new Vector3(266.128f, 97.458f, 1502.111f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_04] = new Vector3(633.805f, 131.86f, 1583.964f);
            MapGpsCoordinates[MapLocation.Patrol_13_Totem_01] = new Vector3(553.9409f, 113.9781f, 1037.732f);
            MapGpsCoordinates[MapLocation.Drum_04] = new Vector3(528.0157f, 98.74426f, 1195.127f);
            MapGpsCoordinates[MapLocation.A04_S02_d_lake_canyons] = new Vector3(638.569f, 100.836f, 640.5784f);
            MapGpsCoordinates[MapLocation.Caves_16] = new Vector3(622.8455f, 115.1992f, 914.7893f);
            MapGpsCoordinates[MapLocation.Patrol_01_Totem_01] = new Vector3(829.2627f, 113.6137f, 986.3329f);
            MapGpsCoordinates[MapLocation.food_ration_12_liane] = new Vector3(818.2549f, 121.5284f, 1125.887f);
            MapGpsCoordinates[MapLocation.A01S08_CrashedPlane] = new Vector3(691.866f, 123.331f, 1492.652f);
            MapGpsCoordinates[MapLocation.PVE2_Kid_03] = new Vector3(188.6288f, 120.5623f, 1300.699f);
            MapGpsCoordinates[MapLocation.Albino_05] = new Vector3(864.0753f, 101.627f, 943.7858f);
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_01] = new Vector3(290.0611f, 114.7704f, 1272.283f);
            MapGpsCoordinates[MapLocation.PVE2_Wounded_01_cave] = new Vector3(187.747f, 108.275f, 1775.392f);
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_19] = new Vector3(591.97f, 142.1901f, 1581.551f);
            MapGpsCoordinates[MapLocation.Wounded_03] = new Vector3(701.3249f, 97.00243f, 943.8785f);
            MapGpsCoordinates[MapLocation.A04S01_f_Barrel2] = new Vector3(615.5436f, 118.2208f, 1153.271f);
            MapGpsCoordinates[MapLocation.A03S02_TutorialCamp] = new Vector3(1198.763f, 98.715f, 1122.541f);
            MapGpsCoordinates[MapLocation.PoisonedWater02] = new Vector3(571.6883f, 112.1667f, 1456.68f);
            MapGpsCoordinates[MapLocation.A04S02_d] = new Vector3(591.8564f, 114.23f, 732.6316f);
            MapGpsCoordinates[MapLocation.SmallAICamp_04] = new Vector3(649.5515f, 116.9505f, 969.9714f);
            MapGpsCoordinates[MapLocation.Patrol_14_Totem_01] = new Vector3(905.1836f, 141.5217f, 1226.398f);
            MapGpsCoordinates[MapLocation.SmallAICamp_07] = new Vector3(673.9833f, 118.4775f, 1207.747f);
        }

        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
        private static KeyCode ModBindingKeyId { get; set; } = KeyCode.Alpha7;
        public bool ShowMap { get; private set; }

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ModBindingKeyId = GetConfigurableKey(nameof(ModBindingKeyId));

            StartCoroutine(LoadTexture(delegate (Texture2D t)
            {
                MapLocationTexture = t;
            }, MapLocationTextureUrl));
        }

        private KeyCode GetConfigurableKey(string keybindingId)
        {
            KeyCode configuredKeyCode = default;
            string configuredKeybinding = string.Empty;

            try
            {
                //ModAPI.Log.Write($"Searching XML runtime configuration file {RuntimeConfigurationFile}...");
                if (File.Exists(RuntimeConfigurationFile))
                {
                    using (var xmlReader = XmlReader.Create(new StreamReader(RuntimeConfigurationFile)))
                    {
                        //ModAPI.Log.Write($"Reading XML runtime configuration file...");
                        while (xmlReader.Read())
                        {
                            //ModAPI.Log.Write($"Searching configuration for Button with ID = {keybindingId}...");
                            if (xmlReader.ReadToFollowing(nameof(Button)))
                            {
                                if (xmlReader["ID"] == keybindingId)
                                {
                                    //ModAPI.Log.Write($"Found configuration for Button with ID = {keybindingId}!");
                                    configuredKeybinding = xmlReader.ReadElementContentAsString();
                                    //ModAPI.Log.Write($"Configured keybinding = {configuredKeybinding}.");
                                }
                            }
                        }
                    }
                    //ModAPI.Log.Write($"XML runtime configuration\n{File.ReadAllText(RuntimeConfigurationFile)}\n");
                }

                configuredKeyCode = !string.IsNullOrEmpty(configuredKeybinding)
                                                            ? (KeyCode)Enum.Parse(typeof(KeyCode), configuredKeybinding)
                                                            : ModBindingKeyId;
                //ModAPI.Log.Write($"Configured key code: { configuredKeyCode }");
                return configuredKeyCode;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetConfigurableKey));
                return configuredKeyCode;
            }
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

            if (Input.GetKeyDown(ModBindingKeyId))
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

            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                if ( Input.GetKeyDown(KeyCode.M))
                {
                    if (!ShowMap)
                    {
                        InitData();
                        InitMapLocations();
                        EnableCursor(true);
                    }
                    ToggleShowUI(2);
                    if (!ShowMap)
                    {
                        EnableCursor(false);
                    }
                }

                if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
                {
                    InitData();
                    PrintPlayerInfo();
                }

                if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.L))
                {
                    InitData();
                    PrintDebugSpawnerInfo();
                }
            }
        }

        private IEnumerator LoadTexture(Action<Texture2D> action, string url)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();
                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    ModAPI.Log.Write(uwr.error);
                }
                else
                {
                    action(DownloadHandlerTexture.GetContent(uwr));
                }
            }
        }

        private void DrawMapLocations()
        {
            try
            {
                foreach (var mapLocationpGpsCoordinates in MapGpsCoordinates)
                {
                    (float gps_lat, float gps_long) gPSCoordinates = ConvertToGpsCoordinates(mapLocationpGpsCoordinates.Value);
                    GUI.DrawTexture(new Rect(gPSCoordinates.gps_lat - MapLocationIconSize / 2f, gPSCoordinates.gps_long - MapLocationIconSize / 2f, MapLocationIconSize, MapLocationIconSize), MapLocationTexture,ScaleMode.ScaleAndCrop, true);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DrawMapLocations));
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
                case 2:
                    ShowMap = !ShowMap;
                    break;
                default:
                    ShowUI = !ShowUI;
                    ShowMapsUI = !ShowMapsUI;
                    ShowMap = !ShowMap;
                    break;
            }
        }

        private void OnGUI()
        {
            if (ShowMap)
            {
                InitData();
                InitMapLocations();
                DrawMapLocations();
            }
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
                ModConfirmFastTravelDialogWindow = GUILayout.Window(GetHashCode(), ModConfirmFastTravelDialogWindow, InitModConfirmFastTravelDialogWindow, " Teleport?", GUI.skin.window);
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
                    ModOptionsBox();
                    CustomMapLocationBox();
                    MapLocationsBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void OnlyForSingleplayerOrWhenHostBox()
        {
            using (var infoScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUI.color = Color.yellow;
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                GUI.color = Color.white;
            }
        }

        private void ModOptionsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                Color defaultC = GUI.color;
                using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Options for mod behaviour", GUI.skin.label);
                    StatusForMultiplayer();
                    GUILayout.Label($"To teleport to next map location, press [6]", GUI.skin.label);
                    GUILayout.Label($"To show your current GPS position and set these as custom coordinates, press [Left Alt]+[P]", GUI.skin.label);
                    GUILayout.Label($"To log debug spawners gps positions, press [Left Alt]+[L]", GUI.skin.label);
                }
                GUI.color = defaultC;
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void StatusForMultiplayer()
        {
            string reason = string.Empty;
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                GUI.color = Color.green;
                if (IsModActiveForSingleplayer)
                {
                    reason = "you are the game host";
                }
                if (IsModActiveForMultiplayer)
                {
                    reason = "the game host allowed usage";
                }
                GUILayout.Toggle(true, PermissionChangedMessage($"granted", $"{reason}"), GUI.skin.toggle);
            }
            else
            {
                if (!IsModActiveForSingleplayer)
                {
                    reason = "you are not the game host";
                }
                if (!IsModActiveForMultiplayer)
                {
                    reason = "the game host did not allow usage";
                }
                GUI.color = Color.yellow;
                GUILayout.Toggle(false, PermissionChangedMessage($"revoked", $"{reason}"), GUI.skin.toggle);
            }
            GUI.color = Color.white;
        }

        private void CustomMapLocationBox()
        {
            using (var customScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUI.color = Color.cyan;
                GUILayout.Label($"Current custom map location set to GPS coordinates: x: {CustomGpsCoordinates.x}f, y: {CustomGpsCoordinates.y}f, z: {CustomGpsCoordinates.z}f", GUI.skin.label);
                GUI.color = Color.white;
                GUILayout.Label($"Set GPS coordinates x, y and z to bind to your custom map location. Then click [Bind custom].", GUI.skin.label);
                using (var coordScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("x: ", GUI.skin.label);
                    CustomX = GUILayout.TextField(CustomX, GUI.skin.textField);
                    GUILayout.Label("y: ", GUI.skin.label);
                    CustomY = GUILayout.TextField(CustomY, GUI.skin.textField);
                    GUILayout.Label("z: ", GUI.skin.label);
                    CustomZ = GUILayout.TextField(CustomZ, GUI.skin.textField);
                    if (GUILayout.Button("Bind custom", GUI.skin.button, GUILayout.Width(150f)))
                    {
                        if (float.TryParse(CustomX, out CustomGpsCoordinates.x) &&
                        float.TryParse(CustomY, out CustomGpsCoordinates.y) &&
                        float.TryParse(CustomZ, out CustomGpsCoordinates.z))
                        {
                            MapGpsCoordinates[MapLocation.Custom] = CustomGpsCoordinates;
                        }
                        else
                        {
                            ShowHUDBigInfo(HUDBigInfoMessage($"Could not set custom map location\nReason: Invalid GPS coordinates\nx: {CustomX} y: {CustomY} z: {CustomZ}.", MessageType.Warning, Color.yellow));
                        }
                    }
                }
            }
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
                GUILayout.Label($"Teleport to {NextMapLocation.ToString().Replace("_", " ")}?", GUI.skin.label);
                GUI.color = Color.white;
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    if (GUILayout.Button("Yes", GUI.skin.button))
                    {
                        GpsCoordinates = MapGpsCoordinates.GetValueOrDefault(NextMapLocation);
                        if (GpsCoordinates != Vector3.zero)
                        {
                            TeleportToLocation(GpsCoordinates);
                        }
                        else
                        {
                            ShowHUDBigInfo(HUDBigInfoMessage($"GPS coordinates for map location {SelectedMapLocationName} not found!", MessageType.Error, Color.red));
                        }

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
                                TeleportToLocation(GpsCoordinates);
                            }
                            else
                            {
                                ShowHUDBigInfo(HUDBigInfoMessage($"GPS coordinates for map location {SelectedMapLocationName} not found!", MessageType.Error, Color.red));
                            }
                        }
                        else
                        {
                            ShowHUDBigInfo(HUDBigInfoMessage($"Map location {SelectedMapLocationName} not found!", MessageType.Error, Color.red));
                        }
                    }
                    else
                    {
                        ShowHUDBigInfo(HUDBigInfoMessage($"Map location names not found!", MessageType.Error, Color.red));
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickTeleport));
            }
        }

        private (float gps_lat, float gps_long) ConvertToGpsCoordinates(Vector3 position)
        {
            Vector3 position2 = MapTab.Get().m_WorldZeroDummy.position;
            Vector3 position3 = MapTab.Get().m_WorldOneDummy.position;
            float num = position3.x - position2.x;
            float num2 = position3.z - position2.z;
            float num3 = num / 35f;
            float num4 = num2 / 27f;
            Vector3 vector = MapTab.Get().m_WorldZeroDummy.InverseTransformPoint(position);
            float item = vector.x / num3 + 20f;
            float item2 = vector.z / num4 + 14f;
            return (item, item2);
        }

        public void TeleportToLocation(Vector3 gpsCoordinates)
        {
            try
            {
                MapLocationObject.transform.position = gpsCoordinates;
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
                CustomX = playerPosition.x.ToString();
                CustomY = playerPosition.y.ToString();
                CustomZ = playerPosition.z.ToString();
                ShowHUDBigInfo(HUDBigInfoMessage($"Player gps coordinates\nx: {playerPosition.x}, y: {playerPosition.y} z: {playerPosition.z} ", MessageType.Info, Color.green));
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
                StringBuilder logBuilder = new StringBuilder($"");
                DebugSpawner[] array = FindObjectsOfType<DebugSpawner>();
                for (int i = 0; i < array.Length; i++)
                {
                   logBuilder.AppendLine(PositionInfo(array[i].gameObject.transform.position, array[i].gameObject.name));
                }
                ModAPI.Log.Write(logBuilder.ToString());
                ShowHUDBigInfo(HUDBigInfoMessage($"{nameof(DebugSpawner)} info logged to\n{LogPath}.", MessageType.Info, Color.green));
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(PrintDebugSpawnerInfo));
            }
        }

        public string PositionInfo(Vector3 position, string mapLocation = "Teleport_Start_Location")
        {
            try
            {
                string info =$"\nMapGpsCoordinates[MapLocation.{mapLocation}] = new Vector3({position.x}f, {position.y}f, {position.z}f);";
                info += $"\t[W,S] = [{ConvertToGpsCoordinates(position)}]";
                return info;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(PositionInfo));
                return string.Empty;
            }
        }


    }
}
