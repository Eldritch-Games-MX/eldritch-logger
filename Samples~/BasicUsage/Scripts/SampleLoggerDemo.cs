using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EldritchGames.EldritchLogger;
using System;
using EldritchGames.EldritchLogger.Settings;

public class SampleLoggerDemo : MonoBehaviour
{
    event Action OnPlayerDeath = () => { /* Player death logic */ };

    void Start()
    {
        // --- Direct logging ---
        EldritchLogger.Instance.Log(LogLevel.Info, LogCategory.Gameplay,
            $"{SampleLogConstants.PLAYER} picked up {SampleLogConstants.ITEM_POTION}",
            new Dictionary<string, object> { { SampleLogConstants.ITEM_ID, 42 } });

        EldritchLogger.Instance.Log(LogLevel.Warning, LogCategory.Network,
            $"{SampleLogConstants.PLAYER} lost connection to server");

        EldritchLogger.Instance.Log(LogLevel.Error, LogCategory.AI,
            $"{SampleLogConstants.ITEM_POTION} failed to pathfind");

        EldritchLogger.Instance.Log(LogLevel.Critical, LogCategory.Gameplay,
            "Critical gameplay failure!");

        // --- Fluent builder ---
        EldritchLogger.Instance.AtDebug(LogCategory.General)
            .AddKeyValue("InitStep", 1)
            .Log("Debugging startup sequence");

        EldritchLogger.Instance.AtInfo(LogCategory.Gameplay)
            .AddKeyValue(SampleLogConstants.ITEM_ID, 42)
            .WithComponent(this)
            .Log($"{SampleLogConstants.PLAYER} picked up {SampleLogConstants.ITEM_POTION}");

        EldritchLogger.Instance.AtWarning(LogCategory.Network)
            .WithEvent(OnPlayerDeath, nameof(OnPlayerDeath))
            .Log($"{SampleLogConstants.PLAYER} lost connection to server");

        EldritchLogger.Instance.AtError(LogCategory.AI)
            .AddKeyValue("AI_STATE", "PathfindingFailed")
            .WithException(new InvalidOperationException("Boom!"))
            .Log($"{SampleLogConstants.ITEM_POTION} failed to pathfind");

        EldritchLogger.Instance.AtCritical(LogCategory.Gameplay)
            .AddKeyValue(SampleLogConstants.PLAYER, 99)
            .Log("Critical gameplay failure!");

        // --- Contextual logging with GameObject ---
        var go = new GameObject("Player");
        EldritchLogger.Instance.AtInfo(LogCategory.Gameplay)
            .WithComponent(this)
            .Log("Contextual log with GameObject");

        // --- Exporter showcase ---
        foreach (var exporter in EldritchLogger.Instance.Exporters)
        {
            Debug.Log($"Exporter active: {exporter.GetType().Name}");
        }

        // --- Coroutine showcase ---
        StartCoroutine(LogOverTime());
    }

    private IEnumerator LogOverTime()
    {
        for (int i = 1; i <= 3; i++)
        {
            EldritchLogger.Instance.AtInfo(LogCategory.Gameplay)
                .AddKeyValue("Tick", i)
                .Log($"Coroutine tick {i}");

            yield return new WaitForSeconds(1f);
        }

        EldritchLogger.Instance.AtInfo(LogCategory.General)
            .Log("Coroutine finished logging sequence");
    }
}
