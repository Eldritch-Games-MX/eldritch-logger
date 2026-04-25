using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using EldritchGames.EldritchLogger.Core;

// Pattern A — per-class named logger initialized in Awake (recommended for MonoBehaviours).
// Do NOT use a field initializer: Unity can run MonoBehaviour constructors during
// edit-mode deserialization, before LoggerBootstrap has registered the factory.
public class SampleLoggerDemo : MonoBehaviour
{
    private IEldritchLogger _logger;

    event Action OnPlayerDeath = () => { /* Player death logic */ };

    void Awake()
    {
        _logger = ELoggerFactory.GetLogger<SampleLoggerDemo>();
    }

    void Start()
    {
        Debug.Log("Starting SampleLoggerDemo...");
        // --- Direct logging ---
        _logger.Log(LogLevel.Info, LogCategory.Gameplay,
            $"{SampleLogConstants.PLAYER} picked up {SampleLogConstants.ITEM_POTION}",
            new Dictionary<string, object> { { SampleLogConstants.ITEM_ID, 42 } });

        _logger.Log(LogLevel.Warning, LogCategory.Network,
            $"{SampleLogConstants.PLAYER} lost connection to server");

        _logger.Log(LogLevel.Error, LogCategory.AI,
            $"{SampleLogConstants.ITEM_POTION} failed to pathfind");

        _logger.Log(LogLevel.Critical, LogCategory.Gameplay,
            "Critical gameplay failure!");

        // --- Fluent builder ---
        _logger.AtDebug(LogCategory.General)
            .AddKeyValue("InitStep", 1)
            .Log("Debugging startup sequence");

        _logger.AtInfo(LogCategory.Gameplay)
            .AddKeyValue(SampleLogConstants.ITEM_ID, 42)
            .WithComponent(this)
            .Log($"{SampleLogConstants.PLAYER} picked up {SampleLogConstants.ITEM_POTION}");

        _logger.AtWarning(LogCategory.Network)
            .WithEvent(OnPlayerDeath, nameof(OnPlayerDeath))
            .Log($"{SampleLogConstants.PLAYER} lost connection to server");

        _logger.AtError(LogCategory.AI)
            .AddKeyValue("AI_STATE", "PathfindingFailed")
            .WithException(new InvalidOperationException("Boom!"))
            .Log($"{SampleLogConstants.ITEM_POTION} failed to pathfind");

        _logger.AtCritical(LogCategory.Gameplay)
            .AddKeyValue(SampleLogConstants.PLAYER, 99)
            .Log("Critical gameplay failure!");

        // --- Contextual logging with GameObject ---
        _logger.AtInfo(LogCategory.Gameplay)
            .WithComponent(this)
            .Log("Contextual log with GameObject");

        // --- Coroutine showcase ---
        StartCoroutine(LogOverTime());
    }

    private IEnumerator LogOverTime()
    {
        for (int i = 1; i <= 3; i++)
        {
            _logger.AtInfo(LogCategory.Gameplay)
                .AddKeyValue("Tick", i)
                .Log($"Coroutine tick {i}");

            yield return new WaitForSeconds(1f);
        }

        _logger.AtInfo(LogCategory.General)
            .Log("Coroutine finished logging sequence");
    }
}

// Pattern B — constructor injection for pure C# classes (recommended outside MonoBehaviour).
// The DI container (or test setup) provides the IEldritchLogger instance explicitly.
public class SampleGameService
{
    private readonly IEldritchLogger _logger;

    public SampleGameService(IEldritchLogger logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        _logger.AtInfo(LogCategory.Gameplay).Log("SampleGameService doing work");
    }
}
