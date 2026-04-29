# Popup Management
This module is responsible for the logic of all popup windows in the game. The architecture is designed so that the UI is as decoupled as possible from the game's business logic.

## Base Classes and Management
**BasePopup**: The base class inherited by all popups. It contains the common logic inherent to all popup windows in the system.

**PopupController**: Responsible for managing the lifecycle of the windows. It handles opening, closing, animations, as well as the popup stack logic.


## Architectural Approaches
We use two different approaches for building popups, depending on whether the data inside them changes in real-time.

**Static Windows (Passive View + DTO)**
Used for windows where data is generated once upon opening and does not change (e.g., AcceptOfferPopup). The model (*Data.cs) acts as a Data Transfer Object (DTO). It is a data structure (without logic) that contains information for display and callbacks (Action) for buttons. The view (*Popup.cs) acts as a Passive View. It simply receives the DTO, renders the data, and binds callbacks to the buttons. It has no subscriptions or its own business logic.

**Dynamic Windows (Container + Observable Model)**
Used for windows where data can be updated while the window is open (e.g., LawPoliciesPopup). The model (*Data.cs) acts as an Observable Model. It contains data and an event (e.g., Action OnDataUpdated) that it triggers when the state changes. The view (*Popup.cs) acts as a Container / Presenter (Smart component). It subscribes to the model's events. Upon updating, it manages child elements (instantiates prefabs, calls their update methods).



# Управління Попапами
Цей модуль відповідає за логіку роботи всіх спливаючих вікон у грі. Архітектура побудована так, щоб UI був максимально відв'язаний від бізнес-логіки гри.


## Базові класи та управління
**BasePopup**: Базовий клас, який наслідують усі попапи. Він містить загальну логіку, притаманну всім спливаючим вікнам у системі.

**PopupController**: Відповідає за керування життєвим циклом вікон. Обробляє відкриття, закриття, анімації, а також логіку стеку попапів.


## Архітектурні підходи
Ми використовуємо два різних підходи для побудови попапів, залежно від того, чи змінюються дані всередині них у реальному часі.

**Статичні вікна (Passive View + DTO)**
Використовується для вікон, дані в яких генеруються один раз при відкритті і не змінюються (наприклад, AcceptOfferPopup). Модель (*Data.cs) виступає як Data Transfer Object (DTO). Це структура даних (без логіки), яка містить інформацію для відображення та колбеки (Action) для кнопок. В'юшка (*Popup.cs) виступає як Passive View. Вона просто отримує DTO, малює дані та прив'язує колбеки до кнопок. Не має жодних підписок чи власної бізнес-логіки.

**Динамічні вікна (Container + Observable Model)**
Використовується для вікон, де дані можуть оновлюватися, поки вікно відкрите (наприклад, LawPoliciesPopup). Модель (*Data.cs) виступає як Observable Model. Вона містить дані та подію (наприклад, Action OnDataUpdated), яку викликає при зміні стану. В'юшка (*Popup.cs) виступає як Container / Presenter (Розумний компонент). Вона підписується на події моделі. При оновленні вона керує дочірніми елементами (створює префаби, викликає їхні методи оновлення).