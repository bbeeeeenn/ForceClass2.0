using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;

namespace ForceClass
{
    [ApiVersion(2, 1)]
    public class TShockPlugin : TerrariaPlugin
    {
        public override string Name => "ForceClass 2.0";
        public override string Author => "TRANQUILZOIIP - github.com/bbeeeeenn";
        public override string Description => base.Description;
        public override Version Version => new(2, 0);

        public static readonly List<string> Classes = new()
        {
            "WARRIOR",
            "RANGER",
            "MAGE",
            "SUMMONER",
            "SUPREME",
        };

        public static readonly string ConfigPath = "tshock/ForceClass.json";
        private static Config Config = new();

        public TShockPlugin(Main game)
            : base(game) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            InitializeDatabase();
            Config = Config.Load();
            GeneralHooks.ReloadEvent += OnReload;
            PlayerHooks.PlayerPostLogin += OnPlayerLogin;
        }

        #region Initialize Database
        public static void InitializeDatabase()
        {
            TShock.DB.Query(
                @"
                    CREATE TABLE IF NOT EXISTS PlayerClass (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        World INTEGER NOT NULL,
                        Username TEXT NOT NULL,
                        Primary TEXT DEFAULT None,
                        Secondary TEXT DEFAULT None
                    )
                "
            );
        }
        #endregion

        #region Reloading config
        private void OnReload(ReloadEventArgs e)
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            Config NewConfig = Config.Load();
            if (!Classes.Contains(NewConfig.ClassAll.ToUpper()))
            {
                NewConfig.ClassAll = "WARRIOR";
                TShock.Log.ConsoleError(
                    "There is a typo in the config 'ClassAll' property. It is now temporarily set to 'WARRIOR'."
                );
            }

            if (Config.Enabled != NewConfig.Enabled)
            {
                TShock.Utils.Broadcast(
                    $"ForceClass has been {(NewConfig.Enabled ? "ENABLED" : "DISABLED. Players are now free to use any weapon")}.",
                    Color.LightCyan
                );

                if (NewConfig.Enabled)
                {
                    if (NewConfig.SameAll)
                    {
                        TShock.Utils.Broadcast(
                            $"Players are now forced to be '{NewConfig.ClassAll}'.",
                            Color.LightCyan
                        );
                    }
                    else
                    {
                        TShock.Utils.Broadcast(
                            "Players are now forced to choose a class.",
                            Color.LightCyan
                        );
                    }
                }
            }
            if (
                (Config.SameAll != NewConfig.SameAll || Config.ClassAll != NewConfig.ClassAll)
                && Config.Enabled == NewConfig.Enabled
            )
            {
                if (NewConfig.SameAll)
                {
                    TShock.Utils.Broadcast(
                        $"Players are now forced to be '{NewConfig.ClassAll}'.",
                        Color.LightCyan
                    );
                }
                else
                {
                    TShock.Utils.Broadcast("Players are now on their own class.", Color.LightCyan);
                }
            }

            Config = NewConfig;
        }
        #endregion

        #region  Player join
        private void OnPlayerLogin(PlayerPostLoginEventArgs args)
        {
            TSPlayer player = args.Player;
        }
        #endregion
    }
}
