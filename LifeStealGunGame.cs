using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BattleBitBaseModules;
using BBRAPIModules;

namespace LifeStealGunGame;

[RequireModule(typeof(RichText))]
public class LifeStealGunGame : BattleBitModule
{
    [ModuleReference] public dynamic? RichText { get; set; }
    [ModuleReference] public dynamic? LoadingScreenText { get; set; }
    [ModuleReference] public dynamic? ProfanityFilter { get; set; }
    [ModuleReference] public dynamic? DiscordWebhook { get; set; }
    [ModuleReference] public dynamic? Announcements { get; set; }

    private readonly List<string> MapRotation = new()
    {
        "Azagor",
        "Valley",
        "River",
        "Lonovo",
        "Basra",
        "Namak",
        "Frugis",
        "Dustydew",
        "Construction",
        "Wineparadise",
        "Old_Multuislands",
        "Old_Namak"
    };

    private string welcomeMessage = String.Empty;
    private LifeStealGunGameConfiguration LifeStealGunGameConfiguration { get; set; } = new();
    private readonly Dictionary<ulong, LifeStealGunGamePlayer> players = new();

    private LifeStealGunGamePlayer GetPlayer(RunnerPlayer player)
    {
        if (!players.ContainsKey(player.SteamID))
            players.Add(player.SteamID, new LifeStealGunGamePlayer(player));

        return players[player.SteamID];
    }

    public override Task<bool> OnPlayerTypedMessage(RunnerPlayer player, ChatChannel channel, string msg)
    {
        Console.WriteLine($"[{channel}] ({player.Name} | {player.SteamID}): {msg}");
        return Task.FromResult(true);
    }

    public override Task OnConnected()
    {
        foreach (var map in MapRotation)
        {
            Server.MapRotation.AddToRotation(map);
        }

        Server.ExecuteCommand("set fps 128");

        Server.GamemodeRotation.AddToRotation("TDM");
        Server.ServerSettings.PlayerCollision = true;
        Server.ServerSettings.FriendlyFireEnabled = true;
        Server.ServerSettings.CanVoteDay = true;
        Server.ServerSettings.CanVoteNight = false;
        Server.ServerSettings.FriendlyFireEnabled = true;
        Server.ServerSettings.TeamlessMode = false;
        Server.ServerSettings.UnlockAllAttachments = true;

        welcomeMessage = new StringBuilder()
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("MediumVioletRed")}Welcome to Life Steal Gun Game{RichText.Color()}{RichText.NewLine()}")
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("White")}You have to kill other players to get a better weapon and to replenish your health.{RichText.Color()}{RichText.NewLine()}")
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("LimeGreen")}There are currently a total of {LifeStealGunGameConfiguration.WeaponList.Count + 4} levels.{RichText.Color()}{RichText.NewLine()}")
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.Sprite("Special")}{RichText.FromColorName("White")}Made by {RichText.FromColorName("LightCoral")}@DasIschBims{RichText.Color()}{RichText.Sprite("Special")}{RichText.NewLine()}")
            .AppendLine(
                $"{RichText.Bold(true)}GitHub: {RichText.FromColorName("Gold")}https://github.com/DasIschBims/BattleBitLifeSteal{RichText.Color()}{RichText.NewLine()}")
            .ToString();

        ShuffleList(LifeStealGunGameConfiguration.WeaponList);
        GenerateLoadouts();

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("Current Loadout Rotation:");
        Console.ForegroundColor = ConsoleColor.Yellow;
        var loadoutIndex = 1;
        foreach (var loadout in LifeStealGunGameConfiguration.LoadoutList)
        {
            Console.WriteLine(loadoutIndex + ". Weapon " + loadout.PrimaryWeapon + " with Sight " +
                              loadout.PrimaryWeaponSight);
            loadoutIndex++;
        }

        Console.ResetColor();

        return Task.CompletedTask;
    }

    public override Task OnPlayerConnected(RunnerPlayer player)
    {
        Server.UILogOnServer($"{player.Name} joined the server!", 5);

        return Task.CompletedTask;
    }

    public override Task OnPlayerDisconnected(RunnerPlayer player)
    {
        players.Remove(player.SteamID);

        Server.UILogOnServer($"{player.Name} left the server!", 5);

        return Task.CompletedTask;
    }

    public override Task OnSessionChanged(long oldSessionID, long newSessionID)
    {
        if (players.Count > 0)
            players.Clear();

        Server.ExecuteCommand("set fps 128");

        return Task.CompletedTask;
    }

    public override Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole)
    {
        if (requestedRole == GameRole.Assault) return Task.FromResult(true);
        player.Message("You can only be Assault!", 5);
        return Task.FromResult(false);
    }

    public override Task OnPlayerJoinedSquad(RunnerPlayer player, Squad<RunnerPlayer> squad)
    {
        player.Message("You can't join a squad", 5);
        player.KickFromSquad();
        return Task.CompletedTask;
    }

    public override Task<bool> OnPlayerRequestingToChangeTeam(RunnerPlayer player, Team requestedTeam)
    {
        return Task.FromResult(false);
    }

    public override Task OnTick()
    {
        Task.Run(async () =>
        {
            switch (Server.RoundSettings.State)
            {
                case GameState.Playing:
                {
                    Server.RoundSettings.SecondsLeft = 69420;
                    Server.RoundSettings.TeamATickets = 69420;
                    Server.RoundSettings.TeamBTickets = 69420;
                    break;
                }
                case GameState.WaitingForPlayers:
                {
                    Server.ForceStartGame();
                    break;
                }
            }

            await Task.Delay(1000);
        });

        return Task.CompletedTask;
    }

    private void UpdateLeaderboard(IReadOnlyList<LifeStealGunGamePlayer> top5)
    {
        var infoMessage = new StringBuilder();
        infoMessage.AppendLine(
            $"{RichText.Bold(true)}{RichText.FromColorName("LawnGreen")}{RichText.Sprite("Veteran")} Top 5 Players {RichText.Sprite("Veteran")}");
        infoMessage.AppendLine(
            $"{RichText.Bold(true)}{RichText.FromColorName("LightGoldenrodYellow")}{RichText.Bold(true)}----------------------------------------------");

        var leaderboardMessage = new StringBuilder();
        for (var i = 0; i < top5.Count; i++)
        {
            var topPlayer = top5[i];
            leaderboardMessage.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("Gold")} {i + 1}. {RichText.FromColorName("White")}{topPlayer.Player.Name} {RichText.FromColorName("Gold")}Kills: {RichText.FromColorName("White")}{topPlayer.Kills} {RichText.FromColorName("Gold")}K/D: {RichText.FromColorName("White")}{topPlayer.Kd}");
        }

        foreach (var player in Server.AllPlayers)
        {
            var playerStatsMessage = new StringBuilder();
            var nextWeapon = LifeStealGunGameConfiguration.LoadoutList[GetPlayer(player).Kills + 1].PrimaryWeapon;

            if (GetPlayer(player).Kills + 1 < LifeStealGunGameConfiguration.LoadoutList.Count)
            {
                nextWeapon = LifeStealGunGameConfiguration.LoadoutList[GetPlayer(player).Kills + 1].PrimaryWeapon;
                if (nextWeapon == null)
                {
                    nextWeapon = "Special Weapon";
                }
            }
            else
            {
                nextWeapon = "Finished!";
            }

            playerStatsMessage.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("LightGoldenrodYellow")}{RichText.Bold(true)}----------------------------------------------");
            playerStatsMessage.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("Gold")}Next Weapon: {nextWeapon}{RichText.Color()}");
            playerStatsMessage.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("Gold")} Your Stats {RichText.FromColorName("White")}");
            playerStatsMessage.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("LawnGreen")} Kills: {RichText.FromColorName("White")}{GetPlayer(player).Kills} {RichText.FromColorName("Red")}Deaths: {RichText.FromColorName("White")}{GetPlayer(player).Deaths}");
            playerStatsMessage.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("Blue")} K/D: {RichText.FromColorName("White")}{GetPlayer(player).Kd}");

            if (player.IsAlive)
            {
                player.Message($"{infoMessage}{leaderboardMessage}{playerStatsMessage}");
            }
            else if ((player.HP <= 0 || GetPlayer(player).Deaths == 0) && player.IsAlive == false)
            {
                player.Message(welcomeMessage);
            }
        }
    }

    private static void UpdateLoadout(RunnerPlayer player, Loadout loadout)
    {
        var primaryWeapon = loadout.PrimaryWeapon == null
            ? default
            : loadout.PrimaryWeapon;
        var primaryExtraMagazines = loadout.PrimaryExtraMagazines == 0
            ? default
            : loadout.PrimaryExtraMagazines;
        var secondaryWeapon = loadout.SecondaryWeapon == null
            ? default
            : loadout.SecondaryWeapon;
        var secondaryExtraMagazines = loadout.SecondaryExtraMagazines == 0
            ? default
            : loadout.SecondaryExtraMagazines;
        var heavyGadgetName = loadout.HeavyGadgetName == null
            ? default
            : loadout.HeavyGadgetName;
        var heavyGadgetExtra = loadout.HeavyGadgetExtra == 0
            ? default
            : loadout.HeavyGadgetExtra;
        var lightGadgetName = loadout.LightGadgetName == null
            ? default
            : loadout.LightGadgetName;
        var lightGadgetExtra = loadout.LightGadgetExtra == 0
            ? default
            : loadout.LightGadgetExtra;

        if (primaryWeapon != null)
        {
            var cantedSight = loadout.PrimaryWeaponCantedSight == null
                ? default
                : new Attachment(loadout.PrimaryWeaponCantedSight, AttachmentType.CantedSight);
            player.SetPrimaryWeapon(
                new WeaponItem()
                {
                    ToolName = primaryWeapon, MainSightName = loadout.PrimaryWeaponSight,
                    BarrelName = loadout.PrimaryWeaponBarrel, UnderRailName = loadout.PrimaryWeaponUnderBarrel,
                    CantedSight = cantedSight
                },
                primaryExtraMagazines, true);
        }

        if (secondaryWeapon != null)
            player.SetSecondaryWeapon(
                new WeaponItem() { ToolName = secondaryWeapon, MainSightName = loadout.SecondaryWeaponSight },
                secondaryExtraMagazines, true);

        if (heavyGadgetName != null)
            player.SetHeavyGadget(heavyGadgetName, heavyGadgetExtra, true);

        if (lightGadgetName != null)
            player.SetLightGadget(lightGadgetName, lightGadgetExtra, true);
    }

    private static void ShuffleList<T>(IList<T> list)
    {
        var rng = new System.Random();
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    private void GenerateLoadouts()
    {
        var weapons = LifeStealGunGameConfiguration.WeaponList;
        var barrels = LifeStealGunGameConfiguration.BarrelList;
        var underBarrels = LifeStealGunGameConfiguration.UnderBarrelRailList;
        var sights = LifeStealGunGameConfiguration.SightList;
        var gadgets = LifeStealGunGameConfiguration.GadgetList;

        List<Loadout> loadouts = new();

        var random = new Random();

        foreach (var weapon in weapons)
        {
            var loadout = new Loadout();

            loadout.PrimaryWeapon = weapon.Name;
            loadout.PrimaryWeaponBarrel = random.Next(0, 100) < 69 ? null : GetRandomItem(barrels, random).Name;
            loadout.PrimaryWeaponUnderBarrel =
                random.Next(0, 100) < 69 ? null : GetRandomItem(underBarrels, random).Name;
            loadout.PrimaryWeaponSight = GetRandomItem(sights, random).Name;
            if (weapon.WeaponType == WeaponType.SniperRifle)
                loadout.PrimaryWeaponCantedSight = Attachments.Ironsight.Name;
            loadout.HeavyGadgetName = random.Next(0, 100) < 69 ? null : GetRandomItem(gadgets, random).Name;

            loadouts.Add(loadout);
        }

        foreach (var gadget in gadgets)
        {
            var gadgetLoadout = new Loadout();

            if (gadget.Name == "SuicideC4")
            {
                gadgetLoadout.LightGadgetName = gadget.Name;
            }
            else
            {
                gadgetLoadout.HeavyGadgetName = gadget.Name;
                gadgetLoadout.HeavyGadgetExtra = byte.MaxValue;
            }
            
            loadouts.Add(gadgetLoadout);
        }

        LifeStealGunGameConfiguration.LoadoutList = loadouts;
    }

    private static T GetRandomItem<T>(List<T> itemList, Random random)
    {
        if (itemList.Count > 0)
        {
            int randomIndex = random.Next(0, itemList.Count);
            return itemList[randomIndex];
        }

        return default;
    }

    private Loadout GetNewWeapon(RunnerPlayer player)
    {
        if (GetPlayer(player).Kills >= LifeStealGunGameConfiguration.LoadoutList.Count)
        {
            Server.SayToAllChat(
                $"{RichText.FromColorName("Gold")}{player.Name} won the game!");
            Server.AnnounceLong(
                $"{RichText.Sprite("Special")}{RichText.FromColorName("Black")}{player.Name} won the game!{RichText.Sprite("Special")}");

            var top3 = players.Values.OrderByDescending(x => x.Kills).Take(3).ToList();
            var topPlayerList = top3.Select(topPlayer =>
                new EndGamePlayer<RunnerPlayer>(topPlayer.Player, GetPlayer(topPlayer.Player).Kills)).ToList();

            Server.ForceEndGame(topPlayerList);
            return default;
        }

        var currentWeaponIndex = GetPlayer(player).Kills;
        var loadout = new Loadout()
        {
            PrimaryWeapon = LifeStealGunGameConfiguration.WeaponList[currentWeaponIndex].Name,
            PrimaryWeaponSight = GetRandomAttachment(LifeStealGunGameConfiguration.SightList),
            PrimaryWeaponBarrel = GetRandomAttachment(LifeStealGunGameConfiguration.BarrelList),
            PrimaryWeaponUnderBarrel = GetRandomAttachment(LifeStealGunGameConfiguration.UnderBarrelRailList),
            PrimaryExtraMagazines = 20,
        };

        return loadout;
    }

    private static string GetRandomAttachment(IReadOnlyList<Attachment> attachmentList)
    {
        if (attachmentList.Count <= 0) return default;

        var random = new Random();
        var randomIndex = random.Next(0, attachmentList.Count);
        return attachmentList[randomIndex].Name;
    }

    public override Task OnPlayerSpawned(RunnerPlayer player)
    {
        // Disabled due to issues
        player.Modifications.JumpHeightMultiplier = 1.25f;
        player.Modifications.RunningSpeedMultiplier = 1.5f;

        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.CanSpectate = true;
        player.Modifications.ReloadSpeedMultiplier = 1.5f;
        player.Modifications.GiveDamageMultiplier = 1f;
        player.Modifications.RespawnTime = 1;
        player.Modifications.DownTimeGiveUpTime = 5;
        player.Modifications.MinimumDamageToStartBleeding = 100f;
        player.Modifications.MinimumHpToStartBleeding = 0f;
        player.Modifications.HitMarkersEnabled = true;
        player.Modifications.KillFeed = true;
        player.Modifications.AirStrafe = true;
        player.Modifications.CanSuicide = true;
        player.Modifications.StaminaEnabled = false;
        player.Modifications.PointLogHudEnabled = false;
        player.Modifications.SpawningRule = SpawningRule.None;
        player.Modifications.FriendlyHUDEnabled = true;

        return Task.CompletedTask;
    }

    public override Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
    {
        var loadout = GetNewWeapon(player);
        UpdateLoadout(player, loadout);

        request.Loadout.FirstAid = default;
        request.Loadout.Throwable = default;

        return Task.FromResult<OnPlayerSpawnArguments?>(request);
    }

    public override Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<RunnerPlayer> args)
    {
        if (args.Killer == args.Victim)
        {
            args.Victim.Kill();
            GetPlayer(args.Victim).Deaths++;
        }
        else
        {
            // args.Victim.Kill();
            GetPlayer(args.Victim).Deaths++;
            GetPlayer(args.Killer).Kills++;
            args.Killer.SetHP(100);

            var newLoadout = GetNewWeapon(args.Killer);

            UpdateLoadout(args.Killer, newLoadout);

            List<LifeStealGunGamePlayer> top5 = players.Values.OrderByDescending(x => x.Kills).Take(5).ToList();

            UpdateLeaderboard(top5);
        }

        return Task.CompletedTask;
    }
}

public class LifeStealGunGamePlayer
{
    public RunnerPlayer Player { get; set; }

    public LifeStealGunGamePlayer(RunnerPlayer player)
    {
        Player = player;
    }

    public int Kills { get; set; }
    public int Deaths { get; set; }
    public float Kd => Deaths == 0 ? Kills : (float)Math.Round((float)Kills / Deaths, 2);
}

public class LifeStealGunGameConfiguration
{
    public readonly List<Attachment> SightList = new()
    {
        Attachments.Holographic,
        Attachments.RedDot,
        Attachments.Reflex,
        Attachments.Strikefire,
        Attachments.Kobra
    };

    public readonly List<Attachment> BarrelList = new()
    {
        Attachments.Basic,
        Attachments.Tactical,
        Attachments.SDN6762,
        Attachments.LongBarrel,
        Attachments.SuppressorShort
    };

    public readonly List<Attachment> UnderBarrelRailList = new()
    {
        Attachments.VerticalGrip,
        Attachments.B25URK,
        Attachments.StabilGrip,
        Attachments.FABDTFG,
        Attachments.AngledGrip
    };

    public List<Weapon> WeaponList = new()
    {
        Weapons.AK74,
        Weapons.M4A1,
        Weapons.G36C,
        Weapons.ACR,
        Weapons.SCARH,
        Weapons.AUGA3,
        Weapons.SG550,
        Weapons.HK419,
        Weapons.AsVal,
        Weapons.ScorpionEVO,
        Weapons.FAL,
        Weapons.HoneyBadger,
        Weapons.KrissVector,
        Weapons.PP2000,
        Weapons.MP5,
        Weapons.MP7,
        Weapons.PP19,
        Weapons.L96,
        Weapons.M110,
        Weapons.Ultimax100,
        Weapons.MG36,
        Weapons.Glock18,
        Weapons.USP,
        Weapons.DesertEagle,
        Weapons.M9,
    };

    public readonly List<Gadget> GadgetList = new()
    {
        Gadgets.Rpg7HeatExplosive,
        Gadgets.SledgeHammerSkinC,
        Gadgets.PickaxeIronPickaxe,
        Gadgets.SuicideC4
    };

    public List<Loadout> LoadoutList = new();
}

public struct Loadout
{
    public string? PrimaryWeapon { get; set; } = default;
    public string? PrimaryWeaponSight { get; set; } = default;
    public string? PrimaryWeaponCantedSight { get; set; } = default;
    public string? PrimaryWeaponBarrel { get; set; } = default;
    public string? PrimaryWeaponUnderBarrel { get; set; } = default;
    public byte PrimaryExtraMagazines { get; set; } = 0;
    public string? SecondaryWeapon { get; set; } = default;
    public string? SecondaryWeaponSight { get; set; } = default;
    public byte SecondaryExtraMagazines { get; set; } = 0;
    public string? HeavyGadgetName { get; set; } = default;
    public byte HeavyGadgetExtra { get; set; } = 0;
    public string? LightGadgetName { get; set; } = default;

    public byte LightGadgetExtra { get; set; } = 0;

    public Loadout()
    {
    }
}