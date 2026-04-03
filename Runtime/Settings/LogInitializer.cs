using UnityEngine;

namespace EldritchGames.EldritchLogger.Setttings
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