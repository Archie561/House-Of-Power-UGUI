# Система управління попапами (Popup System)

Цей модуль забезпечує централізоване управління спливаючими вікнами (попапами) в грі. Вся взаємодія з системою відбувається через єдину точку входу — PopupController, який бере на себе логіку відкриття/закриття, управління стеком вікон та фоновим затемненням (overlay).

# Архітектура Попапів

Для забезпечення чистої архітектури, реалізація кожного вікна розділена на два взаємопов'язані класи:

### [PopupName]Data (DTO контейнер):
Клас, що містить початкові (статичні) дані для вікна. Сюди входять усі необхідні значення, конфігурації та колбеки (наприклад, що робити при натисканні кнопок). Цей об'єкт є незмінним (Immutable) після створення.

### [PopupName] (View):
Безпосередньо візуальний компонент (наслідується від BasePopup). Його єдина відповідальність — відображення даних. Він заповнює всі UI-елементи значеннями з Data-класу через метод Initialize.

## Робота з динамічними даними

Якщо попап відображає дані, які можуть змінюватися в реальному часі (поки вікно відкрите), клас **[PopupName]** повинен містити специфічні методи для їх оновлення (наприклад, UpdateCostVisuals). Це дозволяє оновлювати лише потрібні елементи UI без повної переініціалізації всього вікна.

Виклик цих методів має відбуватися через підписку на відповідні події ігрової логіки. Реєстрація таких підписок здійснюється через параметр setupAction безпосередньо під час виклику методу PopupController.Show().

Важливо: Щоб уникнути витоків пам'яті (Memory Leaks), потрібно завжди відписуватись від подій ігрової логіки, використовуючи базовий івент OnPopupClosed, який гарантовано викликається при знищенні/закритті попапу.

# Приклад використання

    var data = new ReplenishLawsPopupData(
        targetTime: targetTime,
        costType: costType,
        costAmount: costAmount,
        canAfford: canAfford,
        onConfirmClick: () =>
        {
            LawLogicController.Instance.TryReplenishLaws();
            PopupController.Instance.CloseCurrentPopup();
        }
    );

    PopupController.Instance.Show<ReplenishLawsPopup>(popup =>
    {
        // 1. Ініціалізація статичними даними
        popup.Initialize(data);
        
        // 2. Локальна функція для обробки динамічних змін
        void OnLawsCountChanged(int newCount)
        {
            // Якщо логічна умова виконана, закриваємо вікно примусово
            if (newCount >= _maxLawsCount)
            {
                PopupController.Instance.CloseCurrentPopup();
            }
            else
            {
                // Інакше — розраховуємо нові значення і просимо View оновитися
                var newCostAmount = LawLogicController.Instance.GetTotalLawsReplenishCost();
                var newCanAfford = LawLogicController.Instance.CanAfford(ResourceType.Gems, newCostAmount);
                popup.UpdateCostVisuals(newCostAmount, newCanAfford);
            }
        }

        // 3. Підписка на подію логіки
        LawLogicController.Instance.OnLawsCountChanged += OnLawsCountChanged;

        // 4. Гарантована відписка при закритті вікна
        popup.OnPopupClosed += () => LawLogicController.Instance.OnLawsCountChanged -= OnLawsCountChanged;
    });
