using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using EldritchGames.EldritchLogger.Core;
using EldritchGames.EldritchLogger.Settings;

public class SampleLoggerDemo : MonoBehaviour
{
    event Action OnPlayerDeath = () => { /* Player death logic */ };

    void Start()
    {
        // --- Direct logging ---
        ELogger.Log(LogLevel.Info, LogCategory.Gameplay,
            $"{SampleLogConstants.PLAYER} picked up {SampleLogConstants.ITEM_POTION}",
            new Dictionary<string, object> { { SampleLogConstants.ITEM_ID, 42 } });

        ELogger.Log(LogLevel.Warning, LogCategory.Network,
            $"{SampleLogConstants.PLAYER} lost connection to server");

        ELogger.Log(LogLevel.Error, LogCategory.AI,
            $"{SampleLogConstants.ITEM_POTION} failed to pathfind");

        ELogger.Log(LogLevel.Critical, LogCategory.Gameplay,
            "Critical gameplay failure!");

        // --- Fluent builder ---
        ELogger.AtDebug(LogCategory.General)
            .AddKeyValue("InitStep", 1)
            .Log("Debugging startup sequence");

        ELogger.AtInfo(LogCategory.Gameplay)
            .AddKeyValue(SampleLogConstants.ITEM_ID, 42)
            .WithComponent(this)
            .Log($"{SampleLogConstants.PLAYER} picked up {SampleLogConstants.ITEM_POTION}");

        ELogger.AtWarning(LogCategory.Network)
            .WithEvent(OnPlayerDeath, nameof(OnPlayerDeath))
            .Log($"{SampleLogConstants.PLAYER} lost connection to server");

        ELogger.AtError(LogCategory.AI)
            .AddKeyValue("AI_STATE", "PathfindingFailed")
            .WithException(new InvalidOperationException("Boom!"))
            .Log($"{SampleLogConstants.ITEM_POTION} failed to pathfind");

        ELogger.AtCritical(LogCategory.Gameplay)
            .AddKeyValue(SampleLogConstants.PLAYER, 99)
            .Log("Critical gameplay failure!");

        // --- Contextual logging with GameObject ---
        var go = new GameObject("Player");
        ELogger.AtInfo(LogCategory.Gameplay)
            .WithComponent(this)
            .Log("Contextual log with GameObject");

        // --- Active sinks ---
        foreach (var sink in EldritchLogger.Instance.Sinks)
            Debug.Log($"Sink active: {sink.GetType().Name}");

        // --- Coroutine showcase ---
        StartCoroutine(LogOverTime());
    }

    private IEnumerator LogOverTime()
    {
        for (int i = 1; i <= 3; i++)
        {
            ELogger.AtInfo(LogCategory.Gameplay)
                .AddKeyValue("Tick", i)
                .Log($"Coroutine tick {i}");

            yield return new WaitForSeconds(1f);
        }

        ELogger.AtInfo(LogCategory.General)
            .Log("Coroutine finished logging sequence");
    }
}
