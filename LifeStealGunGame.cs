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

    private readonly List<string> MapRotation = new()
    {
        "Azagor",
        "Valley",
        "River",
        "Lonovo",
        "Basra",
        "Namak",
        "Wineparadise"
    };

    public string welcomeMessage = String.Empty;
    public LifeStealGunGameConfiguration LifeStealGunGameConfiguration { get; set; } = new();
    private readonly Dictionary<ulong, LifeStealGunGamePlayer> players = new();

    private LifeStealGunGamePlayer getPlayer(RunnerPlayer player)
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

        Server.GamemodeRotation.AddToRotation("TDM");
        Server.ServerSettings.PlayerCollision = true;
        Server.ServerSettings.FriendlyFireEnabled = true;
        Server.ServerSettings.CanVoteDay = true;
        Server.ServerSettings.CanVoteNight = false;
        Server.ServerSettings.FriendlyFireEnabled = true;
        Server.ServerSettings.TeamlessMode = true;

        welcomeMessage = new StringBuilder()
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("MediumVioletRed")}Welcome to Life Steal Gun Game{RichText.Color()}{RichText.NewLine()}")
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("White")}You have to kill other players to get a better weapon and to replenish your health.{RichText.Color()}{RichText.NewLine()}")
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("LimeGreen")}There are currently a total of {LifeStealGunGameConfiguration.LoadoutList.Count} levels.{RichText.Color()}{RichText.NewLine()}")
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.Sprite("Special")}{RichText.FromColorName("White")}Made by {RichText.FromColorName("LightCoral")}@DasIschBims{RichText.Color()}{RichText.Sprite("Special")}{RichText.NewLine()}")
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("Gold")}https://github.com/DasIschBims/BattleBitLifeSteal{RichText.Color()}{RichText.NewLine()}")
            .ToString();

        return Task.CompletedTask;
    }

    public override Task OnPlayerJoiningToServer(ulong steamId, PlayerJoiningArguments args)
    {
        var stats = args.Stats;

        stats.Progress.Rank = 200;
        stats.Progress.Prestige = 10;

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

        return Task.CompletedTask;
    }

    public override Task<bool> OnPlayerRequestingToChangeRole(RunnerPlayer player, GameRole requestedRole)
    {
        if (requestedRole != GameRole.Assault)
        {
            player.Message("You can only be Assault!", 5);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
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

    public override async Task OnTick()
    {
        Task.Run(async () =>
        {
            switch (Server.RoundSettings.State)
            {
                case GameState.Playing:
                {
                    Server.RoundSettings.SecondsLeft = 69420;
                    break;
                }
                case GameState.WaitingForPlayers:
                {
                    Server.ForceStartGame();
                    break;
                }
            }

            var top5 = players.Values.OrderByDescending(x => x.Kills).Take(5).ToList();

            var infoMessage = new StringBuilder();
            infoMessage.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("MediumVioletRed")} Life Steal Gun Game {RichText.FromColorName("LightSkyBlue")}by @DasIschBims{RichText.Color()}{RichText.NewLine()}");
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
                playerStatsMessage.AppendLine(
                    $"{RichText.Bold(true)}{RichText.FromColorName("LightGoldenrodYellow")}{RichText.Bold(true)}----------------------------------------------");
                playerStatsMessage.AppendLine(
                    $"{RichText.Bold(true)}{RichText.FromColorName("Gold")} Your Stats {RichText.FromColorName("White")}");
                playerStatsMessage.AppendLine(
                    $"{RichText.Bold(true)}{RichText.FromColorName("LawnGreen")} Kills: {RichText.FromColorName("White")}{getPlayer(player).Kills} {RichText.FromColorName("Red")}Deaths: {RichText.FromColorName("White")}{getPlayer(player).Deaths}");
                playerStatsMessage.AppendLine(
                    $"{RichText.Bold(true)}{RichText.FromColorName("Blue")} K/D: {RichText.FromColorName("White")}{getPlayer(player).Kd}");
                playerStatsMessage.AppendLine(
                    $"{RichText.Bold(true)}{RichText.FromColorName("LightGoldenrodYellow")}{RichText.Bold(true)}----------------------------------------------");

                if (player.IsAlive)
                {
                    player.Message(infoMessage.ToString() + leaderboardMessage.ToString() +
                                   playerStatsMessage.ToString());
                }
                else if (player.HP < 0 || getPlayer(player).Deaths == 0)
                {
                    player.Message(welcomeMessage);
                }
            }

            await Task.Delay(1000);
        });

        await Task.CompletedTask;
    }

    public Loadout GetNewWeapon(RunnerPlayer player)
    {
        if (getPlayer(player).Kills >= LifeStealGunGameConfiguration.LoadoutList.Count)
        {
            Server.SayToAllChat(
                $"{RichText.FromColorName("Gold")}{player.Name} won the game!");
            Server.AnnounceLong(
                $"{RichText.Sprite("Special")}{RichText.FromColorName("Black")}{player.Name} won the game!{RichText.Sprite("Special")}");
            Server.ForceEndGame();
            return default;
        }

        var loadout = LifeStealGunGameConfiguration.LoadoutList[getPlayer(player).Kills];

        return loadout;
    }

    public void UpdateLoadout(RunnerPlayer player, Loadout loadout)
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
            player.SetPrimaryWeapon(
                new WeaponItem() { ToolName = primaryWeapon, MainSightName = loadout.PrimaryWeaponSight },
                primaryExtraMagazines, true);

        if (secondaryWeapon != null)
            player.SetSecondaryWeapon(
                new WeaponItem() { ToolName = secondaryWeapon, MainSightName = loadout.SecondaryWeaponSight },
                secondaryExtraMagazines, true);

        if (heavyGadgetName != null)
            player.SetHeavyGadget(heavyGadgetName, heavyGadgetExtra, true);

        if (lightGadgetName != null)
            player.SetLightGadget(lightGadgetName, lightGadgetExtra, true);
    }

    public override Task OnPlayerSpawned(RunnerPlayer player)
    {
        // Disabled due to issues
        player.Modifications.JumpHeightMultiplier = 1.25f;
        player.Modifications.RunningSpeedMultiplier = 1.5f;

        player.Modifications.FallDamageMultiplier = 0f;
        player.Modifications.CanSpectate = false;
        player.Modifications.ReloadSpeedMultiplier = 1.5f;
        player.Modifications.GiveDamageMultiplier = 1f;
        player.Modifications.RespawnTime = 0;
        player.Modifications.DownTimeGiveUpTime = 0;
        player.Modifications.MinimumDamageToStartBleeding = 100f;
        player.Modifications.MinimumHpToStartBleeding = 0f;
        player.Modifications.HitMarkersEnabled = false;
        player.Modifications.KillFeed = true;
        player.Modifications.AirStrafe = true;
        player.Modifications.CanSuicide = false;
        player.Modifications.StaminaEnabled = false;
        player.Modifications.PointLogHudEnabled = false;
        player.Modifications.SpawningRule = SpawningRule.None;
        player.Modifications.FriendlyHUDEnabled = false;

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
            getPlayer(args.Victim).Deaths++;
        }
        else
        {
            Task.Delay(200);
            args.Victim.Kill();
            args.Killer.SetHP(100);

            getPlayer(args.Killer).Kills++;
            getPlayer(args.Victim).Deaths++;

            var newLoadout = GetNewWeapon(args.Killer);

            UpdateLoadout(args.Killer, newLoadout);
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
    public readonly List<Loadout> LoadoutList = new()
    {
        new Loadout()
        {
            PrimaryWeapon = Weapons.M4A1.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.AK74.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.KrissVector.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.MP5.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.P90.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.AsVal.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.Groza.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.SCARH.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.SVD.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.MK14EBR.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.M110.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.MK20.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.AK15.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.AUGA3.Name,
            PrimaryWeaponSight = Attachments.RedDot.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.MSR.Name,
            PrimaryWeaponSight = Attachments.TRI4X32.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.Rem700.Name,
            PrimaryWeaponSight = Attachments.TRI4X32.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.M200.Name,
            PrimaryWeaponSight = Attachments.TRI4X32.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            PrimaryWeapon = Weapons.SV98.Name,
            PrimaryWeaponSight = Attachments.TRI4X32.Name,
            PrimaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            SecondaryWeapon = Weapons.DesertEagle.Name,
            SecondaryWeaponSight = Attachments.RedDot.Name,
            SecondaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            SecondaryWeapon = Weapons.Rsh12.Name,
            SecondaryWeaponSight = Attachments.RedDot.Name,
            SecondaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            SecondaryWeapon = Weapons.Glock18.Name,
            SecondaryWeaponSight = Attachments.RedDot.Name,
            SecondaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            SecondaryWeapon = Weapons.USP.Name,
            SecondaryWeaponSight = Attachments.RedDot.Name,
            SecondaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            SecondaryWeapon = Weapons.Unica.Name,
            SecondaryWeaponSight = Attachments.RedDot.Name,
            SecondaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            SecondaryWeapon = Weapons.MP443.Name,
            SecondaryWeaponSight = Attachments.RedDot.Name,
            SecondaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            SecondaryWeapon = Weapons.USP.Name,
            SecondaryWeaponSight = Attachments.RedDot.Name,
            SecondaryExtraMagazines = byte.MaxValue
        },
        new Loadout()
        {
            HeavyGadgetName = Gadgets.Rpg7HeatExplosive.Name,
            HeavyGadgetExtra = byte.MaxValue,
        },
        new Loadout()
        {
            HeavyGadgetName = Gadgets.SledgeHammerSkinC.Name,
        },
        new Loadout()
        {
            HeavyGadgetName = Gadgets.PickaxeIronPickaxe.Name,
        },
        new Loadout()
        {
            LightGadgetName = Gadgets.SuicideC4.Name,
        }
    };
}

public struct Loadout
{
    public string? PrimaryWeapon { get; set; } = default;
    public string? PrimaryWeaponSight { get; set; } = default;
    public byte PrimaryExtraMagazines { get; set; } = 0;
    public string? SecondaryWeapon { get; set; } = default;
    public string? SecondaryWeaponSight { get; set; } = default;
    public byte SecondaryExtraMagazines { get; set; } = 0;
    public string? HeavyGadgetName { get; set; } = default;
    public byte HeavyGadgetExtra { get; set; } = 0;
    public string? LightGadgetName { get; set; } = default;
    public byte LightGadgetExtra { get; set; } = 0;

    public Loadout(string? primaryWeapon, byte primaryExtraMagazines, string secondaryWeapon,
        byte secondaryExtraMagazines, string heavyGadgetName, byte heavyGadgetExtra, string lightGadgetName,
        byte lightGadgetExtra)
    {
        PrimaryWeapon = primaryWeapon;
        PrimaryExtraMagazines = primaryExtraMagazines;
        SecondaryWeapon = secondaryWeapon;
        SecondaryExtraMagazines = secondaryExtraMagazines;
        HeavyGadgetName = heavyGadgetName;
        HeavyGadgetExtra = heavyGadgetExtra;
        LightGadgetName = lightGadgetName;
        LightGadgetExtra = lightGadgetExtra;
    }
}