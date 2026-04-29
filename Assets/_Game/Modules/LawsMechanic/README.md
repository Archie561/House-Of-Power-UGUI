# Управління механікою законів
Цей модуль відповідає за логіку обчислення і відображення всіх параметрів у механіці законів.

## Основні елементи
**LawConfig:** Містить загальні налаштування механіки, які можна легко змінити для тестування (максиальне число законів, ціна поповнення законів, ціна досвіду політик і т.д.) Також містить функціонал для конвертації даних законів із TSV таблиці у обʼєкт LawData. 

**LawLogicController:** Відповідає за логіку і обчислення всіх даних механіки. Оновлює значення досвіду для політик, вираховує рівень на основі досвіду, зберігає і модифікує кількість доступних для прийняття законів, зберігає поточний активний закон гравця, обробляє прийняття закону, їх подачу і т.д. Керує таймером і реалізовує інтерфейс **IResourceLogicHandler** для обробки тразакцій для ресурсів політики. Містить публічні методи для надання view всіх необхідних даних.

**LawViewController:** Отримує сирі дані від логіки і передає їх у View. Відповідає за оновлення інтерфейсу і містить методи для обробки взаємодії користувача з UI. Не бере участі у безпосередній зміні даних гравця, натомість сповіщає про виконану дію логіку (LawLogicController), яка вже виконує потрібну операцію.

## Інші елементи
До інших елементів модуля входять:

**Допоміжні класи (Helpers):** Клас для парсингу даних з TSV таблиці у обʼєкти LawData (**LawDataParser**)

**Класи-моделі (Models):** Містять чисті дані про об'єкт (**LawData**, **DetailedPolicyProgressData**) 

**Класи-в'ю (Views):** Пасивні компоненти, які відповідають виключно за відображення переданих їм даних в UI (**DetailedPolicyProgressView**, **LawView**, **PolicyProgressView**).



# Law Mechanics Management
This module is responsible for the logic, calculation, and display of all parameters within the law mechanics.

## Core Elements
**LawConfig**: Contains general mechanics settings that can be easily modified for testing (maximum number of laws, law restock price, policy experience cost, etc.). It also includes functionality for converting law data from a TSV table into a LawData object.

**LawLogicController**: Responsible for the logic and calculation of all mechanics data. It updates experience values for policies, calculates the level based on experience, stores and modifies the number of laws available for adoption, stores the player's current active law, and handles law adoption, proposals, etc. It manages the timer and implements the **IResourceLogicHandler** interface to process transactions for policy resources. It contains public methods to provide the view with all necessary data.

**LawViewController**: Receives raw data from the logic and passes it to the View. It is responsible for updating the interface and contains methods for handling user interactions with the UI. It does not directly modify player data; instead, it notifies the logic (LawLogicController) of the performed action, which then executes the required operation.

## Other Elements
Other elements of the module include:

**Helper Classes (Helpers):** A class for parsing data from a TSV table into LawData objects (**LawDataParser**).

**Model Classes (Models):** Contain pure data about the object (**LawData**, **DetailedPolicyProgressData**).

**View Classes (Views):** Passive components strictly responsible for displaying the data passed to them in the UI (**DetailedPolicyProgressView**, **LawView**, **PolicyProgressView**).