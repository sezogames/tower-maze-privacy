using System;

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

namespace TowerMaze
{
    public static class NotificationManager
    {
        private const int LivesFullNotificationId = 42;
        private const string ChannelId = "lives";
        private const string NotificationIdentifier = "lives_full";

        public static void RequestPermissions()
        {
#if UNITY_IOS
            RequestIOSPermissions();
#endif
#if UNITY_ANDROID
            RegisterAndroidChannel();
#endif
        }

#if UNITY_IOS
        private static async void RequestIOSPermissions()
        {
            using var request = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound, true);
            while (!request.IsFinished)
            {
                await System.Threading.Tasks.Task.Yield();
            }
        }
#endif

#if UNITY_ANDROID
        private static void RegisterAndroidChannel()
        {
            var channel = new AndroidNotificationChannel
            {
                Id = ChannelId,
                Name = "Can Bildirimleri",
                Description = "Canlar dolduğunda bildirim gönderir",
                Importance = Importance.Default,
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
        }
#endif

        /// <summary>
        /// Canların dolacağı zamanda yerel bildirim zamanlar.
        /// Mevcut bir bildirim varsa önce iptal eder.
        /// </summary>
        public static void ScheduleLivesFullNotification(DateTime fireAt)
        {
            CancelLivesFullNotification();

#if UNITY_IOS
            var timeInterval = fireAt.ToUniversalTime() - DateTime.UtcNow;
            if (timeInterval.TotalSeconds < 1.0)
                return;

            var notification = new iOSNotification
            {
                Identifier = NotificationIdentifier,
                Title = "Canlar Doldu!",
                Body = "Tower Maze'e dön ve oynamaya devam et!",
                ShowInForeground = false,
                Trigger = new iOSNotificationTimeIntervalTrigger
                {
                    TimeInterval = timeInterval,
                    Repeats = false,
                },
            };
            iOSNotificationCenter.ScheduleNotification(notification);
#endif

#if UNITY_ANDROID
            var timeSpan = fireAt.ToUniversalTime() - DateTime.UtcNow;
            if (timeSpan.TotalSeconds < 1.0)
                return;

            var notification = new AndroidNotification
            {
                Title = "Canlar Doldu!",
                Text = "Tower Maze'e dön ve oynamaya devam et!",
                FireTime = DateTime.Now.Add(timeSpan),
                SmallIcon = "icon_0",
                LargeIcon = "icon_0",
            };
            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, ChannelId, LivesFullNotificationId);
#endif
        }

        /// <summary>
        /// Planlanmış "canlar doldu" bildirimini iptal eder.
        /// </summary>
        public static void CancelLivesFullNotification()
        {
#if UNITY_IOS
            iOSNotificationCenter.RemoveScheduledNotification(NotificationIdentifier);
#endif

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelScheduledNotification(LivesFullNotificationId);
#endif
        }
    }
}
