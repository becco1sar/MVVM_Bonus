using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MVVM_Bonus.Services
{
    public static class Mediator
    {
        private static readonly ConcurrentDictionary<string, List<Action<object>>> _subscribers 
            = new ConcurrentDictionary<string, List<Action<object>>>(StringComparer.OrdinalIgnoreCase);

        public static void Subscribe(string token, Action<object> callback)
        {
            if (string.IsNullOrWhiteSpace(token) || callback == null)
                return;

            _subscribers.AddOrUpdate(
                token,
                new List<Action<object>> { callback },
                (key, existingList) =>
                {
                    lock (existingList)
                    {
                        if (!existingList.Contains(callback))
                        {
                            existingList.Add(callback);
                        }
                    }
                    return existingList;
                });
        }

        public static void UnSubscribe(string token, Action<object> callback)
        {
            if (string.IsNullOrWhiteSpace(token) || callback == null)
                return;

            if (_subscribers.TryGetValue(token, out var list))
            {
                lock (list)
                {
                    list.Remove(callback);
                }
            }
        }

        public static void Notify(string token, object args = null)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            if (_subscribers.TryGetValue(token, out var list))
            {
                List<Action<object>> callbacksCopy;
                lock (list)
                {
                    callbacksCopy = new List<Action<object>>(list);
                }

                foreach (var callback in callbacksCopy)
                {
                    callback?.Invoke(args);
                }
            }
        }
    }
}
