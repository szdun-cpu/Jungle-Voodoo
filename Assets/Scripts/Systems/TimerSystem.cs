using System;
using System.Collections.Generic;
using UnityEngine;
using JungleVoodoo.Data;

namespace JungleVoodoo.Systems
{
    public enum TimerType { Construction, Training, Research, March, Gathering }

    /// <summary>
    /// Fires events on timer completion and reconciles with server timestamps
    /// loaded from PlayFab on startup. All timers are server-authoritative
    /// (stored as UTC ISO-8601 end times); this class handles local countdown.
    /// </summary>
    public class TimerSystem
    {
        private readonly List<GameTimer> _timers = new();

        public event Action<GameTimer> OnTimerCompleted;
        public event Action<GameTimer> OnTimerAdded;
        public event Action<string>    OnTimerRemoved;

        // Called every frame by whoever ticks this system (e.g. a MonoBehaviour wrapper).
        public void Tick()
        {
            var now = DateTime.UtcNow;
            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                if (now >= _timers[i].EndUtc)
                {
                    var completed = _timers[i];
                    _timers.RemoveAt(i);
                    OnTimerCompleted?.Invoke(completed);
                }
            }
        }

        public void AddTimer(ActiveTimer timerData)
        {
            if (!DateTime.TryParse(timerData.EndUtc, null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var endUtc))
            {
                Debug.LogError($"[TimerSystem] Invalid EndUtc for timer '{timerData.TimerId}': {timerData.EndUtc}");
                return;
            }

            // Already expired — fire immediately
            if (DateTime.UtcNow >= endUtc)
            {
                OnTimerCompleted?.Invoke(new GameTimer(timerData, endUtc));
                return;
            }

            var timer = new GameTimer(timerData, endUtc);
            _timers.Add(timer);
            OnTimerAdded?.Invoke(timer);
        }

        public void AddTimer(string timerId, TimerType type, string targetId, DateTime endUtc)
        {
            AddTimer(new ActiveTimer
            {
                TimerId  = timerId,
                Type     = type.ToString().ToLower(),
                TargetId = targetId,
                EndUtc   = endUtc.ToString("o")
            });
        }

        public void RemoveTimer(string timerId)
        {
            _timers.RemoveAll(t => t.TimerId == timerId);
            OnTimerRemoved?.Invoke(timerId);
        }

        public void LoadFromProfile(List<ActiveTimer> timers)
        {
            _timers.Clear();
            foreach (var t in timers)
                AddTimer(t);
        }

        public float GetRemainingSeconds(string timerId)
        {
            var timer = _timers.Find(t => t.TimerId == timerId);
            if (timer == null) return 0f;
            return Mathf.Max(0f, (float)(timer.EndUtc - DateTime.UtcNow).TotalSeconds);
        }

        public List<ActiveTimer> Serialize()
        {
            var result = new List<ActiveTimer>();
            foreach (var t in _timers)
                result.Add(new ActiveTimer
                {
                    TimerId  = t.TimerId,
                    Type     = t.Type.ToString().ToLower(),
                    TargetId = t.TargetId,
                    EndUtc   = t.EndUtc.ToString("o")
                });
            return result;
        }
    }

    public class GameTimer
    {
        public string    TimerId;
        public TimerType Type;
        public string    TargetId;
        public DateTime  EndUtc;

        public GameTimer(ActiveTimer data, DateTime endUtc)
        {
            TimerId  = data.TimerId;
            TargetId = data.TargetId;
            EndUtc   = endUtc;
            Enum.TryParse<TimerType>(data.Type, true, out Type);
        }
    }
}
