using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

[assembly: InternalsVisibleTo("Abg.DebugValues.Editor")]

namespace Abg.Debug
{
    public class DebugValues
    {
        private static Thread mainThread;
        private static readonly ConcurrentQueue<(string, ValueToDispatch)> valueQueue = new ConcurrentQueue<(string, ValueToDispatch)>();
        public static readonly Dictionary<string, Category> categories = new Dictionary<string, Category>();
        public static readonly Category defaultCategory;

        public static event Action<Category> CategoryAdded;
        public static event Action Cleared;

        static DebugValues()
        {
            defaultCategory = new Category("Default");
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            mainThread = Thread.CurrentThread;
        }

        public static void DispatchValues()
        {
            while (valueQueue.TryDequeue(out var value))
            {
                GetCategory(value.Item2.Category).SetValue(value.Item2.Name, value.Item2.Value);
            }
        }

        private static Category GetCategory(string name)
        {
            if (name == null) return defaultCategory;
            if (!categories.TryGetValue(name, out var category))
            {
                category = new Category(name);
                categories.Add(name, category);
                CategoryAdded?.Invoke(category);
            }

            return category;
        }

        private static string FormattedValue(object value, string format = null)
        {
            if (value is string stringValue && format == null) return stringValue;
            return format != null ? string.Format("{0:" + format + "}", value) : value?.ToString();
        }

        public static void Show(string name, object value, string format = null)
        {
            if (Thread.CurrentThread != mainThread)
            {
                valueQueue.Enqueue((name, new ValueToDispatch(null, name, FormattedValue(value, format))));
                return;
            }
            GetCategory(null).SetValue(name, FormattedValue(value, format));
        }

        public static void ShowAt(object category, string name, object value, string format = null)
        {
            if (Thread.CurrentThread != mainThread)
            {
                valueQueue.Enqueue((name, new ValueToDispatch(category.ToString(), name, FormattedValue(value, format))));
                return;
            }

            GetCategory(category.ToString()).SetValue(name, FormattedValue(value, format));
        }

        public static void Clear()
        {
            defaultCategory.Values.Clear();
            categories.Clear();
            Cleared?.Invoke();
        }

        public class Category
        {
            public readonly string Name;
            public readonly Dictionary<string, ValueInstance> Values = new Dictionary<string, ValueInstance>();
            public event Action<string, ValueInstance> ValueAdded;

            public Category(string name)
            {
                Name = name;
            }

            public void SetValue(string name, string value)
            {
                if (!Values.TryGetValue(name, out var valueInstance))
                {
                    valueInstance = new ValueInstance();
                    Values[name] = valueInstance;
                    ValueAdded?.Invoke(name, valueInstance);
                }

                valueInstance.Value = value;
            }
        }

        private struct ValueToDispatch
        {
            public readonly string Category;
            public readonly string Name;
            public readonly string Value;

            public ValueToDispatch(string category, string name, string value)
            {
                Category = category;
                Name = name;
                Value = value;
            }
        }

        public class ValueInstance
        {
            public string Value;

            public override string ToString()
            {
                return Value;
            }
        }
    }
}
