﻿using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Construction;
using Content.Shared._CM14.Xenos.Egg;
using Content.Shared._CM14.Xenos.Evolution;
using Content.Shared._CM14.Xenos.Hive;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._CM14.Xenos.Construction;

public sealed class XenoHiveCoreSystem : SharedXenoHiveCoreSystem
{
    [Dependency] private readonly XenoEvolutionSystem _evolution = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, GhostRoleSpawnerUsedEvent>(OnXenoSpawnerUsed);

        SubscribeLocalEvent<XenoHiveCoreRoleComponent, MapInitEvent>(OnCoreRoleMapInit);
        SubscribeLocalEvent<XenoHiveCoreRoleComponent, GhostRoleSpawnerUsedEvent>(OnCoreRoleSpawnerUsed);
    }

    private void OnXenoSpawnerUsed(Entity<XenoComponent> xeno, ref GhostRoleSpawnerUsedEvent args)
    {
        if (TryComp(args.Spawner, out HiveMemberComponent? member))
            _xeno.SetHive((xeno, xeno), member.Hive);

        if (TryComp(args.Spawner, out HiveCoreComponent? core))
            core.LiveLesserDrones.Add(xeno);
    }

    private void OnCoreRoleMapInit(Entity<XenoHiveCoreRoleComponent> ent, ref MapInitEvent args)
    {
        _ghostRole.UpdateAllEui();
    }

    private void OnCoreRoleSpawnerUsed(Entity<XenoHiveCoreRoleComponent> ent, ref GhostRoleSpawnerUsedEvent args)
    {
        ent.Comp.Core = args.Spawner;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // TODO CM14 lesser drone job bans
        // TODO CM14 30 second delay to grabbing the next lesser drone role
        // TODO CM14 hive specific
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<HiveCoreComponent, GhostRoleMobSpawnerComponent>();
        while (query.MoveNext(out var uid, out var core, out var spawner))
        {
            for (var i = core.LiveLesserDrones.Count - 1; i >= 0; i--)
            {
                var drone = core.LiveLesserDrones[i];
                if (TerminatingOrDeleted(drone) ||
                    !HasComp<XenoComponent>(drone) ||
                    _mobState.IsDead(drone))
                {
                    core.LiveLesserDrones.RemoveSwap(i);
                    core.CurrentLesserDrones = Math.Max(0, core.CurrentLesserDrones - 1);
                }
            }

            _ghostRole.SetCurrent((uid, spawner), core.LiveLesserDrones.Count);

            if (!_evolution.HasLiving<XenoComponent>(1) &&
                !_evolution.HasLiving<XenoEvolutionGranterComponent>(1))
            {
                _ghostRole.SetAvailable((uid, spawner), 0);
                continue;
            }

            var living = _evolution.GetLiving<XenoComponent>(x => x.Comp.CountedInSlots);
            var available = Math.Max(core.MinimumLesserDrones, living / core.XenosPerLesserDrone);
            core.MaxLesserDrones = available;

            if (time > core.NextLesserDroneAt)
            {
                var hasOvipositor = _evolution.HasLiving<XenoAttachedOvipositorComponent>(1);
                core.NextLesserDroneAt = time + (hasOvipositor ? core.NextLesserDroneOviCooldown : core.NextLesserDroneCooldown * 2);
                core.CurrentLesserDrones = Math.Min(core.MaxLesserDrones, core.CurrentLesserDrones + 1);
            }

            _ghostRole.SetAvailable((uid, spawner), core.CurrentLesserDrones);
        }
    }
}
