Це коренева папка з основними файлами гри. Все, що створюється безпосередньо розробниками, зберігається тут. Структура розбита на логічні розділи для забезпечення чистоти архітектури та масштабованості:
Modules - Тут знаходяться самостійні модулі гри (ігрові механіки, навігація, система спливаючих вікон тощо). Зазвичай модулі незалежні один від одного, але деякі (наприклад, Popups) можуть використовуватися іншими як інструменти.
Scenes - Папка з усіма ігровими сценами проекту
Services - На відміну від модулів, тут знаходяться глобальні сервіси, які не мають прямого відношення до UI і містять фундаментальну бізнес-логіку. Вони доступні для використання будь-яким модулем (наприклад, EconomyService, GameDataService, AudioService).
Shared - Тут знаходяться елементи, спільні для різних модулів гри, які не є складними системами чи сервісами. Це визначення ігрових ресурсів (ScriptableObjects), шрифти, глобальні префаби та спільні елементи UI.

This is the root folder containing the game’s core files. Everything created directly by the developers is stored here. The structure is divided into logical sections to ensure clean architecture and scalability:
Modules - This folder contains standalone game modules (game mechanics, navigation, popup system, etc.). Typically, modules are independent of one another, but some (such as Popups) can be used by others as tools.
Scenes - A folder containing all the game scenes for the project
Services - Unlike modules, this folder contains global services that are not directly related to the UI and contain fundamental business logic. They are available for use by any module (e.g., EconomyService, GameDataService, AudioService).
Shared - This folder contains elements shared by various game modules that are not complex systems or services. These include game resource definitions (ScriptableObjects), fonts, global prefabs, and shared UI elements.