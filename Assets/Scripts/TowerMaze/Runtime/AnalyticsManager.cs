using System.Collections.Generic;
using UnityEngine;
#if FIREBASE_ANALYTICS
using Firebase;
using Firebase.Analytics;
#endif

namespace TowerMaze
{
    public static class AnalyticsManager
    {
        private static bool initialized;
        private static bool firebaseReady;

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;
#if FIREBASE_ANALYTICS
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    firebaseReady = true;
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    Debug.Log("[Analytics] Firebase initialized successfully");
                }
                else
                {
                    Debug.LogWarning($"[Analytics] Firebase not available: {task.Result}. Using fallback logging.");
                }
            });
#endif
            LogEvent("app_start");
        }

        public static void LogEvent(string eventName)
        {
#if FIREBASE_ANALYTICS
            if (firebaseReady)
            {
                FirebaseAnalytics.LogEvent(eventName);
                return;
            }
#endif
            if (Application.isEditor || Debug.isDebugBuild)
                Debug.Log($"[Analytics] {eventName}");
        }

        public static void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                LogEvent(eventName);
                return;
            }

#if FIREBASE_ANALYTICS
            if (firebaseReady)
            {
                var firebaseParams = new List<Parameter>(parameters.Count);
                foreach (var kvp in parameters)
                {
                    if (kvp.Value is int intVal)
                        firebaseParams.Add(new Parameter(kvp.Key, intVal));
                    else if (kvp.Value is long longVal)
                        firebaseParams.Add(new Parameter(kvp.Key, longVal));
                    else if (kvp.Value is float floatVal)
                        firebaseParams.Add(new Parameter(kvp.Key, (double)floatVal));
                    else if (kvp.Value is double doubleVal)
                        firebaseParams.Add(new Parameter(kvp.Key, doubleVal));
                    else
                        firebaseParams.Add(new Parameter(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
                }
                FirebaseAnalytics.LogEvent(eventName, firebaseParams.ToArray());
                return;
            }
#endif
            if (Application.isEditor || Debug.isDebugBuild)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("[Analytics] ").Append(eventName).Append(" { ");
                bool first = true;
                foreach (var kvp in parameters)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(kvp.Key).Append(": ").Append(kvp.Value);
                    first = false;
                }
                sb.Append(" }");
                Debug.Log(sb.ToString());
            }
        }
    }
}
