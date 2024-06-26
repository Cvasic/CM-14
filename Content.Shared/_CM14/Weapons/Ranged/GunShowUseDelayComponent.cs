﻿using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunShowUseDelayComponent : Component
{
    [DataField, AutoNetworkedField]
    public string DelayId = "CMGunShowUseDelay";
}
