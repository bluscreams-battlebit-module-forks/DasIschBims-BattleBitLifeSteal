using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using BBRAPIModules;

namespace LifeStealGunGame;

public class LifeStealGunGame: BattleBitModule
{
    public LifeStealGunGameConfiguration LifeStealGunGameConfiguration { get; set; } = new();
    private readonly Dictionary<ulong, LifeStealGunGamePlayer> players = new();
    private LifeStealGunGamePlayer getPlayer(RunnerPlayer player)
    {
        if (!players.ContainsKey(player.SteamID))
            players.Add(player.SteamID, new LifeStealGunGamePlayer(player));
        
        return players[player.SteamID];
    }
    
    public override Task OnConnected()
    {
        Server.ServerSettings.PlayerCollision = true;
        Server.MapRotation.AddToRotation("");
        Server.GamemodeRotation.AddToRotation("TDM");
        Server.ServerSettings.FriendlyFireEnabled = true;
        
        return Task.CompletedTask; 
    }

    public override Task OnPlayerJoiningToServer(ulong steamId, PlayerJoiningArguments args)
    {
        var stats = args.Stats;
        
        stats.Progress.Rank = 200;
        stats.Progress.Prestige = 10;
        
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
            PrimaryWeapon = new WeaponItem { Tool = Weapons.KrissVector, MainSight = Attachments.RedDot, TopSight = null, Barrel = null, CantedSight = null, BoltAction = null, SideRail = null, UnderRail = null}
        },
        new PlayerLoadout()
        {
            HeavyGadget = Gadgets.PickaxeIronPickaxe,
        }
    };
}