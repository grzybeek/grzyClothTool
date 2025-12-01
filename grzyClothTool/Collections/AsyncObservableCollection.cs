using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace grzyClothTool.Collections
{

    public class AsyncObservableCollection<T> : ObservableCollection<T>
    {
        private readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current;
        private readonly Dispatcher _dispatcher;
        private bool _suppressNotification = false;

        public AsyncObservableCollection()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public AsyncObservableCollection(IEnumerable<T> collection) : base(collection)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void AddRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            CheckReentrancy();

            _suppressNotification = true;
            try
            {
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }

        public void RemoveRange(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            CheckReentrancy();

            _suppressNotification = true;
            try
            {
                foreach (var item in items.ToList())
                {
                    Items.Remove(item);
                }
            }
            finally
            {
                _suppressNotification = false;
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }

        public void ReplaceAll(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            CheckReentrancy();

            _suppressNotification = true;
            try
            {
                Items.Clear();
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }

        public Task AddAsync(T item)
        {
            return InvokeOnDispatcherAsync(() => Add(item));
        }

        public Task RemoveAsync(T item)
        {
            return InvokeOnDispatcherAsync(() => Remove(item));
        }

        public Task ClearAsync()
        {
            return InvokeOnDispatcherAsync(() => Clear());
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_suppressNotification)
                return;

            if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
            {
                _synchronizationContext.Post(_ => base.OnCollectionChanged(e), null);
            }
            else
            {
                base.OnCollectionChanged(e);
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (_suppressNotification)
                return;

            if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
            {
                _synchronizationContext.Post(_ => base.OnPropertyChanged(e), null);
            }
            else
            {
                base.OnPropertyChanged(e);
            }
        }

        private Task InvokeOnDispatcherAsync(Action action)
        {
            if (_dispatcher == null || _dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            _dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));

            return tcs.Task;
        }
    }
}
