using Newtonsoft.Json;
using TShockAPI;

namespace ForceClass;

public class Config
{
    public bool Enabled = true;
    public bool SameAll = false;
    public string PrimaryClass = "WARRIOR";
    public string SecondaryClass = "NONE";
    public Dictionary<string, List<Dictionary<string, int>>> StartingWeapons = new()
    {
        {
            "WARRIOR",
            new List<Dictionary<string, int>>()
            {
                new()
                {
                    { "netID", Terraria.ID.ItemID.BatBat },
                    { "prefixID", Terraria.ID.PrefixID.Legendary },
                    { "stack", 1 },
                },
                new()
                {
                    { "netID", Terraria.ID.ItemID.FeralClaws },
                    { "prefixID", Terraria.ID.PrefixID.Violent },
                    { "stack", 1 },
                },
            }
        },
        {
            "RANGER",
            new List<Dictionary<string, int>>()
            {
                new()
                {
                    { "netID", Terraria.ID.ItemID.EndlessMusketPouch },
                    { "prefixID", 0 },
                    { "stack", 1 },
                },
                new()
                {
                    { "netID", Terraria.ID.ItemID.EndlessQuiver },
                    { "prefixID", 0 },
                    { "stack", 1 },
                },
            }
        },
        {
            "MAGE",
            new List<Dictionary<string, int>>()
            {
                new()
                {
                    { "netID", Terraria.ID.ItemID.ManaFlower },
                    { "prefixID", Terraria.ID.PrefixID.Arcane },
                    { "stack", 1 },
                },
                new()
                {
                    { "netID", Terraria.ID.ItemID.LesserManaPotion },
                    { "prefixID", 0 },
                    { "stack", 999 },
                },
            }
        },
        {
            "SUMMONER",
            new List<Dictionary<string, int>>()
            {
                new()
                {
                    { "netID", Terraria.ID.ItemID.FlinxStaff },
                    { "prefixID", Terraria.ID.PrefixID.Mythical },
                    { "stack", 1 },
                },
                new()
                {
                    { "netID", Terraria.ID.ItemID.FlinxFurCoat },
                    { "prefixID", 0 },
                    { "stack", 1 },
                },
            }
        },
    };
    public int PunishDuration = 2;
    public int ErrorMessageInterval = 5;

    public string INSTRUCTIONS =
        "// Valid classes are WARRIOR, RANGER, MAGE, SUMMONER, SUPREME, or NONE. Case insensitive. Classes will be automatically set to default if there is a typo.";

    public Config Load()
    {
        if (!File.Exists(TShockPlugin.ConfigPath))
        {
            File.WriteAllText(
                TShockPlugin.ConfigPath,
                JsonConvert.SerializeObject(this, Formatting.Indented)
            );
            return new Config();
        }

        Config? deserialized = JsonConvert.DeserializeObject<Config>(
            File.ReadAllText(TShockPlugin.ConfigPath)
        );

        if (deserialized == null)
        {
            TShock.Log.ConsoleError(
                "[ForceClass] Failed to load Config file\n[ForceClass] Now using the default settings"
            );
            return new Config();
        }

        return deserialized;
    }
}
