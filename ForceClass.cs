using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
        #region Metadata
        public override string Name => "ForceClass 2.0";
        public override string Author => "TRANQUILZOIIP - github.com/bbeeeeenn";
        public override string Description => base.Description;
        public override Version Version => new(2, 0);

        public TShockPlugin(Main game)
            : base(game) { }
        #endregion
        private readonly List<string> CommandInstructions = new()
        {
            "/class get <class> - to get a class.\nYou can have 2 classes: Primary and Secondary.",
            "/class all - to see the classes of all currently online players",
            "Available Classes:",
            "   [c/de881f:WARRIOR] - for Melee weapons.",
            "   [c/1ee3b2:RANGER] - for Bows, Guns, and Throwables.",
            "   [c/aa1cbd:MAGE] - for Magic weapons.",
            "   [c/328adb:SUMMONER] - for Whips and Summon weapons.",
            "   [c/328adb:SUPREME] - VIP Exclusive; Freely uses anything.",
        };
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
            ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
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
        private void OnGameInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command(OnCommand, "class") { AllowServer = false });
        }

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

        private readonly Dictionary<string, List<string>> PlayerClasses = new();

        #region  Player join
        private void OnPlayerLogin(PlayerPostLoginEventArgs args)
        {
            TSPlayer player = args.Player;
            if (player == null)
                return;

            if (player.Group.HasPermission("tshock.godmode"))
            {
                PlayerClasses[player.Account.Name] = new List<string>() { "SUPREME", "SUPREME" };
                return;
            }

            using QueryResult result = TShock.DB.QueryReader(
                @"
                    SELECT * FROM PlayerClass
                    WHERE World=@0 AND Username=@1
                ",
                Main.worldID,
                player.Account.Name
            );
            if (result.Read())
            {
                string Primary = result.Reader.Get<string>("Primary");
                string Secondary = result.Reader.Get<string>("Secondary");
                PlayerClasses[player.Account.Name] = new List<string>() { Primary, Secondary };
            }
            else
            {
                TShock.DB.Query(
                    @"
                        INSERT INTO PlayerClass (World, Username)
                        VALUES (@0, @1)
                    ",
                    Main.worldID,
                    player.Account.Name
                );
                PlayerClasses[player.Account.Name] = new List<string>() { "NONE", "NONE" };
            }
        }
        #endregion

        #region Command
        private void OnCommand(CommandArgs args)
        {
            TSPlayer player = args.Player;
            if (player == null || !player.Active)
                return;

            if (
                args.Parameters.Count == 0
                || (
                    args.Parameters.Count >= 1
                    && !new List<string>() { "all", "get" }.Contains(args.Parameters[0])
                )
            )
            // Send Instructions
            {
                player.SendMessage(string.Join("\n", CommandInstructions), Color.LightCyan);
                return;
            }
            if (args.Parameters[0].ToLower() == "all")
            // Send all playerclasses
            {
                List<string> strings = new() { "-- Player Classes --" };
                foreach (TSPlayer p in TShock.Players)
                {
                    strings.Add(
                        $"{p.Name} Primary:{PlayerClasses[p.Account.Name][0]} Secondary:{PlayerClasses[p.Account.Name][1]}"
                    );
                }
                player.SendMessage(string.Join("\n", strings), Color.LightCyan);
                return;
            }

            if (args.Parameters.Count <= 1)
            {
                player.SendMessage("", Color.LightCyan);
            }
        }
        #endregion
    }
}
