using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimatorGraphRepairTool
{
    [MenuItem("Tools/Animation/Scan Animator Graphs")]
    public static void ScanAnimatorGraphs()
    {
        int controllers = 0;
        int brokenTransitions = 0;

        foreach (var controller in LoadAllControllers())
        {
            controllers++;
            brokenTransitions += CountBrokenTransitions(controller);
        }

        Debug.Log($"Animator scan complete. Controllers: {controllers}, broken transitions found: {brokenTransitions}");
    }

    [MenuItem("Tools/Animation/Repair Broken Animator Transitions")]
    public static void RepairBrokenAnimatorTransitions()
    {
        int controllers = 0;
        int removed = 0;

        foreach (var controller in LoadAllControllers())
        {
            controllers++;
            removed += RepairController(controller);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Animator repair complete. Controllers: {controllers}, broken transitions removed: {removed}");
    }

    private static IEnumerable<AnimatorController> LoadAllControllers()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null)
                yield return controller;
        }
    }

    private static int CountBrokenTransitions(AnimatorController controller)
    {
        int broken = 0;
        foreach (var layer in controller.layers)
            broken += CountBrokenInStateMachine(layer.stateMachine);
        return broken;
    }

    private static int RepairController(AnimatorController controller)
    {
        int removed = 0;
        bool changed = false;

        foreach (var layer in controller.layers)
            removed += RepairStateMachineRecursive(layer.stateMachine, ref changed);

        if (changed)
            EditorUtility.SetDirty(controller);

        return removed;
    }

    private static int CountBrokenInStateMachine(AnimatorStateMachine sm)
    {
        int broken = 0;

        // Any State transitions in this state machine.
        foreach (var transition in sm.anyStateTransitions)
        {
            if (IsBrokenTransition(transition))
                broken++;
        }

        // State transitions.
        foreach (var childState in sm.states)
        {
            foreach (var transition in childState.state.transitions)
            {
                if (IsBrokenTransition(transition))
                    broken++;
            }
        }

        // Child state machines.
        foreach (var child in sm.stateMachines)
            broken += CountBrokenInStateMachine(child.stateMachine);

        return broken;
    }

    private static int RepairStateMachineRecursive(AnimatorStateMachine sm, ref bool changed)
    {
        int removed = 0;

        // Repair Any State transitions.
        var repairedAny = new List<AnimatorStateTransition>();
        foreach (var transition in sm.anyStateTransitions)
        {
            if (IsBrokenTransition(transition))
            {
                removed++;
                changed = true;
            }
            else
            {
                repairedAny.Add(transition);
            }
        }
        if (repairedAny.Count != sm.anyStateTransitions.Length)
            sm.anyStateTransitions = repairedAny.ToArray();

        // Repair transitions on each state.
        for (int i = 0; i < sm.states.Length; i++)
        {
            AnimatorState state = sm.states[i].state;
            var repaired = new List<AnimatorStateTransition>();
            foreach (var transition in state.transitions)
            {
                if (IsBrokenTransition(transition))
                {
                    removed++;
                    changed = true;
                }
                else
                {
                    repaired.Add(transition);
                }
            }
            if (repaired.Count != state.transitions.Length)
                state.transitions = repaired.ToArray();
        }

        // Recurse.
        foreach (var child in sm.stateMachines)
            removed += RepairStateMachineRecursive(child.stateMachine, ref changed);

        return removed;
    }

    private static bool IsBrokenTransition(AnimatorStateTransition transition)
    {
        if (transition == null) return true;
        if (transition.isExit) return false;
        return transition.destinationState == null && transition.destinationStateMachine == null;
    }
}
