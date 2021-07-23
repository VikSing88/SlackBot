# SlackBot

Бот для слака, который умеет отпинивать сообщения и скачивать треды

## Установка

В настройках бота во вкладке Socket Mode нужно включить одноименный мод, создать App-level Token на вкладке Basic Information с разрешением connections:write, полученный токен вставить в appsettings.json в BotLevelToken.  
На вкладке Interactivity & Shortcuts создать свой Shortcut для сообщений и придумать Callback Id и так же вставить его в appsettings.json.  
После этого в appsettings.json в категорию PathToDownloadDirectory указать желаемый путь для скачивания тредов.

## Необходимые разрешения бота

pins:read, pins:write, channels:history, groups:history, im:history, mpim:history, reactions:write, chat:write, users:read
