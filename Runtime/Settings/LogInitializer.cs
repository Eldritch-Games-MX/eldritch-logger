using UnityEngine;

namespace EldritchGames.EldritchLogger.Settings
{
    public class LogInitializer : MonoBehaviour
    {
    	[SerializeField] private LogSettings logSettings;

        private void Awake()
    	{
    		if (logSettings == null)
    		{
    			Debug.LogError("LogSettings not assigned in LogInitializer");
    			return;
    		}

    		EldritchLogger.Initialize(logSettings);
    	}
    }
}