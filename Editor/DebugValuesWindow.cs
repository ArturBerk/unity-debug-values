using System.Collections.Concurrent;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Abg.Debug
{
    class DebugValuesWindow : EditorWindow
    {
        [MenuItem("Window/Debug Values")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(DebugValuesWindow)).titleContent = new GUIContent("Debug Values");
        }

        public DebugValuesWindow()
        {
            titleContent = new GUIContent("Debug Values");
        }

        private VisualElement Container;
        private CategoryElement defaultCategory;
        private ConcurrentBag<(string Name, DebugValues.Category Category)> newCategories =
            new ConcurrentBag<(string Name, DebugValues.Category Category)>();

        private class CategoryElement : Foldout
        {
            private readonly VisualElement Container;
            private ConcurrentBag<(string Name, DebugValues.ValueInstance Value)> newValues =
                new ConcurrentBag<(string Name, DebugValues.ValueInstance Value)>();

            public CategoryElement(DebugValues.Category category)
            {
                text = category.Name;
                Add(Container = new VisualElement());
                category.ValueAdded += CategoryOnValueAdded;
                foreach (var valuesKey in category.Values)
                {
                    AddItemElement(valuesKey.Key, valuesKey.Value);
                }
            }

            private void CategoryOnValueAdded(string name, DebugValues.ValueInstance valueInstance)
            {
                newValues.Add((name, valueInstance));
            }

            private void AddItemElement(string name, DebugValues.ValueInstance valueInstance)
            {
                var itemHandler = new ItemElement(name, valueInstance, Container.childCount);
                Container.Add(itemHandler);
            }

            public void Update()
            {
                while (newValues.TryTake(out var valueTuple))
                {
                    AddItemElement(valueTuple.Name, valueTuple.Value);
                }

                if (!value) return;
                for (int i = 0; i < Container.childCount; i++)
                {
                    var item = Container[i] as ItemElement;
                    item?.Update();
                }
            }
        }

        private class ItemElement : VisualElement
        {
            private readonly DebugValues.ValueInstance valueInstance;
            public readonly int Index;
            public readonly Label ValueLabel;

            public ItemElement(string name, DebugValues.ValueInstance valueInstance, int index)
            {
                this.valueInstance = valueInstance;
                this.Index = index;
                style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
                style.paddingBottom = 2;
                style.paddingTop = 2;
                // style.backgroundColor = Index % 2 == 0
                //     ? new StyleColor(Color.Lerp(Color.gray, Color.white, 0.7f))
                //     : new StyleColor(Color.Lerp(Color.gray, Color.white, 0.6f));
                var nameLabel = new Label(name);
                //                NameLabel.style.backgroundColor = new StyleColor(Color.green);
                nameLabel.style.flexGrow = 1;
                nameLabel.style.flexBasis = 50;
                ValueLabel = new Label();
                //                ValueLabel.style.backgroundColor = new StyleColor(Color.blue);
                ValueLabel.style.flexGrow = 2;
                ValueLabel.style.flexBasis = 100;
                Add(nameLabel);
                Add(ValueLabel);
            }

            public void Update()
            {
                ValueLabel.text = valueInstance.Value;
            }
        }

        private void OnEnable()
        {
            const float margin = 5;
            //DebugValues.Initialize();
            DebugValues.CategoryAdded += OnCategoryAdded;
            DebugValues.Cleared += OnCleared;
            Container = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    marginBottom = margin,
                    marginTop = margin,
                    marginLeft = margin,
                    marginRight = margin
                }
            };
            Container.style.flexGrow = 1;
            var clearButton = new Button(ClickEvent)
            {
                text = "Clear",
                style =
                {
                    marginBottom = margin,
                    marginLeft = margin,
                    marginRight = margin
                }
            };
            clearButton.style.flexGrow = 0;
            rootVisualElement.Add(Container);
            rootVisualElement.Add(clearButton);

            defaultCategory = new CategoryElement(DebugValues.defaultCategory);
            Container.Add(defaultCategory);
            foreach (var category in DebugValues.categories.Values)
            {
                Container.Add(new CategoryElement(category));
            }
        }

        private void OnCategoryAdded(DebugValues.Category category)
        {
            newCategories.Add((name, category));
        }

        private void OnCleared()
        {
            Container.Clear();
            Container.Add(defaultCategory);
        }

        private void ClickEvent()
        {
            DebugValues.Clear();
        }

        private float elapsedTime;

        private void Update()
        {
            elapsedTime += Time.unscaledDeltaTime;
            if (elapsedTime < 0.1f) return;
            elapsedTime = 0.0f;

            while (newCategories.TryTake(out var pair))
            {
                Container.Add(new CategoryElement(pair.Category));
            }
            DebugValues.DispatchValues();

            for (int i = 0; i < Container.childCount; i++)
            {
                var categoryElement = Container[i] as CategoryElement;
                categoryElement?.Update();
            }
        }
    }
}
