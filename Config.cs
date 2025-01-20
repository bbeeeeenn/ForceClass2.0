using Newtonsoft.Json;
using TShockAPI;

namespace ForceClass;

public class Config
{
    public bool Enabled = true;
    public bool SameAll = false;
    public string PrimaryClass = "WARRIOR";
    public string SecondaryClass = "NONE";
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
