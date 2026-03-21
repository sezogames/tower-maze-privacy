using System;
using System.Collections;
using UnityEngine;

namespace TowerMaze
{
    public enum RewardedAdRequestType
    {
        Retry = 0,
        Continue = 1,
        Generic = 2,
    }

    public sealed class AdManager : MonoBehaviour
    {
        [Header("Provider")]
        [SerializeField] private RewardedAdManager rewardedAdProvider;
        [SerializeField] private bool useSimulatedAdsWhenProviderMissing = true;

        [Header("Simulation")]
        [SerializeField, Min(0.5f)] private float simulatedAdDurationSeconds = 2f;
        [SerializeField] private bool verboseLogs;

        private bool adFlowInProgress;

        public bool IsAdFlowInProgress => adFlowInProgress;
        public bool CanShowRewardedAd => rewardedAdProvider != null ? rewardedAdProvider.CanShowRewarded : useSimulatedAdsWhenProviderMissing;

        public void ShowRewardedAd(RewardedAdRequestType requestType, Action<bool> onCompleted)
        {
            if (adFlowInProgress)
            {
                onCompleted?.Invoke(false);
                return;
            }

            StartCoroutine(ShowSingleRewardedRoutine(requestType, onCompleted));
        }

        public void ShowRewardedAds(int adCount, RewardedAdRequestType requestType, Action<bool> onCompleted)
        {
            if (adCount <= 0)
            {
                onCompleted?.Invoke(true);
                return;
            }

            if (adFlowInProgress)
            {
                onCompleted?.Invoke(false);
                return;
            }

            StartCoroutine(ShowSequentialRewardedRoutine(adCount, requestType, onCompleted));
        }

        private IEnumerator ShowSingleRewardedRoutine(RewardedAdRequestType requestType, Action<bool> onCompleted)
        {
            adFlowInProgress = true;
            yield return ExecuteRewardedRoutine(requestType, success => onCompleted?.Invoke(success));
            adFlowInProgress = false;
        }

        private IEnumerator ShowSequentialRewardedRoutine(int adCount, RewardedAdRequestType requestType, Action<bool> onCompleted)
        {
            adFlowInProgress = true;

            bool allCompleted = true;
            for (int index = 0; index < adCount; index++)
            {
                bool finished = false;
                bool success = false;

                yield return ExecuteRewardedRoutine(requestType, result =>
                {
                    success = result;
                    finished = true;
                });

                if (!finished || !success)
                {
                    allCompleted = false;
                    break;
                }
            }

            adFlowInProgress = false;
            onCompleted?.Invoke(allCompleted);
        }

        private IEnumerator ExecuteRewardedRoutine(RewardedAdRequestType requestType, Action<bool> onCompleted)
        {
            if (rewardedAdProvider != null)
            {
                bool callbackReceived = false;
                bool rewarded = false;
                RewardedPlacement placement = MapPlacement(requestType);

                rewardedAdProvider.ShowRewarded(placement, success =>
                {
                    rewarded = success;
                    callbackReceived = true;
                });

                while (!callbackReceived)
                {
                    yield return null;
                }

                LogAdResult(requestType, rewarded, simulated: false);
                onCompleted?.Invoke(rewarded);
                yield break;
            }

            if (!useSimulatedAdsWhenProviderMissing)
            {
                LogAdResult(requestType, false, simulated: false);
                onCompleted?.Invoke(false);
                yield break;
            }

            yield return new WaitForSecondsRealtime(simulatedAdDurationSeconds);
            LogAdResult(requestType, true, simulated: true);
            onCompleted?.Invoke(true);
        }

        private static RewardedPlacement MapPlacement(RewardedAdRequestType requestType)
        {
            switch (requestType)
            {
                case RewardedAdRequestType.Retry:
                case RewardedAdRequestType.Continue:
                    return RewardedPlacement.LifeRefill;
                default:
                    return RewardedPlacement.ShopCoinBoost;
            }
        }

        private void LogAdResult(RewardedAdRequestType requestType, bool success, bool simulated)
        {
            if (!verboseLogs)
            {
                return;
            }

            string source = simulated ? "SIMULATED" : "PROVIDER";
            Debug.Log($"[AdManager] {source} rewarded ad for {requestType}: {(success ? "SUCCESS" : "FAIL")}");
        }
    }
}
