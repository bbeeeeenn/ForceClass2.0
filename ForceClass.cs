﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
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
            ">- Instructions -<",
            "[c/ffffff:/class get <class>] - to get a class.\nYou can have 2 classes: Primary and Secondary.",
            "[c/ffffff:/class all] - to see the classes of all currently online players",
            "Available Classes:",
            "   [c/de881f:WARRIOR] - for Melee weapons.",
            "   [c/1ee3b2:RANGER] - for Bows, Guns, and Throwables.",
            "   [c/aa1cbd:MAGE] - for Magic weapons.",
            "   [c/328adb:SUMMONER] - for Whips and Summon weapons.",
            "   [c/fbff00:SUPREME] - VIP Exclusive; Freely uses anything.",
        };
        public static readonly List<string> Classes = new()
        {
            "WARRIOR",
            "RANGER",
            "MAGE",
            "SUMMONER",
            "SUPREME",
        };
        public readonly Dictionary<string, string> ClassColors = new()
        {
            { "NONE", "[c/ffffff:[NONE][c/ffffff:]]" },
            { "WARRIOR", "[c/de881f:[WARRIOR][c/de881f:]]" },
            { "RANGER", "[c/1ee3b2:[RANGER][c/1ee3b2:]]" },
            { "MAGE", "[c/aa1cbd:[MAGE][c/aa1cbd:]]" },
            { "SUMMONER", "[c/328adb:[SUMMONER][c/328adb:]]" },
            { "SUPREME", "[c/fbff00:[SUPREME][c/fbff00:]]" },
        };

        public static readonly string ConfigPath = "tshock/ForceClass.json";
        private static Config Config = new();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
                GeneralHooks.ReloadEvent -= OnReload;
                PlayerHooks.PlayerPostLogin -= OnPlayerLogin;
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            }
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            InitializeDatabase();
            Config = SanitizeConfig(Config.Load());
            ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
            GeneralHooks.ReloadEvent += OnReload;
            PlayerHooks.PlayerPostLogin += OnPlayerLogin;
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        #region Initialize
        public static void InitializeDatabase()
        {
            TShock.DB.Query(
                @"
                CREATE TABLE IF NOT EXISTS PlayerClass (
                    World INTEGER NOT NULL,
                    Username TEXT NOT NULL,
                    PrimaryClass TEXT DEFAULT 'NONE',
                    SecondaryClass TEXT DEFAULT 'NONE'
                )
                "
            );
        }

        private void OnGameInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command(OnCommand, "class"));
        }
        #endregion

        #region Reloading config
        private void OnReload(ReloadEventArgs e)
        {
            ReloadConfig();
        }

        public void ReloadConfig()
        {
            Config NewConfig = SanitizeConfig(Config.Load());

            if (Config.Enabled != NewConfig.Enabled)
            {
                TShock.Utils.Broadcast(
                    $"ForceClass has been {(NewConfig.Enabled ? "ENABLED." : "DISABLED. Players are now free to use any weapon.")}",
                    Color.Cyan
                );
            }
            if (NewConfig.Enabled && Config.SameAll != NewConfig.SameAll)
            {
                if (NewConfig.SameAll)
                {
                    TShock.Utils.Broadcast(
                        $"All players' classes are set to {ClassColors[Config.PrimaryClass.ToUpper()]}{ClassColors[Config.SecondaryClass.ToUpper()]}!",
                        Color.Cyan
                    );
                }
                else
                {
                    TShock.Utils.Broadcast("Players are now in their own classes!", Color.Cyan);
                }
            }
            if (
                NewConfig.Enabled
                && NewConfig.SameAll
                && (
                    Config.PrimaryClass != NewConfig.PrimaryClass
                    || Config.SecondaryClass != NewConfig.SecondaryClass
                )
            )
            {
                TShock.Utils.Broadcast(
                    $"All players' classes are changed to {ClassColors[NewConfig.PrimaryClass]}{ClassColors[NewConfig.SecondaryClass]}!",
                    Color.Cyan
                );
            }
            Config = NewConfig;
        }

        public static Config SanitizeConfig(Config config)
        {
            config.PrimaryClass = Classes.Contains(config.PrimaryClass.ToUpper())
                ? config.PrimaryClass.ToUpper()
                : "WARRIOR";
            config.SecondaryClass = Classes.Contains(config.SecondaryClass.ToUpper())
                ? config.SecondaryClass.ToUpper()
                : "NONE";
            return config;
        }
        #endregion

        private readonly Dictionary<string, List<string>> PlayerClasses = new();

        #region  Player join
        private void OnPlayerLogin(PlayerPostLoginEventArgs args)
        {
            TSPlayer player = args.Player;
            if (player == null)
                return;

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
                string Primary = result.Reader.Get<string>("PrimaryClass");
                string Secondary = result.Reader.Get<string>("SecondaryClass");
                PlayerClasses[player.Account.Name] = new List<string>() { Primary, Secondary };
            }
            else
            {
                if (player.Group.HasPermission("tshock.godmode"))
                {
                    TShock.DB.Query(
                        @"
                        INSERT INTO PlayerClass (World, Username, PrimaryClass, SecondaryClass)
                        VALUES (@0, @1, 'SUPREME', 'SUPREME')
                        ",
                        Main.worldID,
                        player.Account.Name
                    );
                    PlayerClasses[player.Account.Name] = new List<string>()
                    {
                        "SUPREME",
                        "SUPREME",
                    };
                    return;
                }
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
            if (player == null)
                return;
            if (!Config.Enabled)
            {
                player.SendInfoMessage("Currently disabled.");
                return;
            }
            if (!player.RealPlayer)
            // For the Console
            {
                List<string> strings = new() { "Player Classes:" };
                foreach (string name in PlayerClasses.Keys)
                {
                    string isOnline =
                        TShock.Players.FirstOrDefault(player => player?.Account?.Name == name)?.Name
                        ?? "";
                    strings.Add(
                        $"{name}{(isOnline != "" ? $"({isOnline})" : "")} - [{PlayerClasses[name][0]}]{(PlayerClasses[name][0] == "SUPREME" ? "" : $"[{PlayerClasses[name][1]}]")}"
                    );
                }
                if (strings.Count == 1)
                    strings = new List<string>() { "No recent logins." };
                if (Config.SameAll)
                    strings.Add(
                        $"---\nConfig.SameAll is True\nClasses: [{Config.PrimaryClass}][{Config.SecondaryClass}]"
                    );
                player.SendInfoMessage(string.Join("\n", strings));
                return;
            }
            if (!player.Active)
                return;
            if (Config.SameAll)
            {
                player.SendMessage(
                    $"All players are forced to be {ClassColors[Config.PrimaryClass]}{ClassColors[Config.SecondaryClass]}!",
                    Color.Cyan
                );
                if (PlayerClasses[player.Account.Name].Contains("SUPREME"))
                    player.SendMessage(
                        $"You are a {ClassColors["SUPREME"]}, so you are exempt from this.",
                        Color.Cyan
                    );
                return;
            }
            if (!player.IsLoggedIn)
            {
                player.SendErrorMessage("You must log in first!");
                return;
            }

            if (
                args.Parameters.Count == 0
                || (
                    args.Parameters.Count >= 1
                    && !new List<string>() { "all", "get" }.Contains(args.Parameters[0])
                )
            )
            // Send Instructions
            {
                player.SendMessage(string.Join("\n", CommandInstructions), Color.Cyan);
                return;
            }
            if (args.Parameters[0].ToLower() == "all")
            // Send all playerclasses
            {
                List<string> strings = new() { "-- Player Classes --" };
                foreach (string name in PlayerClasses.Keys)
                {
                    string isOnline =
                        TShock.Players.FirstOrDefault(player => player?.Account?.Name == name)?.Name
                        ?? "";
                    strings.Add(
                        $"{name}{(isOnline != "" ? $"({isOnline})" : "")} - {ClassColors[PlayerClasses[name][0]]}{(PlayerClasses[name][0] == "SUPREME" ? "" : $"{ClassColors[PlayerClasses[name][1]]}")}"
                    );
                }
                player.SendMessage(string.Join("\n", strings), Color.Cyan);
                return;
            }
            if (args.Parameters.Count <= 1)
            // Send example usage
            {
                player.SendMessage(
                    "Example usage:\n/class get warrior\n/class get ranger\n/class get mage\n/class get summoner",
                    Color.Cyan
                );
                return;
            }
            if (!Classes.Contains(args.Parameters[1].ToUpper()))
            // Send valid classes
            {
                player.SendErrorMessage(
                    $"Invalid input '{args.Parameters[1]}'\nAvailable classes:\n- warrior\n- ranger\n- mage\n- summoner"
                );
                return;
            }

            string classwanted = args.Parameters[1].ToUpper();
            if (classwanted == "SUPREME")
            {
                player.SendErrorMessage($"This class is unavailable to you.");
                return;
            }
            bool hasPrimary = true;
            bool hasSecondary = true;

            using (
                QueryResult result = TShock.DB.QueryReader(
                    @"
                    SELECT * FROM PlayerClass
                    WHERE World=@0 AND Username=@1
                    ",
                    Main.worldID,
                    player.Account.Name
                )
            )
            {
                if (result.Read())
                {
                    hasPrimary = result.Reader.Get<string>("PrimaryClass") != "NONE";
                    hasSecondary = result.Reader.Get<string>("SecondaryClass") != "NONE";
                }
                else
                {
                    player.SendErrorMessage("Unsuccessful. Something went wrong.");
                    return;
                }
            }

            if (!hasPrimary)
            {
                TShock.DB.Query(
                    @"
                    UPDATE PlayerClass
                    SET PrimaryClass=@0
                    WHERE World=@1 AND Username=@2
                    ",
                    classwanted,
                    Main.worldID,
                    player.Account.Name
                );
                PlayerClasses[player.Account.Name][0] = classwanted;
                player.SendSuccessMessage($"Successfully set Primary class to {classwanted}!");
                GiveStarterItems(player, classwanted);
                return;
            }
            if (!hasSecondary)
            {
                if (classwanted == PlayerClasses[player.Account.Name][0])
                {
                    player.SendErrorMessage($"{classwanted} is already your primary class!");
                    return;
                }
                TShock.DB.Query(
                    @"
                    UPDATE PlayerClass
                    SET SecondaryClass=@0
                    WHERE World=@1 AND Username=@2
                    ",
                    classwanted,
                    Main.worldID,
                    player.Account.Name
                );
                PlayerClasses[player.Account.Name][1] = classwanted;
                player.SendSuccessMessage($"Successfully set Secondary class to {classwanted}!");
                GiveStarterItems(player, classwanted);
                return;
            }
            player.SendErrorMessage("You already have 2 classes.");
        }

        #endregion

        #region Restrict Actions
        private readonly Dictionary<string, DateTime> LastMessageSent = new();
        static readonly short[] MinionProjectiles =
        {
            ProjectileID.AbigailCounter,
            ProjectileID.AbigailMinion,
            ProjectileID.BabyBird,
            ProjectileID.FlinxMinion,
            ProjectileID.BabySlime,
            ProjectileID.VampireFrog,
            ProjectileID.Hornet,
            ProjectileID.FlyingImp,
            ProjectileID.VenomSpider,
            ProjectileID.JumperSpider,
            ProjectileID.DangerousSpider,
            ProjectileID.BatOfLight,
            ProjectileID.OneEyedPirate,
            ProjectileID.SoulscourgePirate,
            ProjectileID.PirateCaptain,
            ProjectileID.Smolstar,
            ProjectileID.Retanimini,
            ProjectileID.Spazmamini,
            ProjectileID.Pygmy,
            ProjectileID.Pygmy2,
            ProjectileID.Pygmy3,
            ProjectileID.Pygmy4,
            ProjectileID.StormTigerGem,
            ProjectileID.StormTigerTier1,
            ProjectileID.StormTigerTier2,
            ProjectileID.StormTigerTier3,
            ProjectileID.DeadlySphere,
            ProjectileID.Raven,
            ProjectileID.UFOMinion,
            ProjectileID.Tempest,
            ProjectileID.StardustDragon1,
            ProjectileID.StardustDragon2,
            ProjectileID.StardustDragon3,
            ProjectileID.StardustDragon4,
            ProjectileID.StardustCellMinion,
            ProjectileID.EmpressBlade,
        };

        private void OnGetData(GetDataEventArgs args)
        {
            if (!Config.Enabled)
                return;
            TSPlayer player = TShock.Players[args.Msg.whoAmI];
            if (player == null || !player.Active || !player.IsLoggedIn)
                return;

            switch (args.MsgID)
            {
                // Prevent minion summon
                case PacketTypes.ProjectileNew:
                    var playerClasses = Config.SameAll
                        ? new List<string> { Config.PrimaryClass, Config.SecondaryClass }
                        : PlayerClasses[player.Account.Name];

                    if (!playerClasses.Contains("SUMMONER"))
                    {
                        PreventMinion(args, player);
                    }
                    break;

                case PacketTypes.NpcStrike:
                    if (!PlayerClasses[player.Account.Name].Contains("SUPREME"))
                        OnNpcStrike(args, player);
                    break;
            }
        }

        private void PreventMinion(GetDataEventArgs args, TSPlayer player)
        {
            if (PlayerClasses[player.Account.Name].Contains("SUPREME"))
                return;

            using BinaryReader reader = new(
                new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)
            );

            var projectileId = reader.ReadInt16();
            _ = reader.ReadSingle();
            _ = reader.ReadSingle();
            _ = reader.ReadSingle();
            _ = reader.ReadSingle();
            var ownerId = reader.ReadByte();
            var type = reader.ReadInt16();

            if (MinionProjectiles.Contains(type))
            {
                player.SendData(PacketTypes.ProjectileDestroy, "", projectileId, ownerId);
                SendMessage(
                    player,
                    "You have to be a SUMMONER to do that! Type '/class' for more info.",
                    Color.Red
                );
                args.Handled = true;
            }
        }

        private void OnNpcStrike(GetDataEventArgs args, TSPlayer player)
        {
            // Return if just using non weapon tools
            List<string> classes = Config.SameAll
                ? new List<string>() { Config.PrimaryClass, Config.SecondaryClass }
                : PlayerClasses[player.Account.Name];
            Item selecteditem = player.SelectedItem;
            if (
                selecteditem.pick > 0
                || selecteditem.axe > 0
                || selecteditem.hammer > 0
                || selecteditem.damage <= 0
            )
                return;

            // Whether to punish the player if wrong weapon is used
            bool Punish = false;

            if (
                (selecteditem.melee && !classes.Contains("WARRIOR"))
                || (selecteditem.ranged && !classes.Contains("RANGER"))
                || (selecteditem.magic && !classes.Contains("MAGE"))
                || (selecteditem.summon && !classes.Contains("SUMMONER"))
            )
                Punish = true;

            if (Punish)
            {
                args.Handled = true;
                player.SetBuff(149, Config.PunishDuration * 60);

                if (!Config.SameAll && classes[0] == "NONE")
                {
                    SendMessage(
                        player,
                        "You haven't chosen a primary class yet! Type '/class' for more info.",
                        Color.Red
                    );
                    return;
                }
                if (!Config.SameAll && classes[1] == "NONE")
                {
                    SendMessage(
                        player,
                        $"You haven't chosen a secondary class yet! Type '/class get' for more info.",
                        Color.Red
                    );
                    return;
                }

                SendMessage(
                    player,
                    $"You can't use this weapon as {ClassColors[classes[0]]}{ClassColors[classes[1]]}",
                    Color.Red
                );
            }
        }

        #endregion
        private void SendMessage(TSPlayer player, string message, Color color)
        {
            if (
                !LastMessageSent.ContainsKey(player.Name)
                || (DateTime.Now - LastMessageSent[player.Name]).Seconds
                    >= Config.ErrorMessageInterval
            )
            {
                LastMessageSent[player.Name] = DateTime.Now;
                player.SendMessage(message, color);
            }
        }

        private static void GiveStarterItems(TSPlayer player, string classwanted)
        {
            try
            {
                List<Dictionary<string, int>> ClassStarters = Config.StartingWeapons[classwanted];
                foreach (Dictionary<string, int> item in ClassStarters)
                {
                    int netID = item["netID"];
                    int prefixID = item["prefixID"];
                    int stack = item["stack"];
                    player.GiveItem(netID, stack, prefixID);
                }
                player.SendMessage(
                    $"The {classwanted} spirits have blessed you with {string.Join("", ClassStarters.Select(item => $"[i/{(item["stack"] > 1 ? "s" + item["stack"] : "p" + item["prefixID"])}:{item["netID"]}]"))}!",
                    Color.Cyan
                );
            }
            catch (Exception ex)
            {
                player.SendErrorMessage("Something went wrong.\nFailed to give starting items.");
                TShock.Log.ConsoleError(ex.Message);
            }
        }
    }
}
