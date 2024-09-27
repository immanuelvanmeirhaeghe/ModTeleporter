using ModTeleporter.Data.Enums;
using ModTeleporter.Data.Interfaces;
using ModTeleporter.Data.Modding;
using ModTeleporter.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

namespace ModTeleporter
{
    /// <summary>
    /// ModTeleporter is a mod for Green Hell that allows a player to teleport to custom-bound or key map locations in sequence or on selection.
    /// Press Keypad6 (default) or the key configurable in ModAPI to open the mod screen.
    /// Press Alpha6 to fast travel.
    /// Press Left Alt + M to show the map.
    /// Press Left Alt + P to show player GPS info.
    /// Press Left Alt + L  to log debug spawner info to a file.
    /// </summary>
    public class ModTeleporter : MonoBehaviour
    {
        private static ModTeleporter Instance;

        private static readonly string LogPath = $"{Application.dataPath.Replace("GH_Data", "Logs")}/{nameof(ModTeleporter)}.log";
        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
        private static readonly string ModName = nameof(ModTeleporter);
        public string ModTeleporterScreenTitle = $"{ModName} created by [Dragon Legion] Immaanuel#4300";

        private static float ModTeleporterScreenTotalWidth { get; set; } = 700f;
        private static float ModTeleporterScreenTotalHeight { get; set; } = 600f;
        private static float ModTeleporterScreenMinWidth { get; set; } = 700f;
        private static float ModTeleporterScreenMaxWidth { get; set; } = Screen.width;
        private static float ModTeleporterScreenMinHeight { get; set; } = 50f;
        private static float ModTeleporterScreenMaxHeight { get; set; } = Screen.height;
        private static float ModTeleporterScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModTeleporterScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static bool IsModTeleporterScreenMinimized { get; set; } = false;
        private static int ModTeleporterScreenId { get; set; }

        private static float ConfirmFastTravelScreenTotalWidth { get; set; } = 400f;
        private static float ConfirmFastTravelScreenTotalHeight { get; set; } = 400f;
        private static float ConfirmFastTravelScreenMinWidth { get; set; } = 400f;
        private static float ConfirmFastTravelScreenMaxWidth { get; set; } = Screen.width;
        private static float ConfirmFastTravelScreenMinHeight { get; set; } = 50f;
        private static float ConfirmFastTravelScreenMaxHeight { get; set; } = Screen.height;
        private static float ConfirmFastTravelScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ConfirmFastTravelScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static bool IsConfirmFastTravelScreenMinimized { get; set; } = false;

        private static readonly string LocalMapTextureUrl = "https://live.staticflickr.com/65535/52799732305_cb0a8a8394_b.jpg";
        private static readonly float LocalMapLocationMarkerIconSize = 50f;
        private static readonly string LocalMapLocationMarkerTextureUrl = "https://modapi.survivetheforest.net/uploads/objects/9/marker.png";
        private float LocalMapZoom { get; set; } = 1f;
        private Texture2D LocalMapTexture { get; set; }
        private Texture2D LocalMapLocationMarkerTexture { get; set; }

        private Vector2 LocalMapPointerPosition = Vector2.zero;
        private Vector2 MapGridCount { get; set; } = new Vector2(35f, 35f);
        private Vector2 MapGridOffset = new Vector2(19f, 13f);
        private Vector2 MapOffset = new Vector2(4f, 18f);
                
        private bool ShowModTeleporterScreen { get; set; } = false;
        private bool ShowFastTravelScreen { get; set; } = false;
        private bool ShowMap { get; set; } = false;
        private bool ShowModInfo { get; set; } = false;

        private bool GameMapsUnlocked { get; set; } = false;

        private static StylingManager LocalStylingManager;
        private static ItemsManager LocalItemsManager;
        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static MapTab LocalMapTab;

        private KeyCode ShortcutKey { get; set; } = KeyCode.Keypad6;
        private KeyCode FastTravelShortcutKey { get; set; } = KeyCode.Alpha6;
        private KeyCode CustomMapShortcutKey { get; set; } = KeyCode.M;
        private KeyCode PlayerGpsShortcutKey { get; set; } = KeyCode.P;
        private KeyCode LogDebugSpawnerInfoShortcutKey { get; set; } = KeyCode.L;

        private static Rect ModTeleporterScreen = new Rect(ModTeleporterScreenStartPositionX, ModTeleporterScreenStartPositionY, ModTeleporterScreenTotalWidth, ModTeleporterScreenTotalHeight);        
        private static Rect ConfirmFastTravelScreen  = new Rect(ConfirmFastTravelScreenStartPositionX, ConfirmFastTravelScreenStartPositionY, ConfirmFastTravelScreenTotalWidth, ConfirmFastTravelScreenTotalHeight);

        public Dictionary<int, MapLocation> MapLocations = new Dictionary<int, MapLocation>();
        public Dictionary<MapLocation, Vector3> MapGpsCoordinates = new Dictionary<MapLocation, Vector3>();

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public IConfigurableMod SelectedMod { get; set; } = default;

        public static GameObject MapLocationObject = new GameObject(nameof(MapLocation));

        public Vector2 MapLocationsScrollViewPosition { get; set; } = Vector2.zero;
        public string CustomX { get; set; } = string.Empty;
        public string CustomY { get; set; } = string.Empty;
        public string CustomZ { get; set; } = string.Empty;
       
        public Vector3 CustomGpsCoordinates  = Vector3.zero;
        public Vector3 GpsCoordinates = Vector3.zero;

        public Vector2 ModInfoScrollViewPosition { get; set; } = Vector2.zero;

        public MapLocation CurrentMapLocation { get; set; } = MapLocation.Teleport_Start_Location;
        public MapLocation NextMapLocation { get; set; } = MapLocation.Teleport_Start_Location;
        public MapLocation LastMapLocationTeleportedTo { get; set; } = MapLocation.Teleport_Start_Location;
        public string SelectedMapLocationName { get; set; } = string.Empty;
        public int NextMapLocationID { get; set; } = 0;
        public int SelectedMapLocationIndex { get; set; } = 0;
      
        public string[] GetMapLocationNames()
        {
            string[] locationNames = Enum.GetNames(typeof(MapLocation));

            for (int i = 0; i < locationNames.Length; i++)
            {
                string locationName = locationNames[i];
                locationNames[i] = locationName.Replace("_", " ");
            }
            return locationNames;
        }

        private string OnlyForSinglePlayerOrHostMessage()
             => "Only available for single player or when host. Host can activate using ModManager.";
        private string PermissionChangedMessage(string permission, string reason)
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        private string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{(headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))}>{messageType}</color>\n{message}";
        private void OnlyForSingleplayerOrWhenHostBox()
        {
            using (var infoScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), LocalStylingManager.ColoredCommentLabel(Color.yellow));
            }
        }

        public KeyCode GetShortcutKey(string buttonID)
        {
            var ConfigurableModList = GetModList();
            if (ConfigurableModList != null && ConfigurableModList.Count > 0)
            {
                SelectedMod = ConfigurableModList.Find(cfgMod => cfgMod.ID == ModName);
                return SelectedMod.ConfigurableModButtons.Find(cfgButton => cfgButton.ID == buttonID).ShortcutKey;
            }
            else
            {
                return KeyCode.Keypad7;
            }
        }

        private List<IConfigurableMod> GetModList()
        {
            List<IConfigurableMod> modList = new List<IConfigurableMod>();
            try
            {
                if (File.Exists(RuntimeConfigurationFile))
                {
                    using (XmlReader configFileReader = XmlReader.Create(new StreamReader(RuntimeConfigurationFile)))
                    {
                        while (configFileReader.Read())
                        {
                            configFileReader.ReadToFollowing("Mod");
                            do
                            {
                                string gameID = GameID.GreenHell.ToString();
                                string modID = configFileReader.GetAttribute(nameof(IConfigurableMod.ID));
                                string uniqueID = configFileReader.GetAttribute(nameof(IConfigurableMod.UniqueID));
                                string version = configFileReader.GetAttribute(nameof(IConfigurableMod.Version));

                                var configurableMod = new ConfigurableMod(gameID, modID, uniqueID, version);

                                configFileReader.ReadToDescendant("Button");
                                do
                                {
                                    string buttonID = configFileReader.GetAttribute(nameof(IConfigurableModButton.ID));
                                    string buttonKeyBinding = configFileReader.ReadElementContentAsString();

                                    configurableMod.AddConfigurableModButton(buttonID, buttonKeyBinding);

                                } while (configFileReader.ReadToNextSibling("Button"));

                                if (!modList.Contains(configurableMod))
                                {
                                    modList.Add(configurableMod);
                                }

                            } while (configFileReader.ReadToNextSibling("Mod"));
                        }
                    }
                }
                return modList;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetModList));
                modList = new List<IConfigurableMod>();
                return modList;
            }
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc}";
            ModAPI.Log.Write(info);
            ShowHUDBigInfo(HUDBigInfoMessage(exc.Message, MessageType.Error, Color.red));
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

        protected virtual void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalItemsManager = ItemsManager.Get();
            LocalPlayer = Player.Get();
            LocalMapTab = MapTab.Get();
            LocalStylingManager = StylingManager.Get();
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
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_02] = new Vector3(522.948f, 119.284f, 1499.94f); // [W,S] = [(44.81253, 24.01941)]
            MapGpsCoordinates[MapLocation.BadWater_05] = new Vector3(813.9901f, 117.4782f, 1060.074f);  // [W,S] = [(37.7184, 36.01733)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_08] = new Vector3(211.7264f, 95.02083f, 1634.325f);  // [W,S] = [(52.39855, 20.35387)]
            MapGpsCoordinates[MapLocation.food_ration_17_liane] = new Vector3(660.285f, 118.8519f, 1015.001f);  // [W,S] = [(41.46495, 37.24675)]
            MapGpsCoordinates[MapLocation.Patrol_12_01_C9_5] = new Vector3(362.2289f, 121.9925f, 1057.084f);    // [W,S] = [(48.73006, 36.09888)]
            MapGpsCoordinates[MapLocation.A04S01_b] = new Vector3(676.2888f, 152.0546f, 1329.49f);  // [W,S] = [(41.07486, 28.66865)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_09] = new Vector3(692.1146f, 129.0137f, 1585.105f);  // [W,S] = [(40.68911, 21.69642)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner3] = new Vector3(787.1014f, 125.4043f, 1680.41f);  // [W,S] = [(38.37381, 19.09685)]
            MapGpsCoordinates[MapLocation.PT_A03S03_TribeVillage] = new Vector3(1067.149f, 93.396f, 1061.35f);  // [W,S] = [(31.54766, 35.98251)]
            MapGpsCoordinates[MapLocation.A01S12_WhaCaveCamp] = new Vector3(978.8785f, 155.7854f, 1307.038f);   // [W,S] = [(33.69925, 29.28105)]
            MapGpsCoordinates[MapLocation.food_ration_01_mound] = new Vector3(790.4476f, 96.26326f, 945.369f);  // [W,S] = [(38.29224, 39.14606)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BadWaterCaveSpawner] = new Vector3(758.4679f, 117.5116f, 1124.531f);   // [W,S] = [(39.07175, 34.25918)]
            MapGpsCoordinates[MapLocation.A04S01_c_Barrel1] = new Vector3(752.6547f, 118.647f, 1128.953f);  // [W,S] = [(39.21345, 34.13855)]
            MapGpsCoordinates[MapLocation.food_ration_22_liane] = new Vector3(387.4198f, 118.9094f, 1111.187f); // [W,S] = [(48.11603, 34.62317)]
            MapGpsCoordinates[MapLocation.PT_A01S05_River] = new Vector3(172.7413f, 91.34228f, 1525.934f);  // [W,S] = [(53.34881, 23.31038)]
            MapGpsCoordinates[MapLocation.PT_A01S06_Jungle] = new Vector3(243.3692f, 122.7701f, 1251.824f); // [W,S] = [(51.62725, 30.7871)]
            MapGpsCoordinates[MapLocation.A04_S02_c_hot_river] = new Vector3(381.0886f, 123.9664f, 839.0045f);  // [W,S] = [(48.27035, 42.04729)]
            MapGpsCoordinates[MapLocation.A01S07_Village] = new Vector3(461.637f, 106.742f, 1400.653f); // [W,S] = [(46.30699, 26.72759)]
            MapGpsCoordinates[MapLocation.A04_S01_f_hub_village] = new Vector3(677.5998f, 118.8233f, 1086.573f);    // [W,S] = [(41.0429, 35.29454)]
            MapGpsCoordinates[MapLocation.Bike_01] = new Vector3(470.3037f, 106.1938f, 1002.907f);  // [W,S] = [(46.09574, 37.57663)]
            MapGpsCoordinates[MapLocation.food_ration_04_mound] = new Vector3(307.2319f, 122.3167f, 1032.725f); // [W,S] = [(50.07061, 36.7633)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A01_S12_to_A02_S01_02] = new Vector3(1041.067f, 161.0775f, 1441.334f);  // [W,S] = [(32.18341, 25.61797)]
            MapGpsCoordinates[MapLocation.PT_A01S08_PlaneCrash] = new Vector3(695.7698f, 123.5581f, 1488.888f); // [W,S] = [(40.60001, 24.32086)]
            MapGpsCoordinates[MapLocation.PoisonedWater10] = new Vector3(386.1729f, 84.91829f, 1786.645f);  // [W,S] = [(48.14642, 16.19917)]
            MapGpsCoordinates[MapLocation.PVE2_Wounded_05_cave] = new Vector3(432.5529f, 106.36f, 1269.952f);   // [W,S] = [(47.01591, 30.29263)]
            MapGpsCoordinates[MapLocation.WomanInCage_02] = new Vector3(391.5238f, 131.5998f, 1138.18f);    // [W,S] = [(48.016, 33.88689)]
            MapGpsCoordinates[MapLocation.PoisonedWater06] = new Vector3(459.6075f, 100.9277f, 1581.537f);  // [W,S] = [(46.35646, 21.79375)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A02_S02_to_A03_S01] = new Vector3(1281.13f, 99.55441f, 1427.654f);  // [W,S] = [(26.33189, 25.99111)]
            MapGpsCoordinates[MapLocation.A04_S01_g_POI_on_hills] = new Vector3(383.5723f, 131.5995f, 1143.288f);   // [W,S] = [(48.20981, 33.74756)]
            MapGpsCoordinates[MapLocation.PT_A02S02_Lake] = new Vector3(1332.721f, 141.407f, 1564.753f);    // [W,S] = [(25.07434, 22.25155)]
            MapGpsCoordinates[MapLocation.Bike_07] = new Vector3(668.6342f, 132.9498f, 1273.304f);  // [W,S] = [(41.26144, 30.20119)]
            MapGpsCoordinates[MapLocation.A04_S01_g_passage_to_A01_S06] = new Vector3(351.4653f, 135.2879f, 1229.527f); // [W,S] = [(48.99242, 31.39527)]
            MapGpsCoordinates[MapLocation.BadWater_01] = new Vector3(682.3644f, 104.0403f, 949.6216f);  // [W,S] = [(40.92677, 39.03006)]
            MapGpsCoordinates[MapLocation.PVE2_Wounded_04] = new Vector3(389.45f, 89.649f, 1827.353f);  // [W,S] = [(48.06654, 15.08879)]
            MapGpsCoordinates[MapLocation.PT_A03S01_WHACamp] = new Vector3(1285.241f, 92.50415f, 1121.313f);    // [W,S] = [(26.23168, 34.34697)]
            MapGpsCoordinates[MapLocation.A01S06_Cartel_Cave] = new Vector3(180.9195f, 121.599f, 1276.029f);    // [W,S] = [(53.14947, 30.12687)]
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_18] = new Vector3(303.2386f, 91.37659f, 1783.672f);  // [W,S] = [(50.16794, 16.28023)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A01_S07_to_A04_S01b_01] = new Vector3(674.6384f, 146.4894f, 1359.67f);  // [W,S] = [(41.11509, 27.84544)]
            MapGpsCoordinates[MapLocation.food_ration_24_liane] = new Vector3(587.2971f, 128.1964f, 1235.133f); // [W,S] = [(43.24403, 31.24236)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_axe] = new Vector3(163.431f, 110.334f, 1777.722f);   // [W,S] = [(53.57574, 16.44254)]
            MapGpsCoordinates[MapLocation.PT_A01S01_TribeCamp] = new Vector3(412.365f, 98.77797f, 1704.949f);   // [W,S] = [(47.50799, 18.42753)]
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_14] = new Vector3(810.2097f, 132.729f, 1680.386f);   // [W,S] = [(37.81055, 19.09749)]
            MapGpsCoordinates[MapLocation.food_ration_11_liane] = new Vector3(697.466f, 117.237f, 954.044f);    // [W,S] = [(40.55867, 38.90944)]
            MapGpsCoordinates[MapLocation.Cartel_Antena] = new Vector3(275.5441f, 106.631f, 1358.847f); // [W,S] = [(50.84299, 27.8679)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_PantherSpawnerOutside] = new Vector3(441.4531f, 108.6852f, 1805.859f); // [W,S] = [(46.79897, 15.67506)]
            MapGpsCoordinates[MapLocation.Canoe_01] = new Vector3(256.271f, 94.90238f, 1488.79f);   // [W,S] = [(51.31277, 24.32354)]
            MapGpsCoordinates[MapLocation.PoisonedWater05] = new Vector3(355.6259f, 100.0621f, 1359.098f);  // [W,S] = [(48.89101, 27.86105)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner6] = new Vector3(267.8595f, 116.748f, 1269.525f);  // [W,S] = [(51.03031, 30.30428)]
            MapGpsCoordinates[MapLocation.Drum_05] = new Vector3(574.2976f, 121.7476f, 1366.772f);  // [W,S] = [(43.56089, 27.65173)]
            MapGpsCoordinates[MapLocation.A04_S01_c_waterfall_caves] = new Vector3(755.915f, 119.0372f, 1128.727f); // [W,S] = [(39.13398, 34.14473)]
            MapGpsCoordinates[MapLocation.PT_A01S01_CaveCamp] = new Vector3(203.7717f, 107.2649f, 1742.105f);   // [W,S] = [(52.59245, 17.41403)]
            MapGpsCoordinates[MapLocation.PVE2_Kid_02] = new Vector3(664.438f, 123.43f, 1600.454f); // [W,S] = [(41.36372, 21.27775)]
            MapGpsCoordinates[MapLocation.A04S01_g] = new Vector3(459.1981f, 120.9511f, 1122.591f); // [W,S] = [(46.36644, 34.31208)]
            MapGpsCoordinates[MapLocation.Patrol_11_01_C3_10] = new Vector3(377.6392f, 130.9585f, 1189.377f);   // [W,S] = [(48.35443, 32.49042)]
            MapGpsCoordinates[MapLocation.SmallAICamp_05] = new Vector3(423.0704f, 116.412f, 1021.444f);    // [W,S] = [(47.24705, 37.071)]
            MapGpsCoordinates[MapLocation.PT_A01S08_Jungle] = new Vector3(638.9672f, 128.6383f, 1615.468f); // [W,S] = [(41.98457, 20.86823)]
            MapGpsCoordinates[MapLocation.HangedBody_08] = new Vector3(803.2322f, 114.541f, 1048.142f); // [W,S] = [(37.98062, 36.3428)]
            MapGpsCoordinates[MapLocation.Bike_02] = new Vector3(336.4749f, 130.8466f, 1034.524f);  // [W,S] = [(49.35781, 36.71425)]
            MapGpsCoordinates[MapLocation.PT_A01S10_TribeCamp] = new Vector3(802.765f, 129.871f, 1675.741f);    // [W,S] = [(37.99201, 19.22421)]
            MapGpsCoordinates[MapLocation.PT_A01S09_Elevator] = new Vector3(688.0139f, 113.0132f, 1704.087f);   // [W,S] = [(40.78906, 18.45103)]
            MapGpsCoordinates[MapLocation.SmallAICamp_01] = new Vector3(803.2377f, 112.4031f, 1020.303f);   // [W,S] = [(37.98049, 37.10213)]
            MapGpsCoordinates[MapLocation.A04_S01_e_muddy_gorges] = new Vector3(544.3622f, 126.1366f, 1076.951f);   // [W,S] = [(44.29057, 35.55698)]
            MapGpsCoordinates[MapLocation.A04_S01_e_giant_cave] = new Vector3(458.5592f, 105.8317f, 1042.345f); // [W,S] = [(46.38201, 36.5009)]
            MapGpsCoordinates[MapLocation.A04_S01_c_mangrove_border] = new Vector3(879.2043f, 97.11124f, 888.9434f);    // [W,S] = [(36.1288, 40.68514)]
            MapGpsCoordinates[MapLocation.Caves_7] = new Vector3(584.8429f, 128.7122f, 1241.692f);  // [W,S] = [(43.30385, 31.06347)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner2] = new Vector3(750.488f, 123.4451f, 1591.867f);  // [W,S] = [(39.26626, 21.51198)]
            MapGpsCoordinates[MapLocation.food_ration_05_mound] = new Vector3(433.4283f, 126.0453f, 1177.692f); // [W,S] = [(46.99458, 32.80914)]
            MapGpsCoordinates[MapLocation.A01S07_RockToCartel] = new Vector3(461.1687f, 110.8092f, 1481.069f);  // [W,S] = [(46.31841, 24.53415)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner7] = new Vector3(785.7932f, 134.0396f, 1560.431f); // [W,S] = [(38.4057, 22.36944)]
            MapGpsCoordinates[MapLocation.PVE2_Wounded_04_cave] = new Vector3(381.1151f, 85.40839f, 1790.079f); // [W,S] = [(48.26971, 16.10548)]
            MapGpsCoordinates[MapLocation.Patrol_09_02_C9_7] = new Vector3(760.786f, 118.3547f, 1129.719f); // [W,S] = [(39.01524, 34.11767)]
            MapGpsCoordinates[MapLocation.PT_A02S01_Airport] = new Vector3(1163.051f, 179.8508f, 1534.859f);    // [W,S] = [(29.21006, 23.06694)]
            MapGpsCoordinates[MapLocation.HangedBody_04] = new Vector3(608.5613f, 119.6014f, 1138.393f);    // [W,S] = [(42.72572, 33.88109)]
            MapGpsCoordinates[MapLocation.Caves_1] = new Vector3(703.708f, 95.87952f, 933.8008f);   // [W,S] = [(40.40652, 39.46159)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner4] = new Vector3(549.2175f, 126.8764f, 1750.412f); // [W,S] = [(44.17222, 17.18744)]
            MapGpsCoordinates[MapLocation.A04S01_d] = new Vector3(692.6444f, 93.865f, 906.1113f);   // [W,S] = [(40.67619, 40.21687)]
            MapGpsCoordinates[MapLocation.HangedBody_11] = new Vector3(625.6741f, 134.1326f, 1241.724f);    // [W,S] = [(42.30859, 31.0626)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_08] = new Vector3(580.632f, 143.34f, 1542.519f); // [W,S] = [(43.40649, 22.85801)]
            MapGpsCoordinates[MapLocation.Panther_04] = new Vector3(690.197f, 113.1189f, 1705.656f);    // [W,S] = [(40.73585, 18.40824)]
            MapGpsCoordinates[MapLocation.PT_A01S06_Cartel] = new Vector3(284.9425f, 107.07f, 1356.103f);   // [W,S] = [(50.61391, 27.94275)]
            MapGpsCoordinates[MapLocation.Caves_12] = new Vector3(418.3984f, 118.4585f, 1025.398f); // [W,S] = [(47.36093, 36.96317)]
            MapGpsCoordinates[MapLocation.PVE2_Wounded_03] = new Vector3(707.7161f, 123.889f, 1479.558f);   // [W,S] = [(40.30882, 24.57535)]
            MapGpsCoordinates[MapLocation.A02S01_Cenot] = new Vector3(1201.263f, 161.8474f, 1465.778f); // [W,S] = [(28.27865, 24.95123)]
            MapGpsCoordinates[MapLocation.A01S07_GrabbingHook] = new Vector3(625.3212f, 132.5684f, 1429.54f);   // [W,S] = [(42.31719, 25.93967)]
            MapGpsCoordinates[MapLocation.A04S02_c] = new Vector3(539.9725f, 124.804f, 745.6565f);  // [W,S] = [(44.39756, 44.59348)]
            MapGpsCoordinates[MapLocation.Patrol_06_02_C7_2] = new Vector3(739.7904f, 128.055f, 1251.812f); // [W,S] = [(39.52701, 30.78744)]
            MapGpsCoordinates[MapLocation.PT_A03S02_Tutorial] = new Vector3(1199.705f, 98.51162f, 1131.864f);   // [W,S] = [(28.31662, 34.05917)]
            MapGpsCoordinates[MapLocation.Patrol_10_01_C3_3] = new Vector3(419.8765f, 124.898f, 1153.789f); // [W,S] = [(47.3249, 33.46112)]
            MapGpsCoordinates[MapLocation.Drum_08] = new Vector3(684.3138f, 125.2835f, 1490.746f);  // [W,S] = [(40.87925, 24.27018)]
            MapGpsCoordinates[MapLocation.Bike_06] = new Vector3(431.0923f, 126.6285f, 1179.814f);  // [W,S] = [(47.05151, 32.75126)]
            MapGpsCoordinates[MapLocation.Canoe_03] = new Vector3(311.121f, 90.985f, 1589.89f); // [W,S] = [(49.97581, 21.5659)]
            MapGpsCoordinates[MapLocation.Panther_01] = new Vector3(757.5928f, 121.3835f, 1761.791f);   // [W,S] = [(39.09308, 16.87708)]
            MapGpsCoordinates[MapLocation.Albino_01] = new Vector3(472.9952f, 122.0745f, 1085.081f);    // [W,S] = [(46.03013, 35.33522)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_10] = new Vector3(350.8103f, 97.0634f, 1614.13f);    // [W,S] = [(49.00838, 20.90473)]
            MapGpsCoordinates[MapLocation.A04_S01_b_big_enemy_camp] = new Vector3(861.9202f, 144.5093f, 1214.127f); // [W,S] = [(36.5501, 31.81533)]
            MapGpsCoordinates[MapLocation.Panther_03] = new Vector3(469.9473f, 100.9449f, 1564.911f);   // [W,S] = [(46.10443, 22.24725)]
            MapGpsCoordinates[MapLocation.PVE2_Wounded_03_cave] = new Vector3(696.2001f, 123.889f, 1487.78f);   // [W,S] = [(40.58952, 24.35109)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_07] = new Vector3(205.072f, 105.769f, 1442.941f);    // [W,S] = [(52.56075, 25.57413)]
            MapGpsCoordinates[MapLocation.Statue_07] = new Vector3(885.9794f, 142.4441f, 1239.152f);    // [W,S] = [(35.96366, 31.13274)]
            MapGpsCoordinates[MapLocation.PT_A03S01_Rozlewiska] = new Vector3(1357.723f, 91.72222f, 1260.933f); // [W,S] = [(24.46493, 30.53863)]
            MapGpsCoordinates[MapLocation.Caves_10] = new Vector3(512.0596f, 113.136f, 988.2018f);  // [W,S] = [(45.07794, 37.97774)]
            MapGpsCoordinates[MapLocation.PVE2_BigAICamp_06] = new Vector3(781.6992f, 134.0867f, 1558.147f);    // [W,S] = [(38.50549, 22.43172)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A03_S03_to_A04_S01c] = new Vector3(924.827f, 109.442f, 1072.495f);  // [W,S] = [(35.01675, 35.67853)]
            MapGpsCoordinates[MapLocation.PVE2_BigAICamp_05] = new Vector3(601.2031f, 131.8043f, 1786.49f); // [W,S] = [(42.90507, 16.20337)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_05] = new Vector3(492.3003f, 116.0034f, 1629.679f);  // [W,S] = [(45.55957, 20.4806)]
            MapGpsCoordinates[MapLocation.PT_A02S01_Cenote] = new Vector3(1214.44f, 157.0768f, 1471.841f);  // [W,S] = [(27.95744, 24.78583)]
            MapGpsCoordinates[MapLocation.A03S01_StoneRing] = new Vector3(1397.625f, 93.70249f, 1139.184f); // [W,S] = [(23.49232, 33.8595)]
            MapGpsCoordinates[MapLocation.SmallAICamp_08] = new Vector3(614.4554f, 121.1317f, 1036.058f);   // [W,S] = [(42.58205, 36.67238)]
            MapGpsCoordinates[MapLocation.WomanInCage_03] = new Vector3(906.6865f, 141.776f, 1227.885f);    // [W,S] = [(35.45893, 31.44007)]
            MapGpsCoordinates[MapLocation.A04_S01_f_shaman_passage] = new Vector3(579.5886f, 119.3307f, 883.2385f); // [W,S] = [(43.43192, 40.84075)]
            MapGpsCoordinates[MapLocation.Patrol_02_01_C1_4] = new Vector3(766.6841f, 105.8912f, 1008.141f);    // [W,S] = [(38.87148, 37.43386)]
            MapGpsCoordinates[MapLocation.Panther_07] = new Vector3(590.287f, 145.891f, 1555.174f); // [W,S] = [(43.17115, 22.51283)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A01_S12_to_A02_S01_01] = new Vector3(1051.257f, 167.1855f, 1440.325f);  // [W,S] = [(31.93502, 25.64547)]
            MapGpsCoordinates[MapLocation.PT_A01S07_Jungle] = new Vector3(464.6006f, 100.8703f, 1283.359f); // [W,S] = [(46.23475, 29.92694)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner8] = new Vector3(486.9749f, 107.2307f, 1440.981f); // [W,S] = [(45.68938, 25.6276)]
            MapGpsCoordinates[MapLocation.PoisonedWater07] = new Vector3(298.3656f, 91.24091f, 1605.632f);  // [W,S] = [(50.28672, 21.13651)]
            MapGpsCoordinates[MapLocation.Caves_4] = new Vector3(850.405f, 144.5191f, 1197.708f);   // [W,S] = [(36.83079, 32.26318)]
            MapGpsCoordinates[MapLocation.Patrol_13_02_C5_8] = new Vector3(504.2822f, 115.2109f, 1021.302f);    // [W,S] = [(45.26751, 37.07488)]
            MapGpsCoordinates[MapLocation.Drum_01] = new Vector3(569.0981f, 113.2871f, 1417.02f);   // [W,S] = [(43.68763, 26.28117)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner5] = new Vector3(519.1549f, 124.8329f, 1745.209f); // [W,S] = [(44.90499, 17.32937)]
            MapGpsCoordinates[MapLocation.food_ration_13_mound] = new Vector3(890.9215f, 114.2396f, 1011.991f); // [W,S] = [(35.8432, 37.32887)]
            MapGpsCoordinates[MapLocation.HangedBody_07] = new Vector3(791.2347f, 142.8185f, 1254.485f);    // [W,S] = [(38.27306, 30.7145)]
            MapGpsCoordinates[MapLocation.A03S01_Camp] = new Vector3(1288.869f, 92.616f, 1124.57f); // [W,S] = [(26.14325, 34.25812)]
            MapGpsCoordinates[MapLocation.Patrol_05_01_C3_7] = new Vector3(615.3488f, 117.8456f, 1165.385f);    // [W,S] = [(42.56027, 33.14483)]
            MapGpsCoordinates[MapLocation.food_ration_09_liane] = new Vector3(692.1797f, 118.3558f, 1198.036f); // [W,S] = [(40.68752, 32.25422)]
            MapGpsCoordinates[MapLocation.Albino_03] = new Vector3(637.0143f, 114.126f, 903.7427f); // [W,S] = [(42.03218, 40.28147)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_AlbinoSpawner] = new Vector3(874.5867f, 92.24718f, 867.4756f); // [W,S] = [(36.24136, 41.2707)]
            MapGpsCoordinates[MapLocation.PoisonedWater09] = new Vector3(256.3673f, 95.61769f, 1497.594f);  // [W,S] = [(51.31043, 24.0834)]
            MapGpsCoordinates[MapLocation.Wounded_04] = new Vector3(592.7279f, 127.6568f, 1239.862f);   // [W,S] = [(43.11165, 31.11338)]
            MapGpsCoordinates[MapLocation.Statue_02] = new Vector3(850.3285f, 159.7452f, 1300.885f);    // [W,S] = [(36.83265, 29.44888)]
            MapGpsCoordinates[MapLocation.food_ration_19_liane] = new Vector3(596.986f, 120.228f, 1133.384f);   // [W,S] = [(43.00786, 34.01772)]
            MapGpsCoordinates[MapLocation.A01S10_BambooBridge] = new Vector3(831.159f, 138.608f, 1620.014f);    // [W,S] = [(37.29991, 20.74423)]
            MapGpsCoordinates[MapLocation.ChallangeSP_MightyCamp] = new Vector3(404.336f, 103.705f, 1589.994f); // [W,S] = [(47.7037, 21.56306)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A01_S12_to_A04_S01b_01] = new Vector3(847.796f, 147.2365f, 1340.128f);  // [W,S] = [(36.89438, 28.37849)]
            MapGpsCoordinates[MapLocation.Patrol_04_02_C8_3] = new Vector3(640.2123f, 114.3334f, 1063.411f);    // [W,S] = [(41.95422, 35.9263)]
            MapGpsCoordinates[MapLocation.PoisonedWater01] = new Vector3(630.5267f, 125.5038f, 1470.106f);  // [W,S] = [(42.19031, 24.83316)]
            MapGpsCoordinates[MapLocation.Albino_04] = new Vector3(739.8661f, 105.1263f, 1023.71f); // [W,S] = [(39.52517, 37.0092)]
            MapGpsCoordinates[MapLocation.Albino_06] = new Vector3(811.7853f, 98.88179f, 944.3593f);    // [W,S] = [(37.77214, 39.1736)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BabyTapirSpawner1] = new Vector3(688.9691f, 123.2495f, 1512.93f);  // [W,S] = [(40.76578, 23.66508)]
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_16] = new Vector3(289.8511f, 114.7836f, 1276.267f);  // [W,S] = [(50.49426, 30.12039)]
            MapGpsCoordinates[MapLocation.PVE_Boat] = new Vector3(710.8521f, 92.144f, 882.7271f);   // [W,S] = [(40.23238, 40.8547)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_06] = new Vector3(391.0233f, 111.39f, 1341.038f);    // [W,S] = [(48.02819, 28.35366)]
            MapGpsCoordinates[MapLocation.A04S01_e_Barrel3] = new Vector3(533.7843f, 114.296f, 1036.738f);  // [W,S] = [(44.5484, 36.65386)]
            MapGpsCoordinates[MapLocation.PoisonedWater03] = new Vector3(511.2852f, 106.0121f, 1387.515f);  // [W,S] = [(45.09682, 27.08595)]
            MapGpsCoordinates[MapLocation.PT_A01S03_Jungle] = new Vector3(561.4174f, 132.9026f, 1648.427f); // [W,S] = [(43.87485, 19.96922)]
            MapGpsCoordinates[MapLocation.PoisonedWater04] = new Vector3(481.489f, 101.3328f, 1322.913f);   // [W,S] = [(45.8231, 28.84804)]
            MapGpsCoordinates[MapLocation.A01S07_StoneRings] = new Vector3(653.4912f, 138.7564f, 1416.553f);    // [W,S] = [(41.63055, 26.2939)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_07] = new Vector3(527.3976f, 98.689f, 1189.155f);    // [W,S] = [(44.70407, 32.49648)]
            MapGpsCoordinates[MapLocation.A04S02_b] = new Vector3(413.6495f, 122.87f, 869.0811f);   // [W,S] = [(47.47668, 41.22691)]
            MapGpsCoordinates[MapLocation.Patrol_10_03_C3_3] = new Vector3(558.1787f, 117.5632f, 1133.064f);    // [W,S] = [(43.95379, 34.02643)]
            MapGpsCoordinates[MapLocation.SmallAICamp_03] = new Vector3(592.4236f, 120.0635f, 1116.235f);   // [W,S] = [(43.11907, 34.48547)]
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_13] = new Vector3(583.5927f, 138.519f, 1678.677f);   // [W,S] = [(43.33432, 19.14412)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_06] = new Vector3(511.412f, 128.096f, 1788.46f); // [W,S] = [(45.09373, 16.14965)]
            MapGpsCoordinates[MapLocation.QA_Test] = new Vector3(1048.858f, 178.05f, 1815.702f);    // [W,S] = [(31.9935, 15.40659)]
            MapGpsCoordinates[MapLocation.PT_A01S03_Jeep] = new Vector3(530.194f, 127.8356f, 1753.261f);    // [W,S] = [(44.63591, 17.10975)]
            MapGpsCoordinates[MapLocation.PoisonedWater08] = new Vector3(497.1485f, 98.68053f, 1199.397f);  // [W,S] = [(45.44139, 32.21712)]
            MapGpsCoordinates[MapLocation.food_ration_23_mound] = new Vector3(323.3276f, 134.9875f, 1150.45f);  // [W,S] = [(49.67827, 33.55221)]
            MapGpsCoordinates[MapLocation.Patrol_09_01_C9_7] = new Vector3(680.0205f, 118.6463f, 1157.818f);    // [W,S] = [(40.9839, 33.35124)]
            MapGpsCoordinates[MapLocation.SmallAICamp_02] = new Vector3(764.2464f, 154.4589f, 1277.015f);   // [W,S] = [(38.9309, 30.09999)]
            MapGpsCoordinates[MapLocation.A04S01_c] = new Vector3(832.6035f, 125.5567f, 1037.173f); // [W,S] = [(37.2647, 36.64197)]
            MapGpsCoordinates[MapLocation.A04S01_c_Destroy] = new Vector3(774.5425f, 105.3434f, 999.1068f); // [W,S] = [(38.67993, 37.68029)]
            MapGpsCoordinates[MapLocation.Statue_04] = new Vector3(773.095f, 150.9389f, 1291.473f); // [W,S] = [(38.71521, 29.70562)]
            MapGpsCoordinates[MapLocation.BadWater_07] = new Vector3(334.6281f, 130.498f, 1217.956f);   // [W,S] = [(49.40283, 31.7109)]
            MapGpsCoordinates[MapLocation.A01S09_Elevator] = new Vector3(687.057f, 113.0616f, 1702.976f);   // [W,S] = [(40.81239, 18.48134)]
            MapGpsCoordinates[MapLocation.A03S01_CaveAyuhaska] = new Vector3(1323.433f, 92.68927f, 1218.526f);  // [W,S] = [(25.30075, 31.69533)]
            MapGpsCoordinates[MapLocation.A04_S01_c_waterfalls_island] = new Vector3(760.7376f, 105.8637f, 1008.879f);  // [W,S] = [(39.01643, 37.41375)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_PantherSpawner] = new Vector3(418.2625f, 99.71472f, 1851.349f);    // [W,S] = [(47.36424, 14.43427)]
            MapGpsCoordinates[MapLocation.A04_S01_f_passage_to_A01_S04] = new Vector3(574.887f, 116.7671f, 1213.017f);  // [W,S] = [(43.54652, 31.84562)]
            MapGpsCoordinates[MapLocation.SmallAICamp_09] = new Vector3(813.6691f, 122.3933f, 1167.047f);   // [W,S] = [(37.72622, 33.09949)]
            MapGpsCoordinates[MapLocation.Kid_02] = new Vector3(535.14f, 125.63f, 1069.71f);    // [W,S] = [(44.51536, 35.7545)]
            MapGpsCoordinates[MapLocation.food_ration_10_mound] = new Vector3(793.3749f, 155.0388f, 1307.313f); // [W,S] = [(38.22089, 29.27356)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A01_S07] = new Vector3(645.618f, 139.8401f, 1426.096f); // [W,S] = [(41.82246, 26.03359)]
            MapGpsCoordinates[MapLocation.Patrol_14_02_C2_10] = new Vector3(861.7703f, 144.6385f, 1225.617f);   // [W,S] = [(36.55376, 31.50193)]
            MapGpsCoordinates[MapLocation.SmallAICamp_06] = new Vector3(361.7151f, 135.079f, 1207.261f);    // [W,S] = [(48.74258, 32.00262)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_11] = new Vector3(681.452f, 116.383f, 1685.972f);    // [W,S] = [(40.94901, 18.94514)]
            MapGpsCoordinates[MapLocation.A04_S01_c_passage_to_A03_S03] = new Vector3(868.2277f, 118.4762f, 1003.092f); // [W,S] = [(36.39636, 37.57158)]
            MapGpsCoordinates[MapLocation.Caves_6] = new Vector3(768.959f, 150.413f, 1309.557f);    // [W,S] = [(38.81603, 29.21237)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_13] = new Vector3(566.628f, 136.635f, 1735.834f);    // [W,S] = [(43.74784, 17.58509)]
            MapGpsCoordinates[MapLocation.Panther_06] = new Vector3(399.0686f, 96.45319f, 1803.948f);   // [W,S] = [(47.83209, 15.72718)]
            MapGpsCoordinates[MapLocation.food_ration_03_mound] = new Vector3(450.9729f, 111.6943f, 1054.663f); // [W,S] = [(46.56693, 36.16492)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_01] = new Vector3(169.542f, 98.355f, 1631.49f);  // [W,S] = [(53.42679, 20.43122)]
            MapGpsCoordinates[MapLocation.A01S11_SQPlace] = new Vector3(1016.496f, 145.27f, 1645.914f); // [W,S] = [(32.78233, 20.03778)]
            MapGpsCoordinates[MapLocation.A04_S01_d_pve_boat] = new Vector3(715.103f, 91.955f, 883.908f);   // [W,S] = [(40.12877, 40.82249)]
            MapGpsCoordinates[MapLocation.Statue_01] = new Vector3(539.4466f, 115.0281f, 1126.414f);    // [W,S] = [(44.41039, 34.20782)]
            MapGpsCoordinates[MapLocation.PVE2_Kid_04] = new Vector3(518.5273f, 105.8104f, 1385.195f);  // [W,S] = [(44.92029, 27.14922)]
            MapGpsCoordinates[MapLocation.Caves_15] = new Vector3(424.0854f, 125.8335f, 1171.411f); // [W,S] = [(47.22231, 32.98046)]
            MapGpsCoordinates[MapLocation.SmallAICamp_01_outer] = new Vector3(792.3371f, 101.0916f, 959.8417f); // [W,S] = [(38.24619, 38.7513)]
            MapGpsCoordinates[MapLocation.PT_A01S11_JungleBamboo] = new Vector3(961.0281f, 134.4039f, 1629.057f);   // [W,S] = [(34.13435, 20.49758)]
            MapGpsCoordinates[MapLocation.food_ration_25_mound] = new Vector3(718.207f, 126.047f, 1256.917f);   // [W,S] = [(40.05311, 30.64818)]
            MapGpsCoordinates[MapLocation.HangedBody_03] = new Vector3(564.0624f, 121.6178f, 997.6534f);    // [W,S] = [(43.81038, 37.71993)]
            MapGpsCoordinates[MapLocation.HangedBody_13] = new Vector3(709.1241f, 118.3389f, 1163.439f);    // [W,S] = [(40.2745, 33.19791)]
            MapGpsCoordinates[MapLocation.A04_S01_b_passage_to_A01_S07] = new Vector3(673.0984f, 151.4464f, 1326.6f);   // [W,S] = [(41.15263, 28.74749)]
            MapGpsCoordinates[MapLocation.Hub_Village_Outside] = new Vector3(687.7271f, 117.946f, 1101.839f);   // [W,S] = [(40.79605, 34.87813)]
            MapGpsCoordinates[MapLocation.Debug] = new Vector3(2044.52f, 94.9765f, 3.54f);  // [W,S] = [(7.724285, 64.83568)]
            MapGpsCoordinates[MapLocation.Statue_05] = new Vector3(869.5416f, 123.1762f, 1118.95f); // [W,S] = [(36.36433, 34.41142)]
            MapGpsCoordinates[MapLocation.Panther_05] = new Vector3(588.8813f, 142.2167f, 1717.966f);   // [W,S] = [(43.20541, 18.07245)]
            MapGpsCoordinates[MapLocation.PT_A01S07_Jungle2] = new Vector3(531.0518f, 119.1025f, 1497.803f);    // [W,S] = [(44.61501, 24.0777)]
            MapGpsCoordinates[MapLocation.A04_S02_e_river_canyons] = new Vector3(502.4138f, 115.7081f, 658.8477f);  // [W,S] = [(45.31306, 46.9613)]
            MapGpsCoordinates[MapLocation.ChallangeSP_Combat_WT] = new Vector3(417.2121f, 104.01f, 1722.729f);  // [W,S] = [(47.38984, 17.94255)]
            MapGpsCoordinates[MapLocation.food_ration_06_liane] = new Vector3(529.017f, 114.4495f, 1111.192f);  // [W,S] = [(44.6646, 34.62302)]
            MapGpsCoordinates[MapLocation.HangedBody_06] = new Vector3(384.9974f, 132.6187f, 1195.144f);    // [W,S] = [(48.17508, 32.33313)]
            MapGpsCoordinates[MapLocation.PVE2_Kid_01] = new Vector3(173.5f, 93.241f, 1542.29f);    // [W,S] = [(53.33031, 22.86427)]
            MapGpsCoordinates[MapLocation.A02S02_Pond] = new Vector3(1333.651f, 138.657f, 1525.374f);   // [W,S] = [(25.05169, 23.32566)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_09] = new Vector3(710.628f, 124.056f, 1475.585f);    // [W,S] = [(40.23784, 24.68372)]
            MapGpsCoordinates[MapLocation.Patrol_08_02_C2_9] = new Vector3(855.1447f, 141.0819f, 1183.817f);    // [W,S] = [(36.71526, 32.64207)]
            MapGpsCoordinates[MapLocation.Canoe_05] = new Vector3(164.0597f, 109.9217f, 1764.809f); // [W,S] = [(53.56042, 16.79475)]
            MapGpsCoordinates[MapLocation.PVE2_Wounded_02_cave] = new Vector3(217.757f, 127.2224f, 1258.352f);  // [W,S] = [(52.25155, 30.60904)]
            MapGpsCoordinates[MapLocation.PVE2_BigAICamp_04] = new Vector3(375.2101f, 100.0055f, 1474.17f); // [W,S] = [(48.41364, 24.72232)]
            MapGpsCoordinates[MapLocation.PT_A01S01_Harbor] = new Vector3(237.4533f, 89.79554f, 1659.221f); // [W,S] = [(51.77146, 19.67481)]
            MapGpsCoordinates[MapLocation.Albino_02] = new Vector3(500.098f, 124.081f, 1001.522f);  // [W,S] = [(45.3695, 37.61441)]
            MapGpsCoordinates[MapLocation.HangedBody_09] = new Vector3(787.1943f, 93.673f, 917.2256f);  // [W,S] = [(38.37154, 39.91371)]
            MapGpsCoordinates[MapLocation.Wounded_02] = new Vector3(454.0277f, 108.3823f, 1046.644f);   // [W,S] = [(46.49247, 36.38366)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_05] = new Vector3(209.42f, 128.232f, 1260.999f); // [W,S] = [(52.45477, 30.53684)]
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_20] = new Vector3(190.0014f, 107.0012f, 1438.801f);  // [W,S] = [(52.9281, 25.68706)]
            MapGpsCoordinates[MapLocation.Caves_8] = new Vector3(528.8697f, 114.4794f, 1110.941f);  // [W,S] = [(44.66819, 34.62988)]
            MapGpsCoordinates[MapLocation.A01S09_GoldMine] = new Vector3(699.233f, 54.684f, 1817.462f); // [W,S] = [(40.5156, 15.35858)]
            MapGpsCoordinates[MapLocation.A01S02] = new Vector3(420.5364f, 104.306f, 1609.757f);    // [W,S] = [(47.30881, 21.02401)]
            MapGpsCoordinates[MapLocation.SmallAICamp_10] = new Vector3(904.7721f, 141.4325f, 1226.235f);   // [W,S] = [(35.50559, 31.48506)]
            MapGpsCoordinates[MapLocation.WomanInCage_01] = new Vector3(858.1819f, 101.1809f, 934.8375f);   // [W,S] = [(36.64122, 39.43332)]
            MapGpsCoordinates[MapLocation.HangedBody_10] = new Vector3(718.6204f, 117.7737f, 1102.757f);    // [W,S] = [(40.04303, 34.85309)]
            MapGpsCoordinates[MapLocation.A04S01_a] = new Vector3(659.0945f, 120.2171f, 1209.892f); // [W,S] = [(41.49397, 31.93085)]
            MapGpsCoordinates[MapLocation.food_ration_07_mound] = new Vector3(560.7239f, 121.1232f, 1193.243f); // [W,S] = [(43.89175, 32.38496)]
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_11] = new Vector3(191.676f, 94.978f, 1642.514f); // [W,S] = [(52.88728, 20.13051)]
            MapGpsCoordinates[MapLocation.PVE2_Wounded_01] = new Vector3(178.1107f, 108.6276f, 1762.573f);  // [W,S] = [(53.21793, 16.85574)]
            MapGpsCoordinates[MapLocation.A01S12_GHtoAirport] = new Vector3(1037.189f, 159.753f, 1441.974f);    // [W,S] = [(32.27794, 25.60051)]
            MapGpsCoordinates[MapLocation.Drum_03] = new Vector3(376.4304f, 106.3924f, 1356.631f);  // [W,S] = [(48.3839, 27.92836)]
            MapGpsCoordinates[MapLocation.A01S01_CaveCamp] = new Vector3(203.627f, 107.5152f, 1750.827f);   // [W,S] = [(52.59597, 17.17614)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A02_S01_to_A02_S02_02] = new Vector3(1260.208f, 175.802f, 1592.168f);   // [W,S] = [(26.84186, 21.50376)]
            MapGpsCoordinates[MapLocation.HangedBody_02] = new Vector3(614.7563f, 126.8996f, 1048.983f);    // [W,S] = [(42.57471, 36.31986)]
            MapGpsCoordinates[MapLocation.BadWater_06] = new Vector3(879.2523f, 101.1071f, 915.4965f);  // [W,S] = [(36.12763, 39.96087)]
            MapGpsCoordinates[MapLocation.MantisSpawner] = new Vector3(583.1886f, 122.8405f, 1002.815f);    // [W,S] = [(43.34417, 37.57914)]
            MapGpsCoordinates[MapLocation.PT_A01S01_Jungle] = new Vector3(347.9294f, 98.055f, 1795.069f);   // [W,S] = [(49.07861, 15.96937)]
            MapGpsCoordinates[MapLocation.A01S03_Jeep] = new Vector3(528.6524f, 128.076f, 1740.249f);   // [W,S] = [(44.67349, 17.46467)]
            MapGpsCoordinates[MapLocation.Village_02_outside] = new Vector3(440.941f, 107.909f, 1374.49f);  // [W,S] = [(46.81145, 27.44122)]
            MapGpsCoordinates[MapLocation.Canoe_04] = new Vector3(148.7191f, 89.653f, 1561.06f);    // [W,S] = [(53.93435, 22.35228)]
            MapGpsCoordinates[MapLocation.PT_A02S02_Cenote] = new Vector3(1283.802f, 127.8863f, 1431.199f); // [W,S] = [(26.26675, 25.89441)]
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_17] = new Vector3(401.6366f, 103.7196f, 1589.763f);  // [W,S] = [(47.7695, 21.56938)]
            MapGpsCoordinates[MapLocation.Caves_11] = new Vector3(489.2458f, 123.7201f, 1086.87f);  // [W,S] = [(45.63403, 35.28644)]
            MapGpsCoordinates[MapLocation.A04_S01_b_big_caves] = new Vector3(672.5791f, 118.5276f, 1205.922f);  // [W,S] = [(41.16528, 32.03914)]
            MapGpsCoordinates[MapLocation.Kid_04] = new Vector3(759.445f, 99.52f, 956.284f);    // [W,S] = [(39.04793, 38.84834)]
            MapGpsCoordinates[MapLocation.Village_02_inside] = new Vector3(465.58f, 106.642f, 1401.01f);    // [W,S] = [(46.21088, 26.71785)]
            MapGpsCoordinates[MapLocation.food_ration_08_mound] = new Vector3(646.879f, 120.041f, 1212.501f);   // [W,S] = [(41.79172, 31.85968)]
            MapGpsCoordinates[MapLocation.Bike_05] = new Vector3(582.6481f, 128.297f, 1179.854f);   // [W,S] = [(43.35735, 32.75018)]
            MapGpsCoordinates[MapLocation.Patrol_04_01_C8_3] = new Vector3(631.4865f, 113.2527f, 1027.193f);    // [W,S] = [(42.16692, 36.91419)]
            MapGpsCoordinates[MapLocation.food_ration_02_mound] = new Vector3(682.576f, 100.128f, 935.058f);    // [W,S] = [(40.92161, 39.4273)]
            MapGpsCoordinates[MapLocation.A04_S01_d_steamboat] = new Vector3(827.8829f, 92.22733f, 853.9062f);  // [W,S] = [(37.37976, 41.64083)]
            MapGpsCoordinates[MapLocation.Caves_17] = new Vector3(785.2197f, 119.9109f, 1109.238f); // [W,S] = [(38.41967, 34.67633)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A02_S01_to_A02_S02_01] = new Vector3(1246.659f, 188.45f, 1568.647f);    // [W,S] = [(27.17212, 22.14534)]
            MapGpsCoordinates[MapLocation.Canoe_06] = new Vector3(222.568f, 88.427f, 1822.171f);    // [W,S] = [(52.13428, 15.23013)]
            MapGpsCoordinates[MapLocation.A04_S02_b_stone_bridge] = new Vector3(469.8594f, 123.8525f, 888.3677f);   // [W,S] = [(46.10657, 40.70084)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_10] = new Vector3(760.207f, 117.448f, 1728.752f);    // [W,S] = [(39.02936, 17.77826)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_CanoeBoatSpawner] = new Vector3(288.0341f, 88.239f, 1698.035f);    // [W,S] = [(50.53855, 18.61611)]
            MapGpsCoordinates[MapLocation.PT_A01S09_GoldMine] = new Vector3(706.1062f, 51.27618f, 1811.088f);   // [W,S] = [(40.34806, 15.53243)]
            MapGpsCoordinates[MapLocation.BigAICamp_03] = new Vector3(343.2068f, 124.0894f, 1084.375f); // [W,S] = [(49.19372, 35.35448)]
            MapGpsCoordinates[MapLocation.Caves_14] = new Vector3(356.6201f, 135.51f, 1225.28f);    // [W,S] = [(48.86678, 31.51112)]
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_12] = new Vector3(417.0481f, 104.0103f, 1726.759f);  // [W,S] = [(47.39384, 17.83263)]
            MapGpsCoordinates[MapLocation.Wounded_01] = new Vector3(795.0052f, 116.959f, 1099.321f);    // [W,S] = [(38.18115, 34.9468)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_14] = new Vector3(449.701f, 110.414f, 1682.915f);    // [W,S] = [(46.59793, 19.02852)]
            MapGpsCoordinates[MapLocation.food_ration_18_mound] = new Vector3(654.9873f, 117.5151f, 1124.657f); // [W,S] = [(41.59408, 34.25574)]
            MapGpsCoordinates[MapLocation.ChallangeSP_Combat_Battery] = new Vector3(811.5066f, 132.839f, 1683.343f);    // [W,S] = [(37.77893, 19.01685)]
            MapGpsCoordinates[MapLocation.Patrol_03_01_C4_8] = new Vector3(604.604f, 113.9525f, 991.9693f); // [W,S] = [(42.82217, 37.87497)]
            MapGpsCoordinates[MapLocation.Caves_5] = new Vector3(867.5211f, 159.7484f, 1283.711f);  // [W,S] = [(36.41358, 29.91734)]
            MapGpsCoordinates[MapLocation.Caves_3] = new Vector3(879.0111f, 127.0168f, 1108.361f);  // [W,S] = [(36.13351, 34.70023)]
            MapGpsCoordinates[MapLocation.Patrol_07_01_C2_2] = new Vector3(834.3834f, 142.1804f, 1243.138f);    // [W,S] = [(37.22131, 31.02401)]
            MapGpsCoordinates[MapLocation.A01S01_EvilTribeCamp] = new Vector3(410.3247f, 102.0926f, 1715.478f); // [W,S] = [(47.55772, 18.14032)]
            MapGpsCoordinates[MapLocation.BigAICamp_02] = new Vector3(823.118f, 144.7068f, 1267.17f);   // [W,S] = [(37.4959, 30.36851)]
            MapGpsCoordinates[MapLocation.PT_A01S07_TribeVillage] = new Vector3(465.1541f, 106.5126f, 1408.053f);   // [W,S] = [(46.22126, 26.52575)]
            MapGpsCoordinates[MapLocation.Caves_9] = new Vector3(574.9811f, 114.0972f, 1013.144f);  // [W,S] = [(43.54423, 37.29739)]
            MapGpsCoordinates[MapLocation.Panther_02] = new Vector3(477.0969f, 121.1326f, 1746.74f);    // [W,S] = [(45.93015, 17.28761)]
            MapGpsCoordinates[MapLocation.A01S06_Cartel] = new Vector3(275.983f, 106.832f, 1359.832f);  // [W,S] = [(50.83229, 27.84104)]
            MapGpsCoordinates[MapLocation.A01S04] = new Vector3(484.0724f, 106.6023f, 1216.35f);    // [W,S] = [(45.76013, 31.75469)]
            MapGpsCoordinates[MapLocation.Kid_03] = new Vector3(662.3152f, 128.3752f, 1250.005f);   // [W,S] = [(41.41547, 30.83672)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_03] = new Vector3(310.05f, 90.995f, 1588.995f);  // [W,S] = [(50.00192, 21.59032)]
            MapGpsCoordinates[MapLocation.BadWater_03] = new Vector3(604.7839f, 130.648f, 1262.504f);   // [W,S] = [(42.81779, 30.49578)]
            MapGpsCoordinates[MapLocation.food_ration_14_mound] = new Vector3(738.068f, 126.931f, 1245.324f);   // [W,S] = [(39.569, 30.96439)]
            MapGpsCoordinates[MapLocation.A01S11] = new Vector3(900.389f, 136.99f, 1568.039f);  // [W,S] = [(35.61243, 22.16192)]
            MapGpsCoordinates[MapLocation.Kid_01] = new Vector3(656.765f, 118.477f, 1065.82f);  // [W,S] = [(41.55075, 35.8606)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_12] = new Vector3(571.504f, 122.567f, 1357.785f);    // [W,S] = [(43.62899, 27.89687)]
            MapGpsCoordinates[MapLocation.A04_S02_d_river_canyons_cave] = new Vector3(576.4888f, 113.6037f, 549.1804f); // [W,S] = [(43.50748, 49.95262)]
            MapGpsCoordinates[MapLocation.A01S12_Island] = new Vector3(898.0696f, 136.465f, 1425.064f); // [W,S] = [(35.66896, 26.06175)]
            MapGpsCoordinates[MapLocation.A04_S02_e_big_swamps] = new Vector3(381.8928f, 111.3795f, 713.2042f); // [W,S] = [(48.25075, 45.47866)]
            MapGpsCoordinates[MapLocation.A04S02_e] = new Vector3(346.4843f, 110.55f, 694.1174f);   // [W,S] = [(49.11383, 45.99928)]
            MapGpsCoordinates[MapLocation.A01S05_Puddle] = new Vector3(265.83f, 96.967f, 1500.19f); // [W,S] = [(51.07978, 24.01259)]
            MapGpsCoordinates[MapLocation.food_ration_15_liane] = new Vector3(842.9304f, 126.7472f, 1037.069f); // [W,S] = [(37.01298, 36.64481)]
            MapGpsCoordinates[MapLocation.A01S11_EvilTribeCamp] = new Vector3(921.454f, 141.487f, 1657.557f);   // [W,S] = [(35.09897, 19.7202)]
            MapGpsCoordinates[MapLocation.A02S01_Airport] = new Vector3(1166.69f, 179.99f, 1536.7f);    // [W,S] = [(29.12136, 23.01673)]
            MapGpsCoordinates[MapLocation.HangedBody_01] = new Vector3(662.1343f, 116.9716f, 965.6454f);    // [W,S] = [(41.41988, 38.59299)]
            MapGpsCoordinates[MapLocation.food_ration_20_liane] = new Vector3(561.5294f, 114.1614f, 1035.071f); // [W,S] = [(43.87212, 36.69933)]
            MapGpsCoordinates[MapLocation.Patrol_08_01_C2_9] = new Vector3(839.8876f, 128.0847f, 1159.375f);    // [W,S] = [(37.08715, 33.30877)]
            MapGpsCoordinates[MapLocation.Spikes_Tree_Review_Spawner] = new Vector3(722.8529f, 116.6592f, 1114.485f);   // [W,S] = [(39.93986, 34.53319)]
            MapGpsCoordinates[MapLocation.Statue_03] = new Vector3(908.8634f, 127.5488f, 1028.659f);    // [W,S] = [(35.40586, 36.87421)]
            MapGpsCoordinates[MapLocation.PT_A01S05_Pond] = new Vector3(278.0788f, 101.3528f, 1510.454f);   // [W,S] = [(50.78121, 23.73261)]
            MapGpsCoordinates[MapLocation.HangedBody_05] = new Vector3(485.199f, 126.4417f, 1102.653f); // [W,S] = [(45.73266, 34.85593)]
            MapGpsCoordinates[MapLocation.PT_A01S12_WHACamp] = new Vector3(976.809f, 155.6489f, 1309.329f); // [W,S] = [(33.74969, 29.21858)]
            MapGpsCoordinates[MapLocation.PVE_Map_Crate] = new Vector3(849.7507f, 100.8946f, 831.588f); // [W,S] = [(36.84674, 42.24959)]
            MapGpsCoordinates[MapLocation.HangedBody_12] = new Vector3(388.6857f, 121.256f, 1055.43f);  // [W,S] = [(48.08517, 36.144)]
            MapGpsCoordinates[MapLocation.A03S01_GrabbHookHigh] = new Vector3(1245.456f, 91.85075f, 1206.792f); // [W,S] = [(27.20143, 32.0154)]
            MapGpsCoordinates[MapLocation.PT_A03S03_Jungle] = new Vector3(963.957f, 102.942f, 1040.11f);    // [W,S] = [(34.06296, 36.56188)]
            MapGpsCoordinates[MapLocation.A01S10_EvilTribeCamp] = new Vector3(809.3134f, 132.7433f, 1679.564f); // [W,S] = [(37.83239, 19.11993)]
            MapGpsCoordinates[MapLocation.food_ration_21_mound] = new Vector3(432.9763f, 121.0922f, 1081.221f); // [W,S] = [(47.00559, 35.44053)]
            MapGpsCoordinates[MapLocation.Patrol_05_02_C3_7] = new Vector3(624.4211f, 119.2852f, 1210.625f);    // [W,S] = [(42.33913, 31.91086)]
            MapGpsCoordinates[MapLocation.PT_A01S02_Jungle] = new Vector3(378.9291f, 95.36174f, 1626.851f); // [W,S] = [(48.32299, 20.55775)]
            MapGpsCoordinates[MapLocation.PT_A01S11_Jungle] = new Vector3(884.2253f, 140.969f, 1530.652f);  // [W,S] = [(36.00642, 23.18168)]
            MapGpsCoordinates[MapLocation.Statue_06] = new Vector3(758.8989f, 121.1708f, 1219.338f);    // [W,S] = [(39.06124, 31.67319)]
            MapGpsCoordinates[MapLocation.Canoe_02] = new Vector3(193.7189f, 125.4563f, 1260.203f); // [W,S] = [(52.83748, 30.55856)]
            MapGpsCoordinates[MapLocation.Drum_07] = new Vector3(782.2725f, 123.9664f, 1597.695f);  // [W,S] = [(38.49151, 21.35302)]
            MapGpsCoordinates[MapLocation.Bike_03] = new Vector3(406.6446f, 130.72f, 998.4277f);    // [W,S] = [(47.64743, 37.69881)]
            MapGpsCoordinates[MapLocation.PVE2_Wounded_02] = new Vector3(191.3521f, 125f, 1259.16f);    // [W,S] = [(52.89517, 30.587)]
            MapGpsCoordinates[MapLocation.A04_S02_b_sacred_ruins] = new Vector3(412.2589f, 122.8633f, 867.1254f);   // [W,S] = [(47.51058, 41.28025)]
            MapGpsCoordinates[MapLocation.A01S01_Harbor] = new Vector3(243.1012f, 89.09552f, 1661.669f);    // [W,S] = [(51.63379, 19.60805)]
            MapGpsCoordinates[MapLocation.PT_A02S01_Jungle] = new Vector3(1075.127f, 168.4598f, 1443.548f); // [W,S] = [(31.35319, 25.55758)]
            MapGpsCoordinates[MapLocation.ChallangeSP_Raft] = new Vector3(299.1657f, 88.25962f, 1735.181f); // [W,S] = [(50.26722, 17.6029)]
            MapGpsCoordinates[MapLocation.A03S03_TribeVillage] = new Vector3(1066.53f, 93.01f, 1060.56f);   // [W,S] = [(31.56275, 36.00407)]
            MapGpsCoordinates[MapLocation.PVE_StartDebugSpawner] = new Vector3(698.695f, 116.135f, 936.107f);   // [W,S] = [(40.52871, 39.39869)]
            MapGpsCoordinates[MapLocation.Caves_13] = new Vector3(306.5822f, 123.5904f, 1051.654f); // [W,S] = [(50.08644, 36.247)]
            MapGpsCoordinates[MapLocation.A04_S01_b_passage_to_A01_S12] = new Vector3(830.315f, 161.8449f, 1315.05f);   // [W,S] = [(37.32048, 29.06252)]
            MapGpsCoordinates[MapLocation.Patrol_12_02_C9_5] = new Vector3(357.7899f, 121.5589f, 1019.546f);    // [W,S] = [(48.83826, 37.12277)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BadWaterAltarSpawner] = new Vector3(874.16f, 116.0853f, 1142.339f);    // [W,S] = [(36.25176, 33.77345)]
            MapGpsCoordinates[MapLocation.A04_S02_c_dead_bodies_water_cave] = new Vector3(531.2287f, 124.7171f, 750.1702f); // [W,S] = [(44.61069, 44.47036)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_Bike] = new Vector3(547.567f, 126.5762f, 1119.016f);   // [W,S] = [(44.21245, 34.4096)]
            MapGpsCoordinates[MapLocation.Drum_06] = new Vector3(765.4966f, 134.468f, 1536.677f);   // [W,S] = [(38.90042, 23.01736)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_02] = new Vector3(407.422f, 98.8f, 1784.038f);   // [W,S] = [(47.62848, 16.27026)]
            MapGpsCoordinates[MapLocation.A04S01_f] = new Vector3(676.9086f, 118.4762f, 1185.95f);  // [W,S] = [(41.05975, 32.5839)]
            MapGpsCoordinates[MapLocation.Wounded_05] = new Vector3(871.437f, 159.1508f, 1264.011f);    // [W,S] = [(36.31813, 30.45468)]
            MapGpsCoordinates[MapLocation.Drum_02] = new Vector3(646.8541f, 125.5757f, 1453.645f);  // [W,S] = [(41.79233, 25.28217)]
            MapGpsCoordinates[MapLocation.Caves_18] = new Vector3(797.3568f, 94.09662f, 935.3817f); // [W,S] = [(38.12384, 39.41848)]
            MapGpsCoordinates[MapLocation.Bike_04] = new Vector3(386.7533f, 131.5995f, 1131.177f);  // [W,S] = [(48.13227, 34.0779)]
            MapGpsCoordinates[MapLocation.PT_A01S12_RefugeeIsland] = new Vector3(899.3753f, 136.208f, 1424.16f);    // [W,S] = [(35.63714, 26.08642)]
            MapGpsCoordinates[MapLocation.Caves_2] = new Vector3(861.5999f, 118.4731f, 999.8885f);  // [W,S] = [(36.55791, 37.65897)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_AlbinoSpawner_Outside] = new Vector3(842.0014f, 91.95774f, 896.8022f); // [W,S] = [(37.03562, 40.47078)]
            MapGpsCoordinates[MapLocation.Kid_05] = new Vector3(363.0977f, 122.402f, 1052f);    // [W,S] = [(48.70888, 36.23757)]
            MapGpsCoordinates[MapLocation.ChallangeSP_FireCamp] = new Vector3(677.4979f, 129.5853f, 1566.251f); // [W,S] = [(41.04539, 22.2107)]
            MapGpsCoordinates[MapLocation.food_ration_16_mound] = new Vector3(823.552f, 104.811f, 960.18f); // [W,S] = [(37.48532, 38.74207)]
            MapGpsCoordinates[MapLocation.BadWater_04] = new Vector3(675.0228f, 115.4239f, 964.4216f);  // [W,S] = [(41.10572, 38.62637)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_BadWaterBoatSpawner] = new Vector3(856.955f, 93.867f, 837.702f);   // [W,S] = [(36.67113, 42.08282)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_03] = new Vector3(774.035f, 129.143f, 1651.353f);    // [W,S] = [(38.6923, 19.88942)]
            MapGpsCoordinates[MapLocation.Patrol_10_02_C3_3] = new Vector3(481.6177f, 118.0104f, 1129.094f);    // [W,S] = [(45.81996, 34.13471)]
            MapGpsCoordinates[MapLocation.A04S01_e] = new Vector3(447.2341f, 111.1141f, 1045.866f); // [W,S] = [(46.65806, 36.40486)]
            MapGpsCoordinates[MapLocation.Patrol_02_02_C1_4] = new Vector3(678.3525f, 118.2707f, 990.8765f);    // [W,S] = [(41.02456, 37.90478)]
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_15] = new Vector3(245.7734f, 108.9239f, 1448.935f);  // [W,S] = [(51.56866, 25.41063)]
            MapGpsCoordinates[MapLocation.PVE2_Wounded_05] = new Vector3(414.9395f, 108.978f, 1254.39f);    // [W,S] = [(47.44524, 30.71711)]
            MapGpsCoordinates[MapLocation.A03S01_OutofCenot] = new Vector3(1290.175f, 92.15056f, 1380.169f);    // [W,S] = [(26.11142, 27.28631)]
            MapGpsCoordinates[MapLocation.Patrol_06_01_C7_2] = new Vector3(697.1671f, 118.4968f, 1190.747f);    // [W,S] = [(40.56595, 32.45306)]
            MapGpsCoordinates[MapLocation.BadWater_02] = new Vector3(654.35f, 121.1463f, 1214.704f);    // [W,S] = [(41.60962, 31.79959)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_04] = new Vector3(266.128f, 97.458f, 1502.111f); // [W,S] = [(51.07251, 23.96019)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_04] = new Vector3(633.805f, 131.86f, 1583.964f); // [W,S] = [(42.1104, 21.72754)]
            MapGpsCoordinates[MapLocation.Patrol_13_01_C5_8] = new Vector3(553.9409f, 113.9781f, 1037.732f);    // [W,S] = [(44.05709, 36.62673)]
            MapGpsCoordinates[MapLocation.Drum_04] = new Vector3(528.0157f, 98.74426f, 1195.127f);  // [W,S] = [(44.68901, 32.33359)]
            MapGpsCoordinates[MapLocation.A04_S02_d_lake_canyons] = new Vector3(638.569f, 100.836f, 640.5784f); // [W,S] = [(41.99428, 47.45962)]
            MapGpsCoordinates[MapLocation.Caves_16] = new Vector3(622.8455f, 115.1992f, 914.7893f); // [W,S] = [(42.37754, 39.98016)]
            MapGpsCoordinates[MapLocation.Patrol_01_01_C1_1] = new Vector3(829.2627f, 113.6137f, 986.3329f);    // [W,S] = [(37.34613, 38.02871)]
            MapGpsCoordinates[MapLocation.food_ration_12_liane] = new Vector3(818.2549f, 121.5284f, 1125.887f); // [W,S] = [(37.61444, 34.22218)]
            MapGpsCoordinates[MapLocation.A01S08_CrashedPlane] = new Vector3(691.866f, 123.331f, 1492.652f);    // [W,S] = [(40.69517, 24.2182)]
            MapGpsCoordinates[MapLocation.PVE2_Kid_03] = new Vector3(188.6288f, 120.5623f, 1300.699f);  // [W,S] = [(52.96155, 29.45396)]
            MapGpsCoordinates[MapLocation.Albino_05] = new Vector3(864.0753f, 101.627f, 943.7858f); // [W,S] = [(36.49757, 39.18924)]
            MapGpsCoordinates[MapLocation.PVE2_food_ration_hanging_01] = new Vector3(290.0611f, 114.7704f, 1272.283f);  // [W,S] = [(50.48914, 30.22906)]
            MapGpsCoordinates[MapLocation.PVE2_Wounded_01_cave] = new Vector3(187.747f, 108.275f, 1775.392f);   // [W,S] = [(52.98305, 16.50609)]
            MapGpsCoordinates[MapLocation.PVE2_SmallAICamp_19] = new Vector3(591.97f, 142.1901f, 1581.551f);    // [W,S] = [(43.13013, 21.79337)]
            MapGpsCoordinates[MapLocation.Wounded_03] = new Vector3(701.3249f, 97.00243f, 943.8785f);   // [W,S] = [(40.46461, 39.18671)]
            MapGpsCoordinates[MapLocation.A04S01_f_Barrel2] = new Vector3(615.5436f, 118.2208f, 1153.271f); // [W,S] = [(42.55552, 33.47525)]
            MapGpsCoordinates[MapLocation.A03S02_TutorialCamp] = new Vector3(1198.763f, 98.715f, 1122.541f);    // [W,S] = [(28.33958, 34.31347)]
            MapGpsCoordinates[MapLocation.PoisonedWater02] = new Vector3(571.6883f, 112.1667f, 1456.68f);   // [W,S] = [(43.62449, 25.19937)]
            MapGpsCoordinates[MapLocation.A04S02_d] = new Vector3(591.8564f, 114.23f, 732.6316f);   // [W,S] = [(43.1329, 44.94875)]
            MapGpsCoordinates[MapLocation.SmallAICamp_04] = new Vector3(649.5515f, 116.9505f, 969.9714f);   // [W,S] = [(41.72658, 38.47499)]
            MapGpsCoordinates[MapLocation.Patrol_14_01_C2_10] = new Vector3(905.1836f, 141.5217f, 1226.398f);   // [W,S] = [(35.49556, 31.48062)]
            MapGpsCoordinates[MapLocation.SmallAICamp_07] = new Vector3(673.9833f, 118.4775f, 1207.747f);   // [W,S] = [(41.13106, 31.98936)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_14] = new Vector3(934.366f, 104.6269f, 982.124f);    // [W,S] = [(34.78424, 38.14352)]
            MapGpsCoordinates[MapLocation.Arena_Tribe] = new Vector3(415.3701f, 123.048f, 868.556f);    // [W,S] = [(47.43475, 41.24123)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_05_C21_24] = new Vector3(1158.664f, 179.8511f, 1548.957f);    // [W,S] = [(29.31699, 22.6824)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_40] = new Vector3(670.4163f, 111.4363f, 677.4142f);  // [W,S] = [(41.218, 46.45488)]
            MapGpsCoordinates[MapLocation.GuardianMonkeys_02] = new Vector3(1263.425f, 167.816f, 1617.311f);    // [W,S] = [(26.76344, 20.81796)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_11] = new Vector3(1229.446f, 100.3685f, 1066.283f);  // [W,S] = [(27.59167, 35.84797)]
            MapGpsCoordinates[MapLocation.Przejscie_1_WHACamp] = new Vector3(1248.326f, 100.4395f, 1092.074f);  // [W,S] = [(27.13147, 35.14447)]
            MapGpsCoordinates[MapLocation.Village_Yabahuaca_inside] = new Vector3(1069.795f, 93.30344f, 1062.38f);  // [W,S] = [(31.48316, 35.95442)]
            MapGpsCoordinates[MapLocation.GoldenFish_04] = new Vector3(1284.677f, 92.6049f, 1109.902f); // [W,S] = [(26.24542, 34.6582)]
            MapGpsCoordinates[MapLocation.Village_03_fishing_inside] = new Vector3(1309.803f, 150.5214f, 1600.705f);    // [W,S] = [(25.63297, 21.27092)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_20_01_C11_41] = new Vector3(622.5376f, 105.1097f, 617.7622f); // [W,S] = [(42.38504, 48.08196)]
            MapGpsCoordinates[MapLocation.Chapel_10] = new Vector3(1383.465f, 92.02198f, 1211.667f);    // [W,S] = [(23.83747, 31.88244)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A01_S07_to_A04_S01b_02] = new Vector3(677.0226f, 152.353f, 1335.88f);   // [W,S] = [(41.05698, 28.49436)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A01_S10_Kid] = new Vector3(849.541f, 135.54f, 1621.15f);    // [W,S] = [(36.85184, 20.71324)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_13] = new Vector3(1007.115f, 107.7833f, 1117.085f);    // [W,S] = [(33.011, 34.46227)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_18_01_C37_38] = new Vector3(499.4667f, 118.2945f, 785.9908f); // [W,S] = [(45.38489, 43.49331)]
            MapGpsCoordinates[MapLocation.Chapel_09] = new Vector3(1348.943f, 91.58186f, 1091.654f);    // [W,S] = [(24.67895, 35.15595)]
            MapGpsCoordinates[MapLocation.AztecWarrior_04] = new Vector3(948.8544f, 141.744f, 1529.003f);   // [W,S] = [(34.43109, 23.22668)]
            MapGpsCoordinates[MapLocation.Village_03_fishing_QuestGiver] = new Vector3(1295.844f, 158.5364f, 1591.699f);    // [W,S] = [(25.97322, 21.51657)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_07] = new Vector3(1349.284f, 148.2337f, 1549.271f);  // [W,S] = [(24.67063, 22.67384)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_12] = new Vector3(984.2791f, 109.7496f, 1118.861f);  // [W,S] = [(33.56761, 34.41384)]
            MapGpsCoordinates[MapLocation.Lovers_01] = new Vector3(1049.316f, 167.1855f, 1432.13f); // [W,S] = [(31.98234, 25.86901)]
            MapGpsCoordinates[MapLocation.A01_S12_MushroomCave] = new Vector3(1002.067f, 144.831f, 1274.267f);  // [W,S] = [(33.13404, 30.17493)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_13] = new Vector3(1078.384f, 98.1986f, 945.6119f);   // [W,S] = [(31.27382, 39.13943)]
            MapGpsCoordinates[MapLocation.Przejscie_5_zSOA] = new Vector3(905.2977f, 114.4046f, 1047.29f);  // [W,S] = [(35.49278, 36.36604)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_33] = new Vector3(346.5601f, 112.11f, 955.3801f);    // [W,S] = [(49.11198, 38.87299)]
            MapGpsCoordinates[MapLocation.AztecWarrior_05] = new Vector3(926.47f, 145.4557f, 1290.11f); // [W,S] = [(34.9767, 29.74279)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_Lovers_Charm01Spawner] = new Vector3(1297.225f, 160.0687f, 1608.323f); // [W,S] = [(25.93957, 21.06312)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_32] = new Vector3(394.7f, 111.69f, 942.7701f);   // [W,S] = [(47.93857, 39.21695)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_04_C8_26] = new Vector3(1316.064f, 91.58851f, 1193.348f); // [W,S] = [(25.48038, 32.38211)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_07_C23_24] = new Vector3(1095.392f, 178.285f, 1550.853f); // [W,S] = [(30.85923, 22.6307)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_30] = new Vector3(850.519f, 143.0166f, 1564.533f);   // [W,S] = [(36.82801, 22.25755)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_12_01_C32_33] = new Vector3(384.7615f, 111.7954f, 943.5723f); // [W,S] = [(48.18083, 39.19507)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A02_S01_to_A02_S02_03] = new Vector3(1234.777f, 173.606f, 1501.594f);   // [W,S] = [(27.46174, 23.97429)]
            MapGpsCoordinates[MapLocation.GoldenFish_06] = new Vector3(1396.81f, 93.60313f, 1143.039f); // [W,S] = [(23.5122, 33.75436)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_15_02_C9_35] = new Vector3(352.1228f, 112.4872f, 644.7947f);  // [W,S] = [(48.97639, 47.34462)]
            MapGpsCoordinates[MapLocation.Chapel_05] = new Vector3(843.111f, 136.554f, 1398.28f);   // [W,S] = [(37.00858, 26.79232)]
            MapGpsCoordinates[MapLocation.GoldenFish_07] = new Vector3(1283.976f, 99.91479f, 1427.519f);    // [W,S] = [(26.26252, 25.99479)]
            MapGpsCoordinates[MapLocation.Fisherman_01_Spear] = new Vector3(864.307f, 135.122f, 1546.346f); // [W,S] = [(36.49192, 22.75362)]
            MapGpsCoordinates[MapLocation.PVE3_BigAICamp_07] = new Vector3(963.29f, 136.4004f, 1414.42f);   // [W,S] = [(34.07922, 26.35208)]
            MapGpsCoordinates[MapLocation.Village_Yabahuaca_outside] = new Vector3(1041.636f, 92.77078f, 1047.338f);    // [W,S] = [(32.16953, 36.36473)]
            MapGpsCoordinates[MapLocation.AztecWarrior_02] = new Vector3(902.463f, 141.068f, 1614.413f);    // [W,S] = [(35.56187, 20.89701)]
            MapGpsCoordinates[MapLocation.AztecWarrior_01] = new Vector3(1019.531f, 145.7972f, 1644.972f);  // [W,S] = [(32.70835, 20.06348)]
            MapGpsCoordinates[MapLocation.BigAICamp_01] = new Vector3(810.6752f, 94.84235f, 932.3934f); // [W,S] = [(37.7992, 39.49998)]
            MapGpsCoordinates[MapLocation.GoldenFish_03] = new Vector3(1322.997f, 93.73385f, 1233.632f);    // [W,S] = [(25.31137, 31.2833)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_11] = new Vector3(1172.12f, 113.9979f, 1032.934f); // [W,S] = [(28.98901, 36.75762)]
            MapGpsCoordinates[MapLocation.GoldenFish_02] = new Vector3(1367.613f, 91.987f, 1351.38f);   // [W,S] = [(24.22386, 28.07157)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_12] = new Vector3(1036.826f, 104.7122f, 996.2386f);    // [W,S] = [(32.28679, 37.75852)]
            MapGpsCoordinates[MapLocation.Chapel_04] = new Vector3(931.0724f, 143.6237f, 1335.057f);    // [W,S] = [(34.86452, 28.51679)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_06] = new Vector3(1255.653f, 144.0144f, 1432.121f);  // [W,S] = [(26.95288, 25.86927)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_09] = new Vector3(1410.78f, 91.93797f, 1290.161f);   // [W,S] = [(23.17166, 29.74142)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A01_S12_Mushroom] = new Vector3(977.5949f, 146.1709f, 1293.833f);   // [W,S] = [(33.73054, 29.64125)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_AztecWarrior_OutsideCaveSpawner] = new Vector3(1002.75f, 135.2f, 1510.55f);    // [W,S] = [(33.11738, 23.73001)]
            MapGpsCoordinates[MapLocation.Przejscie_2_TutorialKloda] = new Vector3(1230.074f, 101.5902f, 1053.13f); // [W,S] = [(27.57637, 36.20673)]
            MapGpsCoordinates[MapLocation.Arena_Tribe_Gatekeeper] = new Vector3(471.9127f, 122.2351f, 901.2281f);   // [W,S] = [(46.05652, 40.35006)]
            MapGpsCoordinates[MapLocation.AztecWarrior_08] = new Vector3(897.6611f, 136.0009f, 1432.299f);  // [W,S] = [(35.67892, 25.8644)]
            MapGpsCoordinates[MapLocation.Fisherman_04_Bow] = new Vector3(920.199f, 145.874f, 1322.511f);   // [W,S] = [(35.12956, 28.85902)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_01_C7_28] = new Vector3(947.332f, 138.1188f, 1509.694f);  // [W,S] = [(34.46819, 23.75335)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_03_C7_29] = new Vector3(887.8959f, 136.4695f, 1356.002f); // [W,S] = [(35.91695, 27.94549)]
            MapGpsCoordinates[MapLocation.Przejscie_4_zSOA] = new Vector3(827.1899f, 160.1619f, 1319.316f); // [W,S] = [(37.39665, 28.94617)]
            MapGpsCoordinates[MapLocation.GuardianMonkeys_05] = new Vector3(1308.723f, 134.0317f, 1438.716f);   // [W,S] = [(25.6593, 25.68937)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_08_02_C27_28] = new Vector3(999.9949f, 135.9493f, 1546.116f); // [W,S] = [(33.18454, 22.75989)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A01_S10] = new Vector3(829.98f, 139.44f, 1613.2f);  // [W,S] = [(37.32864, 20.93009)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_16_01_C10_36] = new Vector3(443.8204f, 122.4135f, 797.5895f); // [W,S] = [(46.74127, 43.17693)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_Lovers_Charm04Spawner] = new Vector3(889.0361f, 134.8574f, 1387.221f); // [W,S] = [(35.88915, 27.09398)]
            MapGpsCoordinates[MapLocation.PVE3_BigAICamp_11] = new Vector3(592.4301f, 112.826f, 616.16f);   // [W,S] = [(43.11892, 48.12566)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_AztecWarrior_ObsidianScratch_1Spawner] = new Vector3(995.1963f, 135.6329f, 1548.074f); // [W,S] = [(33.30151, 22.70648)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_16_02_C10_36] = new Vector3(514.8763f, 123.6808f, 738.6076f); // [W,S] = [(45.00928, 44.78574)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_13_02_C33_34] = new Vector3(350.0623f, 124.2275f, 823.6575f); // [W,S] = [(49.02662, 42.4659)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_05] = new Vector3(1209.068f, 157.6361f, 1469.744f);    // [W,S] = [(28.08839, 24.84304)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_22] = new Vector3(1190.901f, 176.4332f, 1495.709f);  // [W,S] = [(28.5312, 24.1348)]
            MapGpsCoordinates[MapLocation.PVE3_BigAICamp_10] = new Vector3(482.9301f, 128.81f, 736.86f);    // [W,S] = [(45.78797, 44.83341)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_Lovers_ShackSpawner] = new Vector3(1276.832f, 169.8931f, 1626.23f);    // [W,S] = [(26.43665, 20.57468)]
            MapGpsCoordinates[MapLocation.Przejscie_3_Predream2] = new Vector3(742.92f, 149.0473f, 1381.16f);   // [W,S] = [(39.45073, 27.25929)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_14_01_C9_34] = new Vector3(321.5443f, 114.2035f, 762.3322f);  // [W,S] = [(49.72174, 44.13863)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_23] = new Vector3(1136.084f, 184.2308f, 1498.665f);  // [W,S] = [(29.86738, 24.05418)]
            MapGpsCoordinates[MapLocation.GoldenFish_05] = new Vector3(1255.959f, 92.58701f, 1312.54f); // [W,S] = [(26.94543, 29.13099)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_AztecWarrior_SkullWoodSpawner] = new Vector3(1009.067f, 135.7998f, 1534.511f); // [W,S] = [(32.96339, 23.07644)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_08] = new Vector3(1276.294f, 170.0537f, 1627.783f);  // [W,S] = [(26.44976, 20.53231)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_15_01_C9_35] = new Vector3(310.8055f, 112.9994f, 699.6855f);  // [W,S] = [(49.9835, 45.8474)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_37] = new Vector3(516.1513f, 116.522f, 867.9776f);   // [W,S] = [(44.97821, 41.25701)]
            MapGpsCoordinates[MapLocation.Chapel_08] = new Vector3(1380.222f, 91.64988f, 1307.631f);    // [W,S] = [(23.91653, 29.26489)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_Lovers_Charm03Spawner] = new Vector3(767.689f, 148.261f, 1384.934f);   // [W,S] = [(38.84698, 27.15635)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_19_02_C11_40] = new Vector3(659.1226f, 111.7622f, 687.1094f); // [W,S] = [(41.49329, 46.19043)]
            MapGpsCoordinates[MapLocation.Lovers_06] = new Vector3(1170.38f, 184.1854f, 1577.003f); // [W,S] = [(29.03142, 21.91741)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_04] = new Vector3(1130.329f, 183.3013f, 1504.325f);    // [W,S] = [(30.00765, 23.8998)]
            MapGpsCoordinates[MapLocation.AztecWarrior_03] = new Vector3(875.675f, 134.1329f, 1540.854f);   // [W,S] = [(36.21483, 22.90343)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_12_02_C32_33] = new Vector3(330.6824f, 120.1978f, 944.4921f); // [W,S] = [(49.499, 39.16998)]
            MapGpsCoordinates[MapLocation.Fisherman_05_Bow] = new Vector3(1267.857f, 142.8141f, 1449.539f); // [W,S] = [(26.65542, 25.39417)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_31] = new Vector3(848.2981f, 133.4995f, 1523.79f);   // [W,S] = [(36.88214, 23.36888)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_28] = new Vector3(944.8705f, 138.6077f, 1467.708f);  // [W,S] = [(34.52819, 24.89856)]
            MapGpsCoordinates[MapLocation.PvE3_ClimbingRope_A03_S02] = new Vector3(1211.096f, 103.7661f, 1161.236f);    // [W,S] = [(28.03896, 33.258)]
            MapGpsCoordinates[MapLocation.BigAICamp_01] = new Vector3(810.6752f, 94.84235f, 932.3934f); // [W,S] = [(37.7992, 39.49998)]
            MapGpsCoordinates[MapLocation.Arena_Hunting] = new Vector3(577.64f, 121.717f, 752.2971f);   // [W,S] = [(43.47942, 44.41235)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_AztecWarrior_HiltSpawner] = new Vector3(956.6399f, 134.4811f, 1535.623f);  // [W,S] = [(34.24131, 23.04611)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_38] = new Vector3(547.3218f, 118.5775f, 869.1553f);  // [W,S] = [(44.21843, 41.22489)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_10] = new Vector3(1438.849f, 91.61936f, 1087.803f);    // [W,S] = [(22.4875, 35.26099)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_08_01_C27_28] = new Vector3(956.3168f, 137.3181f, 1596.764f); // [W,S] = [(34.24919, 21.3784)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_26] = new Vector3(1300.342f, 92.33302f, 1230.176f);  // [W,S] = [(25.86359, 31.37758)]
            MapGpsCoordinates[MapLocation.GoldenFish_01] = new Vector3(1448.258f, 91.0339f, 1178.442f); // [W,S] = [(22.25814, 32.7887)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_17_02_C10_39] = new Vector3(532.6371f, 105.7298f, 695.5716f); // [W,S] = [(44.57636, 45.95961)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_11_01_C30_31] = new Vector3(824.0441f, 142.5362f, 1519.478f); // [W,S] = [(37.47333, 23.48648)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_17_01_C10_39] = new Vector3(518.7084f, 123.4701f, 735.199f);  // [W,S] = [(44.91588, 44.87872)]
            MapGpsCoordinates[MapLocation.Lovers_04] = new Vector3(1029.052f, 169.3524f, 1492.034f);    // [W,S] = [(32.47628, 24.23506)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A01_S12_to_A04_S01b_02] = new Vector3(830.073f, 162.543f, 1323.784f);   // [W,S] = [(37.32638, 28.82429)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_35] = new Vector3(462.4638f, 113.8477f, 613.4684f);  // [W,S] = [(46.28684, 48.19908)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_11_02_C30_31] = new Vector3(911.2626f, 138.5872f, 1522.159f); // [W,S] = [(35.34739, 23.41336)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_02] = new Vector3(815.9954f, 144.4268f, 1388.91f); // [W,S] = [(37.66952, 27.0479)]
            MapGpsCoordinates[MapLocation.Fisherman_03_Bow] = new Vector3(1326.319f, 92.84f, 1245.28f); // [W,S] = [(25.2304, 30.96559)]
            MapGpsCoordinates[MapLocation.Przejscie_6_zSOA] = new Vector3(675.2466f, 151.0611f, 1324.278f); // [W,S] = [(41.10027, 28.81082)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_AztecWarrior_ObsidianScratch_2Spawner] = new Vector3(979.2919f, 136.6609f, 1540.673f); // [W,S] = [(33.68917, 22.90835)]
            MapGpsCoordinates[MapLocation.Village_03_fishing_outside] = new Vector3(1343.85f, 140.9749f, 1568.78f); // [W,S] = [(24.80309, 22.14172)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_29] = new Vector3(954.1736f, 136.5295f, 1383.977f);  // [W,S] = [(34.30143, 27.18244)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_02] = new Vector3(890.2789f, 135.1401f, 1366.537f);  // [W,S] = [(35.85886, 27.65815)]
            MapGpsCoordinates[MapLocation.Lovers_03] = new Vector3(1124.69f, 176.17f, 1452.82f);    // [W,S] = [(30.1451, 25.30466)]
            MapGpsCoordinates[MapLocation.GuardianMonkeys_06] = new Vector3(1313.54f, 151.159f, 1609.28f);  // [W,S] = [(25.54189, 21.03701)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_04] = new Vector3(1097.246f, 171.3366f, 1471.035f);  // [W,S] = [(30.81404, 24.80783)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_06] = new Vector3(1197.756f, 190.0195f, 1591.474f);    // [W,S] = [(28.36411, 21.52268)]
            MapGpsCoordinates[MapLocation.Chapel_07] = new Vector3(1301.847f, 138.9983f, 1533.92f); // [W,S] = [(25.82691, 23.09255)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_03] = new Vector3(889.1682f, 143.5634f, 1495.468f);    // [W,S] = [(35.88593, 24.1414)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_06_C22_23] = new Vector3(1123.554f, 175.137f, 1413.1f);   // [W,S] = [(30.17279, 26.38808)]
            MapGpsCoordinates[MapLocation.GuardianMonkeys_07] = new Vector3(1287.827f, 129.4516f, 1433.543f);   // [W,S] = [(26.16866, 25.83047)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_GoldenFishTrapSpawner] = new Vector3(1457.171f, 92.08701f, 1175.043f); // [W,S] = [(22.04088, 32.88139)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_14] = new Vector3(963.611f, 92.71806f, 973.5279f); // [W,S] = [(34.0714, 38.37799)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_07] = new Vector3(1316.97f, 145.2443f, 1585.58f);  // [W,S] = [(25.45828, 21.68346)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A03_S03_to_Mushroom] = new Vector3(963.76f, 122.39f, 1141.64f); // [W,S] = [(34.06776, 33.79251)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_GuardianMonkeys_EyesSpawner] = new Vector3(1310.966f, 150.4914f, 1589.606f);   // [W,S] = [(25.60462, 21.57365)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_09] = new Vector3(1294.937f, 91.44009f, 1370.626f);    // [W,S] = [(25.99534, 27.54662)]
            MapGpsCoordinates[MapLocation.PVE3_BigAICamp_09] = new Vector3(320.1024f, 112.0781f, 742.3892f);    // [W,S] = [(49.75689, 44.6826)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_10_C28_29] = new Vector3(973.2631f, 134.8005f, 1406.805f);    // [W,S] = [(33.83612, 26.55979)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_03] = new Vector3(820.227f, 140.1362f, 1464.578f);   // [W,S] = [(37.56637, 24.98395)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_14_02_C9_34] = new Vector3(392.5903f, 123.956f, 779.9718f);   // [W,S] = [(47.99, 43.65748)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_13_01_C33_34] = new Vector3(305.3275f, 127.6887f, 884.4379f); // [W,S] = [(50.11703, 40.80803)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_01] = new Vector3(999.2695f, 135.6284f, 1551.765f);    // [W,S] = [(33.20222, 22.60581)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_09_01_C27_30] = new Vector3(911.0722f, 137.4267f, 1570.204f); // [W,S] = [(35.35202, 22.10285)]
            MapGpsCoordinates[MapLocation.Chapel_03] = new Vector3(925.0468f, 134.8073f, 1473.622f);    // [W,S] = [(35.0114, 24.73727)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_18_02_C37_38] = new Vector3(605.1151f, 110.3544f, 772.2819f); // [W,S] = [(42.80972, 43.86724)]
            MapGpsCoordinates[MapLocation.GuardianMonkeys_04] = new Vector3(1249.996f, 187.16f, 1579.695f); // [W,S] = [(27.09077, 21.84398)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_42] = new Vector3(573.153f, 113.566f, 600.1981f);    // [W,S] = [(43.58879, 48.56105)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_20_02_C11_41] = new Vector3(578.261f, 113.2937f, 624.4084f);  // [W,S] = [(43.46428, 47.90068)]
            MapGpsCoordinates[MapLocation.PVE3_ClimbingRope_A02_S01_to_A02_S02_04] = new Vector3(1251.19f, 155.57f, 1499.6f);   // [W,S] = [(27.06167, 24.02868)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_27] = new Vector3(998.9966f, 145.3782f, 1678.864f);  // [W,S] = [(33.20887, 19.13902)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_19_01_C11_40] = new Vector3(643.388f, 100.8147f, 651.2411f);  // [W,S] = [(41.87682, 47.16878)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_36] = new Vector3(412.5227f, 121.95f, 812.3784f);    // [W,S] = [(47.50415, 42.77355)]
            MapGpsCoordinates[MapLocation.Arena_Planting] = new Vector3(294.647f, 117.025f, 738.629f);  // [W,S] = [(50.37736, 44.78516)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_39] = new Vector3(628.0947f, 109.2158f, 783.3916f);  // [W,S] = [(42.24959, 43.56421)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_25] = new Vector3(1333.37f, 91.48026f, 1336.779f);   // [W,S] = [(25.05853, 28.46984)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_09_02_C27_30] = new Vector3(942.882f, 138.707f, 1629.76f);    // [W,S] = [(34.57666, 20.4784)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_10] = new Vector3(1307.78f, 91.56807f, 1078.043f);   // [W,S] = [(25.68229, 35.5272)]
            MapGpsCoordinates[MapLocation.Lovers_02] = new Vector3(1207.029f, 158.192f, 1469.097f); // [W,S] = [(28.13809, 24.86068)]
            MapGpsCoordinates[MapLocation.PVE3_BigAICamp_08] = new Vector3(1337.391f, 91.941f, 1172.3f);    // [W,S] = [(24.96052, 32.95621)]
            MapGpsCoordinates[MapLocation.A04_S01_a_SacredPath_blocked] = new Vector3(631.7037f, 118.3573f, 905.4478f); // [W,S] = [(42.16162, 40.23496)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_01] = new Vector3(897.2843f, 138.7777f, 1667.923f);  // [W,S] = [(35.68811, 19.43746)]
            MapGpsCoordinates[MapLocation.Fisherman_02_Spear] = new Vector3(1438.578f, 92.277f, 1220.377f); // [W,S] = [(22.4941, 31.64484)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_GuardianMonkeys_TailSpawner] = new Vector3(1296.359f, 160.909f, 1608.225f);    // [W,S] = [(25.96068, 21.06579)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_mound_08] = new Vector3(1297.934f, 145.2441f, 1461.518f);    // [W,S] = [(25.92229, 25.06742)]
            MapGpsCoordinates[MapLocation.Lovers_07] = new Vector3(1100.776f, 186.312f, 1566.171f); // [W,S] = [(30.728, 22.21287)]
            MapGpsCoordinates[MapLocation.Chapel_06] = new Vector3(1300.463f, 94.01117f, 1407.789f);    // [W,S] = [(25.86065, 26.53295)]
            MapGpsCoordinates[MapLocation.PVE3_food_ration_hanging_05] = new Vector3(1154.936f, 183.3588f, 1577.897f);  // [W,S] = [(29.40786, 21.89304)]
            MapGpsCoordinates[MapLocation.Chapel_01] = new Vector3(849.4526f, 135.0394f, 1518.745f);    // [W,S] = [(36.854, 23.50648)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_41] = new Vector3(593.0156f, 112.4641f, 610.3788f);  // [W,S] = [(43.10464, 48.28335)]
            MapGpsCoordinates[MapLocation.AztecWarrior_06] = new Vector3(1001.25f, 143.8516f, 1437.252f);   // [W,S] = [(33.15394, 25.72931)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_34] = new Vector3(317.114f, 119.993f, 864.155f); // [W,S] = [(49.82973, 41.36127)]
            MapGpsCoordinates[MapLocation.Lovers_08] = new Vector3(1192.484f, 188.423f, 1608.933f); // [W,S] = [(28.49262, 21.04648)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_Lovers_Charm02Spawner] = new Vector3(1281.53f, 99.55441f, 1427.662f);  // [W,S] = [(26.32213, 25.99088)]
            MapGpsCoordinates[MapLocation.A04_S01_a_SacredPath_entrance] = new Vector3(625.1541f, 114.9915f, 915.1239f);    // [W,S] = [(42.32127, 39.97103)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_21] = new Vector3(1179.5f, 180.199f, 1556.2f);   // [W,S] = [(28.80912, 22.48485)]
            MapGpsCoordinates[MapLocation.PVE3_Patrol_02_C7_31] = new Vector3(819.2789f, 140.7081f, 1436.777f); // [W,S] = [(37.58949, 25.74227)]
            MapGpsCoordinates[MapLocation.GuardianMonkeys_03] = new Vector3(1342.722f, 137.63f, 1526.934f); // [W,S] = [(24.83059, 23.28311)]
            MapGpsCoordinates[MapLocation.AztecWarrior_07] = new Vector3(805.2234f, 151.2611f, 1384.388f);  // [W,S] = [(37.93208, 27.17125)]
            MapGpsCoordinates[MapLocation.Chapel_02] = new Vector3(920.8006f, 134.9925f, 1557.269f);    // [W,S] = [(35.11489, 22.45567)]
            MapGpsCoordinates[MapLocation.GuardianMonkeys_01] = new Vector3(1289.558f, 140.26f, 1496.178f); // [W,S] = [(26.12645, 24.12202)]
            MapGpsCoordinates[MapLocation.PVE3_SmallAICamp_24] = new Vector3(1032.506f, 169.4303f, 1501.09f);   // [W,S] = [(32.39209, 23.98803)]
            MapGpsCoordinates[MapLocation.LegendaryQuest_GuardianMonkeys_ClawsSpawner] = new Vector3(1316.597f, 152.725f, 1603.657f);   // [W,S] = [(25.46737, 21.19039)]
            MapGpsCoordinates[MapLocation.Arena_Fishing] = new Vector3(598.8151f, 118.199f, 537.8301f); // [W,S] = [(42.96328, 50.26221)]
            MapGpsCoordinates[MapLocation.Lovers_05] = new Vector3(1175.947f, 185.0796f, 1505.912f);	// [W,S] = [(28.89572, 23.8565)]
        }

        protected virtual void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            
            StartCoroutine(LoadTexture(delegate (Texture2D mapt)
            {
                LocalMapTexture = mapt;
            }, LocalMapTextureUrl));

            StartCoroutine(LoadTexture(delegate (Texture2D markert)
            {
                LocalMapLocationMarkerTexture = markert;
            }, LocalMapLocationMarkerTextureUrl));
            InitData();
            ShortcutKey = GetShortcutKey(nameof(ShortcutKey));
            FastTravelShortcutKey = GetShortcutKey(nameof(FastTravelShortcutKey));
            CustomMapShortcutKey = GetShortcutKey(nameof(CustomMapShortcutKey));
            PlayerGpsShortcutKey = GetShortcutKey(nameof(PlayerGpsShortcutKey));
            LogDebugSpawnerInfoShortcutKey = GetShortcutKey(nameof(LogDebugSpawnerInfoShortcutKey));
        }

        private IEnumerator LoadTexture(Action<Texture2D> action, string url)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.ConnectionError)
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
                foreach (var mapLocationsGpsCoordinates in MapGpsCoordinates)
                {
                    (int gps_lat, int gps_long) = LocalToGpsCoordinates(mapLocationsGpsCoordinates.Value);
                    float gpsLatitude = gps_lat;
                    float gpsLongitude = gps_long;
                    float mapPointerPosX = LocalMapPointerPosition.x + LocalMapTexture.width * LocalMapZoom;
                    float mapPointerPosY = LocalMapPointerPosition.y;
                    float num2 = (LocalMapTexture.width - MapOffset.x) / MapGridCount.x * LocalMapZoom;
                    float num3 = (LocalMapTexture.height - MapOffset.y) / MapGridCount.y * LocalMapZoom;
                    float WestCoordinate = mapPointerPosX - (gpsLatitude - MapGridOffset.x) * num2;
                    float SouthCoordinate = mapPointerPosY + (gpsLongitude - MapGridOffset.y) * num3;

                    GUI.DrawTexture(
                        new Rect(WestCoordinate, SouthCoordinate, LocalMapLocationMarkerIconSize / 5f, LocalMapLocationMarkerIconSize / 5f),
                        LocalMapLocationMarkerTexture);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DrawMapLocations));
            }
        }

        private void DrawPlayerMapLocation()
        {
            try
            {
                (int gps_lat, int gps_long) = LocalToGpsCoordinates(LocalPlayer.transform.position);
                float gpsLatitude = gps_lat;
                float gpsLongitude = gps_long;
                float mapPointerPosX = LocalMapPointerPosition.x + LocalMapTexture.width * LocalMapZoom;
                float mapPointerPosY = LocalMapPointerPosition.y;
                float num2 = (LocalMapTexture.width - MapOffset.x) / MapGridCount.x * LocalMapZoom;
                float num3 = (LocalMapTexture.height - MapOffset.y) / MapGridCount.y * LocalMapZoom;
                float WestCoordinate = mapPointerPosX - (gpsLatitude - MapGridOffset.x) * num2;
                float SouthCoordinate = mapPointerPosY + (gpsLongitude - MapGridOffset.y) * num3;

                GUI.DrawTexture(
                    new Rect(WestCoordinate, SouthCoordinate, LocalMapLocationMarkerIconSize, LocalMapLocationMarkerIconSize),
                    LocalMapLocationMarkerTexture);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DrawPlayerMapLocation));
            }
        }

        private (int gps_lat, int gps_long) LocalToGpsCoordinates(Vector3 localPosition)
        {
            try
            {
                Vector3 position2 = LocalMapTab.m_WorldZeroDummy.position;
                Vector3 position3 = LocalMapTab.m_WorldOneDummy.position;
                float deltaX = position3.x - position2.x;
                float deltaZ = position3.z - position2.z;
                float num3 = deltaX / 35f;
                float num4 = deltaZ / 27f;
                Vector3 vector = LocalMapTab.m_WorldZeroDummy.InverseTransformPoint(localPosition);
                int gps_lat = Mathf.FloorToInt(vector.x / num3) + 20;
                int gps_long = Mathf.FloorToInt(vector.z / num4) + 14;
                return (gps_lat, gps_long);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(LocalToGpsCoordinates));
                return default;
            }
        }

        public Vector3 LocalToWorldPosition(Vector3 localPosition)
        {
            Vector3 worldPosition = Vector3.zero;
            worldPosition.y = Screen.height * localPosition.y;
            if (Screen.width / (float)Screen.height < CJTools.Math.s_AspectRatio16By9)
            {
                worldPosition.x = Screen.width * localPosition.x;
            }
            else
            {
                worldPosition.x = Screen.width * 0.5f + Screen.height * CJTools.Math.s_AspectRatio16By9 * localPosition.x * 0.5f;
            }

            return worldPosition;
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

        private void ShowHUDBigInfo(string text)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();

            HUDBigInfo bigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = 2f;
            HUDBigInfoData bigInfoData = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            bigInfo.AddInfo(bigInfoData);
            bigInfo.Show(true);
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            var messages = ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages)));
            messages.AddMessage($"{localization.Get(localizedTextKey)}  {localization.Get(itemID)}");
        }

        protected virtual void Awake()
        {
            Instance = this;
        }

        protected virtual void OnDestroy()
        {
            Instance = null;
        }

        protected virtual void Update()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                if (Input.GetKeyDown(ShortcutKey))
                {
                    if (!ShowModTeleporterScreen)
                    {
                        InitData();
                        InitMapLocations();
                        EnableCursor(true);
                    }
                    ToggleShowUI(0);
                    if (!ShowModTeleporterScreen)
                    {
                        EnableCursor(false);
                    }
                }

                if (!GameMapsUnlocked)
                {
                    InitData();
                    LocalPlayer.UnlockMap();
                    GameMapsUnlocked = true;
                }

                if (Input.GetKeyDown(FastTravelShortcutKey))
                {
                    if (!ShowFastTravelScreen)
                    {
                        InitData();
                        InitMapLocations();
                        EnableCursor(true);
                    }
                    ToggleShowUI(1);
                    if (!ShowFastTravelScreen)
                    {
                        EnableCursor(false);
                    }
                }

                if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(CustomMapShortcutKey))
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

                if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(PlayerGpsShortcutKey))
                {
                    InitData();
                    GetPlayerGpsCoordinatesInfo();
                }

                if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(LogDebugSpawnerInfoShortcutKey))
                {
                    InitData();
                    PrintDebugSpawnerInfoToLogfile();
                }
            }
        }

        private void ToggleShowUI(int controlId)
        {
            switch (controlId)
            {
                case 0:
                    ShowModTeleporterScreen = !ShowModTeleporterScreen;                  
                    return;
                case 1:
                    ShowFastTravelScreen = !ShowFastTravelScreen;
                    return;
                case 2:
                    ShowMap = !ShowMap;
                    return;
                case 3:
                    ShowModInfo = !ShowModInfo;
                    return;
                default:
                    ShowModTeleporterScreen = !ShowModTeleporterScreen;                
                    ShowFastTravelScreen = !ShowFastTravelScreen;
                    ShowModInfo = !ShowModInfo;
                    return;
            }
        }

        private void OnGUI()
        {
            if (ShowMap)
            {
                InitData();
                InitMapLocations();
                ShowMapWindow();
            }
            if (ShowFastTravelScreen)
            {
                InitData();
                InitMapLocations();
                InitSkinUI();
                ShowFastTravelWindow();
            }
            if (ShowModTeleporterScreen)
            {
                InitData();
                InitMapLocations();
                InitSkinUI();
                ShowModTeleporterWindow();
            }
        }

        private void ShowMapWindow()
        {
            try
            {
                GUI.DrawTexture( new Rect(LocalMapPointerPosition, new Vector2(LocalMapTexture.width * LocalMapZoom, LocalMapTexture.height * LocalMapZoom)), LocalMapTexture);

                if (ShowMap && Input.GetMouseButton(0))
                {
                    Vector2 vector = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));                    
                    LocalMapPointerPosition -= vector * 20f;
                    LocalMapPointerPosition.x = Mathf.Clamp(LocalMapPointerPosition.x, -LocalMapTexture.width, Screen.width);
                    LocalMapPointerPosition.y = Mathf.Clamp(LocalMapPointerPosition.y, -LocalMapTexture.height, Screen.height);
                }

                LocalMapZoom = Mathf.Clamp(LocalMapZoom + Input.mouseScrollDelta.y / 20f, 1f, 3f);

                DrawPlayerMapLocation();
                DrawMapLocations();
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ShowMapWindow));
            }
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void ShowFastTravelWindow()
        {
            ConfirmFastTravelScreen = GUILayout.Window(GetHashCode(), ConfirmFastTravelScreen, InitConfirmFastTravelScreen, " Fast travel?", GUI.skin.window);
        }

        private void ShowModTeleporterWindow()
        {
            ModTeleporterScreenId = GetHashCode();
            
            CurrentMapLocation = LastMapLocationTeleportedTo;
            NextMapLocationID = (int)LastMapLocationTeleportedTo + 1;
            NextMapLocation = MapLocations.GetValueOrDefault(NextMapLocationID);

            ModTeleporterScreen = GUILayout.Window(ModTeleporterScreenId, ModTeleporterScreen, InitModTeleporterScreen, ModTeleporterScreenTitle,
                                                                                                        GUI.skin.window,
                                                                                                        GUILayout.ExpandWidth(true),
                                                                                                        GUILayout.MinWidth(ModTeleporterScreenMinWidth),
                                                                                                        GUILayout.MaxWidth(ModTeleporterScreenMaxWidth),
                                                                                                        GUILayout.ExpandHeight(true),
                                                                                                        GUILayout.MinHeight(ModTeleporterScreenMinHeight),
                                                                                                        GUILayout.MaxHeight(ModTeleporterScreenMaxHeight));
        }

        private void ModTeleporterScreenMenuBox()
        {
            string CollapseButtonText = IsModTeleporterScreenMinimized ? "O" : "-";
            if (GUI.Button(new Rect(ModTeleporterScreen.width - 40f, 0f, 20f, 20f), CollapseButtonText, GUI.skin.button))
            {
                CollapseModTeleporterWindow();
            }

            if (GUI.Button(new Rect(ModTeleporterScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void CollapseModTeleporterWindow()
        {
            if (!IsModTeleporterScreenMinimized)
            {
                ModTeleporterScreen = new Rect(ModTeleporterScreen.x, ModTeleporterScreen.y, ModTeleporterScreenTotalWidth, ModTeleporterScreenMinHeight);
                IsModTeleporterScreenMinimized = true;
            }
            else
            {
                ModTeleporterScreen = new Rect(ModTeleporterScreen.x, ModTeleporterScreen.y, ModTeleporterScreenTotalWidth, ModTeleporterScreenTotalHeight);
                IsModTeleporterScreenMinimized = false;
            }
            ShowModTeleporterWindow();
        }

        private void RefreshWindow(float deltaHeight)
        {
            ModTeleporterScreenStartPositionX = ModTeleporterScreen.x;
            ModTeleporterScreenStartPositionY = ModTeleporterScreen.y;
            ModTeleporterScreenTotalWidth = ModTeleporterScreen.width;
            ModTeleporterScreenTotalHeight = ModTeleporterScreen.height - deltaHeight;

            if (IsModTeleporterScreenMinimized)
            {
                ModTeleporterScreen = new Rect(ModTeleporterScreenStartPositionX, ModTeleporterScreenStartPositionY, ModTeleporterScreenTotalWidth, ModTeleporterScreenMinHeight);             
            }
            else
            {
                ModTeleporterScreen = new Rect(ModTeleporterScreenStartPositionX, ModTeleporterScreenStartPositionY, ModTeleporterScreenTotalWidth, ModTeleporterScreenTotalHeight);              
            }
            ShowModTeleporterWindow();
        }

        private void CloseWindow()
        {
            ShowFastTravelScreen = false;
            ShowModTeleporterScreen = false;
            ShowMap = false;
            EnableCursor(false);
        }

        private void InitModTeleporterScreen(int windowID)
        {
            ModTeleporterScreenStartPositionX = ModTeleporterScreen.x;
            ModTeleporterScreenStartPositionY = ModTeleporterScreen.y;
            ModTeleporterScreenTotalWidth = ModTeleporterScreen.width;

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModTeleporterScreenMenuBox();

                if (!IsModTeleporterScreenMinimized)
                {
                    ModTeleporterManagerBox();
                    CustomMapLocationManagerBox();
                    MapLocationsManagerBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ModTeleporterManagerBox()
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{ModName} Manager", LocalStylingManager.ColoredHeaderLabel(Color.yellow));
                        GUILayout.Label($"{ModName} Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                        using (new GUILayout.VerticalScope(GUI.skin.box))
                        {
                            if (GUILayout.Button($"Mod Info", GUI.skin.button))
                            {
                                ToggleShowUI(3);
                            }
                            if (ShowModInfo)
                            {
                                ModInfoBox();
                            }                           
                            MultiplayerOptionBox();
                            ShortcutKeyInfoBox();
                        }
                    }
                }
                else
                {
                    OnlyForSingleplayerOrWhenHostBox();
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ModTeleporterManagerBox));
            }
        }

        private void ModInfoBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModInfoScrollViewPosition = GUILayout.BeginScrollView(ModInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));

                GUILayout.Label("Mod Info", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.GameID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.GameID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.ID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.ID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.UniqueID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.UniqueID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.Version)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.Version}", LocalStylingManager.FormFieldValueLabel);
                }

                GUILayout.Label("Buttons Info", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));
                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.ID)}:", LocalStylingManager.FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.ID}", LocalStylingManager.FormFieldValueLabel);
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.KeyBinding)}:", LocalStylingManager.FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.KeyBinding}", LocalStylingManager.FormFieldValueLabel);
                    }
                }

                GUILayout.EndScrollView();
            }
        }

        private void MultiplayerOptionBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Multiplayer Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                    string multiplayerOptionMessage = string.Empty;
                    if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                    {
                        if (IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are the game host";
                        }
                        if (IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host allowed usage";
                        }
                        GUILayout.Label(PermissionChangedMessage($"granted", multiplayerOptionMessage), LocalStylingManager.ColoredFieldValueLabel(Color.green));
                    }
                    else
                    {
                        if (!IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are not the game host";
                        }
                        if (!IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host did not allow usage";
                        }
                        GUILayout.Label(PermissionChangedMessage($"revoked", $"{multiplayerOptionMessage}"), LocalStylingManager.ColoredFieldValueLabel(Color.yellow));
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MultiplayerOptionBox));
            }
        }

        private void ShortcutKeyInfoBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"Shortcut key info", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To select a map location", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"[{ShortcutKey}]", LocalStylingManager.FormFieldValueLabel);
                }
                
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To teleport to next map location", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"[{FastTravelShortcutKey}]", LocalStylingManager.FormFieldValueLabel);
                }

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To show the map", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"[LeftAlt]+[{CustomMapShortcutKey}]", LocalStylingManager.FormFieldValueLabel);
                }

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To show your current GPS position and set these as custom coordinates", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"[LeftAlt]+[{PlayerGpsShortcutKey}]", LocalStylingManager.FormFieldValueLabel);
                }

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To log debug spawners GPS positions", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"[LeftAlt]+[{LogDebugSpawnerInfoShortcutKey}]", LocalStylingManager.FormFieldValueLabel);
                }
            }
        }

        private void CustomMapLocationManagerBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"Custom map location Manager", LocalStylingManager.ColoredHeaderLabel(Color.yellow));

                GUILayout.Label($"Custom map location Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Current custom map location set to GPS coordinates (W,S): {LocalToGpsCoordinates(CustomGpsCoordinates)}:", LocalStylingManager.ColoredFieldNameLabel(Color.cyan));
                    GUILayout.Label($"x: {CustomGpsCoordinates.x}f, y: {CustomGpsCoordinates.y}f, z: {CustomGpsCoordinates.z}f", LocalStylingManager.ColoredFieldValueLabel(Color.cyan));
                }

                GUILayout.Label($"Set GPS coordinates x, y and z to bind to your custom map location. Then click [Bind custom].", LocalStylingManager.TextLabel);

                using (var coordScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("x: ", LocalStylingManager.FormFieldNameLabel);
                    CustomX = GUILayout.TextField(CustomX, LocalStylingManager.FormInputTextField);
                    GUILayout.Label("y: ", LocalStylingManager.FormFieldNameLabel);
                    CustomY = GUILayout.TextField(CustomY, LocalStylingManager.FormInputTextField);
                    GUILayout.Label("z: ", LocalStylingManager.FormFieldNameLabel);
                    CustomZ = GUILayout.TextField(CustomZ, LocalStylingManager.FormInputTextField);

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

        private void MapLocationsManagerBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label($"Map locations Manager", LocalStylingManager.ColoredHeaderLabel(Color.yellow));

                GUILayout.Label($"Map locations Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Last map location teleported to:", LocalStylingManager.ColoredFieldNameLabel(Color.cyan));
                    GUILayout.Label($"{LastMapLocationTeleportedTo.ToString().Replace("_", " ")}", LocalStylingManager.ColoredFieldValueLabel(Color.cyan));
                }

                GUILayout.Label("Select next map location to teleport to.", LocalStylingManager.TextLabel);

                MapLocationsScrollViewBox();

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    NextMapLocationID = (int)EnumUtils<MapLocation>.GetValue(SelectedMapLocationName);
                    NextMapLocation = MapLocations.GetValueOrDefault(NextMapLocationID);
                    GpsCoordinates = MapGpsCoordinates.GetValueOrDefault(NextMapLocation);

                    GUILayout.Label($"Teleport to {SelectedMapLocationName.ToString().Replace("_", " ")}?", LocalStylingManager.TextLabel);
                    if (GUILayout.Button($"Go (W,S) {LocalToGpsCoordinates(GpsCoordinates)}", GUI.skin.button, GUILayout.Width(150f)))
                    {
                        OnClickTeleport();
                        CloseWindow();
                    }
                }
            }
        }

        private void MapLocationsScrollViewBox()
        {
            MapLocationsScrollViewPosition = GUILayout.BeginScrollView(MapLocationsScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(300f));
            
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                string[] mapLocationNames = GetMapLocationNames();
                int _SelectedMapLocationIndex = SelectedMapLocationIndex;
                if (mapLocationNames != null)
                {
                    SelectedMapLocationIndex = GUILayout.SelectionGrid(SelectedMapLocationIndex, mapLocationNames, 3, LocalStylingManager.ColoredSelectedGridButton(_SelectedMapLocationIndex!= SelectedMapLocationIndex));
                    SelectedMapLocationName = mapLocationNames[SelectedMapLocationIndex].Replace(" ", "_");
                }
            }
            GUILayout.EndScrollView();
        }

        private void InitConfirmFastTravelScreen(int windowID)
        {
            using (new GUILayout.VerticalScope(LocalStylingManager.WindowBox))
            {
                GUI.backgroundColor = LocalStylingManager.DefaultBackGroundColor;
                GUILayout.Label($"Teleport to {NextMapLocation.ToString().Replace("_", " ")}?", LocalStylingManager.TextLabel);
                
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    if (GUILayout.Button("Yes", GUI.skin.button, GUILayout.Width(150f)))
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
                    if (GUILayout.Button("No", GUI.skin.button, GUILayout.Width(150f)))
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
                if (ShowModTeleporterScreen || ShowFastTravelScreen)
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

        public void GetPlayerGpsCoordinatesInfo()
        {
            try
            {
                Vector3 playerPosition = LocalPlayer.GetWorldPosition();
                CustomX = playerPosition.x.ToString();
                CustomY = playerPosition.y.ToString();
                CustomZ = playerPosition.z.ToString();
                ShowHUDBigInfo(HUDBigInfoMessage($"Player GPS coordinates\nx: {CustomX}, y: {CustomY} z: {CustomZ}\nset to bind as custom map coordinates\n (W,S): {LocalToGpsCoordinates(playerPosition)} . ", MessageType.Info, Color.green));
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetPlayerGpsCoordinatesInfo));
            }
        }

        public void PrintDebugSpawnerInfoToLogfile()
        {
            try
            {
                StringBuilder logBuilder = new StringBuilder($"");
                DebugSpawner[] debugSpawners = FindObjectsOfType<DebugSpawner>();
                for (int i = 0; i < debugSpawners.Length; i++)
                {
                    logBuilder.AppendLine(PositionInfo(debugSpawners[i].gameObject.transform.position, debugSpawners[i].gameObject.name));
                }
                ModAPI.Log.Write(logBuilder.ToString());
                ShowHUDBigInfo(HUDBigInfoMessage($"{nameof(DebugSpawner)} info logged to\n{LogPath}.", MessageType.Info, Color.green));
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(PrintDebugSpawnerInfoToLogfile));
            }
        }

        public string PositionInfo(Vector3 position, string mapLocation)
        {
            try
            {
                string info = $"\nMapGpsCoordinates[MapLocation.{mapLocation}] = new Vector3({position.x}f, {position.y}f, {position.z}f);";
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
