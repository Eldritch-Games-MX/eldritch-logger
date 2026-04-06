using UnityEngine;
using System.Collections.Generic;

public class SampleLoggerDemo : MonoBehaviour
{
    void Start()
    {
        EldritchLogger.Log(LogLevel.Info, LogCategory.Gameplay,
            $"{SampleLogConstants.PLAYER} picked up {SampleLogConstants.ITEM_POTION}",
            new Dictionary<string, object> { { SampleLogConstants.ITEM_ID, 42 } });

        EldritchLogger.Log(LogLevel.Warning, LogCategory.Network,
            $"{SampleLogConstants.PLAYER} lost connection to server");

        EldritchLogger.Log(LogLevel.Error, LogCategory.AI,
            $"{SampleLogConstants.ITEM_POTION} failed to pathfind");

        EldritchLogger.AtInfo()
            .Category(LogCategory.Gameplay)
            .AddKeyValue(SampleLogConstants.ITEM_ID, 42)
            .Log($"{SampleLogConstants.PLAYER} picked up {SampleLogConstants.ITEM_POTION}");

        EldritchLogger.AtWarning()
            .Category(LogCategory.Network)
            .Log($"{SampleLogConstants.PLAYER} lost connection to server");

        EldritchLogger.AtError()
            .Category(LogCategory.AI)
            .AddKeyValue("AI_STATE", "PathfindingFailed")
            .Log($"{SampleLogConstants.ITEM_POTION} failed to pathfind");

        EldritchLogger.AtCritical()
            .Category(LogCategory.Gameplay)
            .AddKeyValue(SampleLogConstants.PLAYER_ID, 99)
            .Log("Critical gameplay failure!");
    }
}