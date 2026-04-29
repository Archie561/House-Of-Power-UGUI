# Trade Mechanics Management
This module is responsible for the logic of calculating and displaying all parameters in the trade mechanics.

## Core Elements
**TradeConfig:** Contains general mechanics settings that can be easily modified for testing (offer refresh time and price, offer generation parameters, storage upgrade parameters).

**TradeLogicController:** Responsible for the logic and calculation of all mechanics data. It handles the generation and execution of offers, timer logic, adding and deducting resources, etc. It implements the **IResourceLogicHandler** interface, defining the transaction logic for trade-type resources. It contains methods for passing mechanics data to the View (including data from the Game Data Service) but does not interact directly with View components in any way.

**TradeViewController:** Receives raw data from the logic and passes it to the View. It is responsible for updating the interface and contains methods for handling user interaction with the UI. It does not participate in directly changing the player's data; instead, it notifies the logic (TradeLogicController) of the performed action, which then executes the required operation.

## Other Elements
Other elements of the module include:

**Helper Classes (Helpers):** Classes for generating offers and calculating the cost of storage upgrades (TradeOfferGenerator, StorageUpgradeCalculator).

**Model Classes (Models):** Contain pure data about an object without logic (TradeOfferData).

**View Classes (Views):** Passive components strictly responsible for rendering the data passed to them in the UI (TradeOfferView, ResourceRowView).



# Управління механікою трейдів
Цей модуль відповідає за логіку обчислення і відображення всіх параметрів у механіці трейдів.

## Основні елементи
**TradeConfig:** Містить загальні налаштування механіки, які можна легко змінити для тестування (час і ціна оновлення оферів, параметри генерації оферів, параметри покращення сховища).

**TradeLogicController:** Відповідає за логіку і обчислення всіх даних механіки. Обробляє генерацію і виконання оферів, логіку таймера, поповнення та списання ресурсів тощо. Реалізовує інтерфейс **IResourceLogicHandler**, де визначає логіку транзакцій для ресурсів типу трейду. Містить методи для передачі даних механіки у View (в тому числі даних із Game Data Service), але ніяк безпосередньо не взаємодіє із View-компонентами.

**TradeViewController:** Отримує сирі дані від логіки і передає їх у View. Відповідає за оновлення інтерфейсу і містить методи для обробки взаємодії користувача з UI. Не бере участі у безпосередній зміні даних гравця, натомість сповіщає про виконану дію логіку (TradeLogicController), яка вже виконує потрібну операцію.

## Інші елементи
До інших елементів модуля входять:

**Допоміжні класи (Helpers):** Класи для генерації оферів і обрахунку вартості покращення сховища (TradeOfferGenerator, StorageUpgradeCalculator).

**Класи-моделі (Models):** Містять чисті дані про об'єкт без логіки (TradeOfferData).

**Класи-в'ю (Views):** Пасивні компоненти, які відповідають виключно за відображення переданих їм даних в UI (TradeOfferView, ResourceRowView).