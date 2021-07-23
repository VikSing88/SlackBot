# SlackBot

Бот для слака, который умеет отпинивать закреплненные сообщения спустя заданный промежуток дней, скачивать треды по нажатию на кнопку, доступную в раскрывающемся меню сообщения

## Конфигурация бота

На сайте конфигурации бота перейти на вкладку Socket Mode и активировать одноименный мод. Создать App-level Token на вкладке Basic Information с разрешением connections:write.  
На вкладке Interactivity & Shortcuts создать свой Shortcut для сообщений и придумать ему Callback Id.
Выдать боту следующие необходимые для работы разрешения на вкладке OAuth & Permissions:  
pins:read, pins:write, channels:history, groups:history, im:history, mpim:history, reactions:write, chat:write, users:read;

## Конфигурация приложения

В файле appsettings.json есть следующие настройки, которые нужно заполнить:

BotToken: токен бота, который можно получить на вкладке OAuth & Permissions  

BotLevelToken: еще один токен бота, который необходим для работы Socket mod`a. Найти/создать можно на вкладке Basic  Information в категории App-Level Tokens  

BotId: id бота, можно найти в slack`e  

ShortcutCallbackID: Callback Id, который был создан в конфигурации бота выше  

PathToDownloadDirectory: Путь по которому бот будет скачивать треды  

Channels: массив каналов, в которых бот будет отпинивать устаревшие сообщения:
- ChannelID: Id канала за которым следит бот  

- DaysBeforeWarning: количество дней через которое бот отправит оповещение о устаревшем треде  

- DaysBeforeUnpining: количество дней через которое бот открепит закрепленный тред(открепление происходит только после истечения DaysBeforeWarning)

## Example

![image](https://user-images.githubusercontent.com/55059498/126780340-452a4b0a-2d10-4069-8569-81c8c6a12fce.png)
