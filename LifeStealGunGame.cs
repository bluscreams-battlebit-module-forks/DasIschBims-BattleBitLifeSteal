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

    private readonly List<string> MapRotation = new()
    {
        "Azagor",
        "Valley",
        "River",
        "Lonovo"
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
        Console.WriteLine($"[{channel}] {player.Name}: {msg}");
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

        welcomeMessage = new StringBuilder()
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("MediumVioletRed")}Welcome to Life Steal Gun Game{RichText.Color()}{RichText.NewLine()}")
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("White")}You have to kill other players to get a better weapon and to replenish your health.{RichText.Color()}{RichText.NewLine()}")
            .AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("Red")} There currently is a bug where you get an \"EMPTYGUN\" when you kill someone.{RichText.NewLine()}Just press \"1\" to get your normal weapon back.{RichText.Color()}")
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
        Server.SayToAllChat($"{RichText.FromColorName("MediumVioletRed")}{player.Name} just joined the server!");

        return Task.CompletedTask;
    }

    public override Task OnPlayerDisconnected(RunnerPlayer player)
    {
        players.Remove(player.SteamID);

        return Task.CompletedTask;
    }

    public override Task OnSessionChanged(long oldSessionID, long newSessionID)
    {
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

    public override Task OnTick()
    {
        var top5 = players.Values.OrderByDescending(x => x.Kills).Take(5).ToList();

        var message = new StringBuilder();
        // temporary warning
        message.AppendLine(
            $"{RichText.Bold(true)}{RichText.FromColorName("MediumVioletRed")} Life Steal Gun Game {RichText.FromColorName("LightSkyBlue")}by @DasIschBims{RichText.Color()}{RichText.NewLine()}");
        message.AppendLine(
            $"{RichText.Bold(true)}{RichText.FromColorName("LawnGreen")}{RichText.Sprite("Veteran")} Top 5 Players {RichText.Sprite("Veteran")}");
        message.AppendLine(
            $"{RichText.Bold(true)}{RichText.FromColorName("LightGoldenrodYellow")}{RichText.Bold(true)}----------------------------------------------");

        for (var i = 0; i < top5.Count; i++)
        {
            var topPlayer = top5[i];
            var kd = topPlayer.Deaths == 0 ? topPlayer.Kills : (float)topPlayer.Kills / (float)topPlayer.Deaths;
            message.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("Gold")} {i + 1}. {RichText.FromColorName("White")}{topPlayer.Player.Name} {RichText.FromColorName("Gold")}Kills: {RichText.FromColorName("White")}{topPlayer.Kills} {RichText.FromColorName("Gold")}K/D: {RichText.FromColorName("White")}{Math.Round(kd, 2)}");
        }

        foreach (var player in Server.AllPlayers)
        {
            message.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("LightGoldenrodYellow")}{RichText.Bold(true)}----------------------------------------------");
            message.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("Gold")} Your Stats {RichText.FromColorName("White")}");
            message.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("LawnGreen")} Kills: {RichText.FromColorName("White")}{getPlayer(player).Kills} {RichText.FromColorName("Red")}Deaths: {RichText.FromColorName("White")}{getPlayer(player).Deaths}");
            var kd = getPlayer(player).Deaths == 0
                ? getPlayer(player).Kills
                : (float)getPlayer(player).Kills / (float)getPlayer(player).Deaths;
            message.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("Blue")} K/D: {RichText.FromColorName("White")}{kd}");
            message.AppendLine(
                $"{RichText.Bold(true)}{RichText.FromColorName("LightGoldenrodYellow")}{RichText.Bold(true)}----------------------------------------------");

            if (player.IsAlive)
                player.Message(message.ToString());

            if (player.HP < 0 && getPlayer(player).Deaths == 0)
                player.Message(welcomeMessage);
        }

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

            await Task.Delay(1000);
        });

        return Task.CompletedTask;
    }

    public PlayerLoadout UpdateWeapon(RunnerPlayer player)
    {
        if (getPlayer(player).Kills >= LifeStealGunGameConfiguration.LoadoutList.Count)
        {
            Server.ForceEndGame();
            return LifeStealGunGameConfiguration.LoadoutList[0];
        }

        return LifeStealGunGameConfiguration.LoadoutList[getPlayer(player).Kills];
    }

    public override Task OnPlayerSpawned(RunnerPlayer player)
    {
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
        player.Modifications.CanSuicide = true;

        return Task.CompletedTask;
    }

    public override Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request)
    {
        request.Loadout = UpdateWeapon(player);

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

            var newLoadout = UpdateWeapon(args.Killer);
            args.Killer.SetPrimaryWeapon(newLoadout.PrimaryWeapon, newLoadout.PrimaryExtraMagazines);
            args.Killer.SetSecondaryWeapon(newLoadout.SecondaryWeapon, newLoadout.SecondaryExtraMagazines);
            if (newLoadout.HeavyGadgetName == null)
            {
                newLoadout.HeavyGadget = default;
            }
            else
            {
                args.Killer.SetHeavyGadget(newLoadout.HeavyGadget?.Name, newLoadout.HeavyGadgetExtra);
            }

            if (newLoadout.LightGadgetName == null)
            {
                newLoadout.LightGadget = default;
            }
            else
            {
                args.Killer.SetLightGadget(newLoadout.LightGadget?.Name, newLoadout.LightGadgetExtra);
            }
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

    public int Kills { get; set; } = 0;
    public int Deaths { get; set; } = 0;
}

public class LifeStealGunGameConfiguration
{
    public readonly List<PlayerLoadout> LoadoutList = new()
    {
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem { Tool = Weapons.KrissVector, MainSight = Attachments.RedDot },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.FAL, MainSight = Attachments.RedDot, TopSight = null, Barrel = null, CantedSight = null,
                BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.MP7, MainSight = Attachments.Holographic, TopSight = null, Barrel = null,
                CantedSight = null, BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.MP5, MainSight = Attachments.RedDot, TopSight = null, Barrel = null, CantedSight = null,
                BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.Groza, MainSight = Attachments.RedDot, TopSight = null, Barrel = null,
                CantedSight = null, BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.HK419, MainSight = Attachments.RedDot, TopSight = null, Barrel = null,
                CantedSight = null, BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.FAL, MainSight = Attachments.RedDot, TopSight = null, Barrel = null, CantedSight = null,
                BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.MP7, MainSight = Attachments.Holographic, TopSight = null, Barrel = null,
                CantedSight = null, BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.MP5, MainSight = Attachments.RedDot, TopSight = null, Barrel = null, CantedSight = null,
                BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.Groza, MainSight = Attachments.RedDot, TopSight = null, Barrel = null,
                CantedSight = null, BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.HK419, MainSight = Attachments.RedDot, TopSight = null, Barrel = null,
                CantedSight = null, BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.Ultimax100, MainSight = Attachments.Holographic, TopSight = null, Barrel = null,
                CantedSight = null, BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            PrimaryWeapon = new WeaponItem
            {
                Tool = Weapons.M4A1, MainSight = Attachments.RedDot, TopSight = null, Barrel = null, CantedSight = null,
                BoltAction = null, SideRail = null, UnderRail = null
            },
            PrimaryExtraMagazines = 20
        },
        new PlayerLoadout()
        {
            HeavyGadget = Gadgets.PickaxeIronPickaxe,
        }
    };
}