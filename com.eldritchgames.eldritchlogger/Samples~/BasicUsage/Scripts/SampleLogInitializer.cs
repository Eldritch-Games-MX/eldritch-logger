using UnityEngine;

public class SampleLogInitializer : MonoBehaviour
{
    [SerializeField] private LogSettings logSettings;

    void Awake()
    {
        EldritchLogger.Initialize(logSettings);
    }
}