using UnityEngine;
using System.Collections.Generic;

public class SampleLoggerDemo : MonoBehaviour
{
    void Start()
    {
        // Gameplay log
        EldritchLogger.Log(LogLevel.Info, LogCategory.Gameplay,
            $"{SampleLogConstants.PLAYER} picked up {SampleLogConstants.ITEM_POTION}",
            new Dictionary<string, object> { { SampleLogConstants.ITEM_ID, 42 } });

        // Networking warning
        EldritchLogger.Log(LogLevel.Warning, LogCategory.Network,
            $"{SampleLogConstants.PLAYER} lost connection to server");

        // AI error
        EldritchLogger.Log(LogLevel.Error, LogCategory.AI,
            $"{SampleLogConstants.ITEM_POTION} failed to pathfind");
    }
}