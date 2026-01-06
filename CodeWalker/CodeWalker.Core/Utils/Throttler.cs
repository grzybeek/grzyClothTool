using System;
using System.Timers;
using Timer = System.Timers.Timer;

namespace CodeWalker
{

    public class Throttler
    {
        private Timer _timer;
        private Action _action;
        private bool _isThrottled;

        public Throttler(double interval)
        {
            _timer = new Timer(interval);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = false;
        }

        public void Throttle(Action action)
        {
            if (_isThrottled)
                return;

            _action = action;
            _isThrottled = true;
            _action?.Invoke();
            _timer.Start();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _isThrottled = false;
        }
    }
}