﻿using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Notifications.Wpf.Controls
{
    public class NotificationArea : Control
    {
        public int MaxItems
        {
            get { return (int)GetValue(MaxItemsProperty); }
            set { SetValue(MaxItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxItems.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxItemsProperty =
            DependencyProperty.Register("MaxItems", typeof(int), typeof(NotificationArea), new PropertyMetadata(int.MaxValue));

        private IList _items;

        public NotificationArea()
        {
            NotificationManager.AddArea(this);
        }

        static NotificationArea()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NotificationArea),
                new FrameworkPropertyMetadata(typeof(NotificationArea)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var itemsControl = GetTemplateChild("PART_Items") as Panel;
            _items = itemsControl?.Children;
        }

#if NET45
        public async void Show(object content, TimeSpan expirationTime, Action onClick, Action onClose)
#else
        public void Show(object content, TimeSpan expirationTime, Action onClick, Action onClose)
#endif
        {
            var notification = new Notification
            {
                Content = content,
                Foreground = Brushes.White
            };
            
            notification.MouseLeftButtonDown += (sender, args) => onClick?.Invoke();
            notification.NotificationClosed += (sender, args) => onClose?.Invoke();
            notification.NotificationClosed += OnNotificationClosed;

            lock (_items)
            {
                _items.Add(notification);

                if (_items.OfType<Notification>().Count(i => !i.IsClosing) > MaxItems)
                {
                    _items.OfType<Notification>().First(i => !i.IsClosing).Close();
                }
            }

#if NET45
            await Task.Delay(expirationTime);
#else
            DelayExecute(expirationTime, () =>
            {
#endif
                notification.Close();
#if !NET45
            });
#endif
        }

        private void OnNotificationClosed(object sender, RoutedEventArgs routedEventArgs)
        {
            var notification = sender as Notification;
            _items.Remove(notification);
        }

#if !NET45
        private static void DelayExecute(TimeSpan delay, Action actionToExecute)
        {
            if (actionToExecute != null)
            {
                var timer = new DispatcherTimer
                {
                    Interval = delay
                };
                timer.Tick += (sender, args) =>
                {
                    timer.Stop();
                    actionToExecute();
                };
                timer.Start();
            }
        }
#endif
    }
}