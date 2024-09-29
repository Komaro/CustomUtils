using System.Collections.Immutable;
using UnityEditor;
using UnityEngine;

public static class EditorCommonExtension {

    private static readonly ImmutableHashSet<EventType> _ignoreEventTypeSet = ImmutableHashSet.Create(EventType.Ignore, EventType.Layout, EventType.Repaint, EventType.Used, EventType.ExecuteCommand, EventType.ValidateCommand);

    public static bool IsProcessableEvent(this EventType eventType) => _ignoreEventTypeSet.Contains(eventType) == false;

    public static BuildTargetGroup GetTargetGroup(this BuildTarget type) => BuildPipeline.GetBuildTargetGroup(type);
}
