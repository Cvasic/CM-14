﻿namespace Content.Shared._CM14.Attachable.Events;

[ByRefEvent]
public readonly record struct RemoveAttachableActionsEvent(EntityUid User);
