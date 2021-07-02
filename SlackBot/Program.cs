using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TestApp;

namespace SlackBotAPI
{
  class SlackBot
  {
    #region Константы

    /// <summary>
    /// 
    /// </summary>
    const string slackApiLink = "https://slack.com/api/";

    /// <summary>
    /// Количество дней до предупреждения по умолчанию.
    /// </summary>
    const int daysBeforeWarningByDefault = 7;

    /// <summary>
    /// Количество дней до открепления (отпинивания) сообщения по умолчанию.
    /// </summary>
    const int daysBeforeUnpiningByDefault = 3;

    /// <summary>
    /// Текст предупреждения.
    /// </summary>
    const string WarningTextMessage = "Новых сообщений не было уже больше {0} дней. Закрываем консультацию?";

    /// <summary>
    /// Текст при откреплении (отпинивании) сообщения.
    /// </summary>
    const string UnpiningTextMessage = "Консультация закрыта.";

    /// <summary>
    /// Название эмодзи. 
    /// </summary>
    const string emojiName = "no_entry_sign";

    /// <summary>
    /// Токен бота.
    /// </summary>
    private static string botToken;

    /// <summary>
    /// ID канала.
    /// </summary>
    private static string channelID;

    /// <summary>
    /// Количество дней до предупреждения.
    /// </summary>
    private static int daysBeforeWarning;

    /// <summary>
    /// Количество дней до открепления (отпинивания) сообщения.
    /// </summary>
    private static int daysBeforeUnpining;

    /// <summary>
    /// ID бота.
    /// </summary>
    private static string botID;

    /// <summary>
    /// Действие, которое надо совершить над запиненным сообщением.
    /// </summary>
    private enum MessageAction
    {
      NeedUnpin,
      NeedWarning,
      DoNothing
    }

    #endregion

    #region Вложенные типы

    public class RemovePinsMethod
    {
      public string channel { get; set; }
      public string timestamp { get; set; }
    }
    public class AddReactionMethod
    {
      public string channel { get; set; }
      public string timestamp { get; set; }
      public string name { get; set; }
    }

    public class PostMessageMethod
    {
      public string channel { get; set; }
      public string text { get; set; }
      public string thread_ts { get; set; }
    }

    /// <summary>
    /// Информация о запиненном сообщении.
    /// </summary>
    private class MessageInfo
    {
      public string timeStamp;
      public MessageAction action;
    }

    #endregion

    #region Поля и свойства

    private static HttpClient client;

    #endregion

    #region Методы

    /// <summary>
    /// Попытаться конвертировать строку в число.
    /// </summary>
    /// <param name="paramName">Имя параметра.</param>
    /// <param name="value">Конвертируемое значение.</param>
    /// <param name="defaultValue">Значение по умолчанию.</param>
    /// <returns></returns>
    private static int TryConvertStringToInt(string paramName, string value, int defaultValue)
    {
      int resultValue;
      try
      { 
        resultValue = Convert.ToInt32(value);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Произошла ошибка при чтении параметра {paramName}: {ex.Message}. " +
          $"Взято значение по умолчанию {defaultValue}.");
        resultValue = defaultValue;
      }

      return resultValue;
    }

    /// <summary>
    /// Прочитать конфиг.
    /// </summary>
    private static void ReadConfig()
    {
      try
      {
        var config = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json")
          .Build();

        botToken = config["BotToken"];
        channelID = config["ChannelID"];
        botID = config["BotID"];
        daysBeforeWarning = TryConvertStringToInt(nameof(daysBeforeWarning), config["DaysBeforeWarning"], 
          daysBeforeWarningByDefault);
        daysBeforeUnpining = TryConvertStringToInt(nameof(daysBeforeUnpining), config["DaysBeforeUnpining"],
          daysBeforeUnpiningByDefault);
      }
      catch(Exception ex)
      {
        Console.WriteLine($"Ошибка чтения конфигурационного файла: {ex.Message}");
        throw;
      }
    }

    private static void ConnectToSlack()
    {
      var webProxy = new WebProxy();
      webProxy.UseDefaultCredentials = true;
      try
      {
        client = new HttpClient(new HttpClientHandler() { Proxy = webProxy });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка при подключении к slack: {ex.Message}");
        throw;
      }
    }

    public static void Main()
    {
      try
      {
        Console.WriteLine("Работа бота начата.");

        ReadConfig();
        ConnectToSlack();
        ProcessPinsList();
        Console.ReadKey();
      }
      catch
      {
        Console.WriteLine("Работа бота завершилась из-за ошибок.");
      }
    }

    /// <summary>
    /// Обработать список запиненных сообщений.
    /// </summary>
    private static async void ProcessPinsList()
    {
      try
      {
        var response = await client.GetAsync(slackApiLink + "pins.list?channel=" + channelID);
        var responseJson = await response.Content.ReadAsStringAsync();
        var list = JsonConvert.DeserializeObject<PinsResponse>(responseJson);
        var oldMessageTSList = GetOldMessageList(list.items);
        ReplyMessageInOldThreads(oldMessageTSList);
      }
      catch(Exception ex)
      {
        Console.WriteLine($"Обработка сообщений завершилась с ошибкой: {ex.Message}");
        throw;
      }
    }
    
    /// <summary>
    /// Отправить сообщение в тред.
    /// </summary>
    /// <param name="messageInfos">Список запиненных сообщений.</param>
    private static void ReplyMessageInOldThreads(List<MessageInfo> messageInfos)
    {
      foreach (MessageInfo messageData in messageInfos)
      {
        if (messageData.action == MessageAction.NeedWarning)
        {
          SendMessage(String.Format(WarningTextMessage, daysBeforeWarning), messageData.timeStamp);
        }
        else if (messageData.action == MessageAction.NeedUnpin)
        {
          SendMessage(UnpiningTextMessage, messageData.timeStamp);
          AddEmoji(messageData.timeStamp);
          UnpinMessage(messageData.timeStamp);
        }
      }
    }

    /// <summary>
    /// Открепить сообщение.
    /// </summary>
    /// <param name="messageTimeStamp">Отметка времени закрепленного сообщения.</param>
    private static async void UnpinMessage(string messageTimeStamp)
    {
      var msg = new RemovePinsMethod
      {
        channel = channelID,
        timestamp = messageTimeStamp
      };

      var content = JsonConvert.SerializeObject(msg);
      var httpContent = new StringContent(
          content,
          Encoding.UTF8,
          "application/json"
      );

      var response = await client.PostAsync(slackApiLink + "pins.remove", httpContent);
      var responseJson = await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Получить список старых закрепленных сообщений.
    /// </summary>
    /// <param name="pinedMessages">Полный список закрепленных сообщений.</param>
    /// <returns>Список закрепленных сообщений, с момента создания которых прошло больше DaysCountBeforeWarning дней.</returns>
    private static List<MessageInfo> GetOldMessageList(List<PinItem> pinedMessages)
    {
      var oldPinedMessageList = new List<MessageInfo>();
      foreach (PinItem pinedMessage in pinedMessages)
      {
        if (IsOldPinedMessage(pinedMessage.message.ts, Math.Min(daysBeforeWarning, daysBeforeUnpining)))
        {
          MessageAction msgAction = Task.Run(() => GetPinedMessageAction(pinedMessage.message.ts)).Result;
          oldPinedMessageList.Add(new MessageInfo()
          {
            timeStamp = pinedMessage.message.ts,
            action = msgAction
          });
        }
      }
      return oldPinedMessageList;
    }

    /// <summary>
    /// Определить действие, которое необходимо с закрепленным сообщением.
    /// </summary>
    /// <param name="messageTimeStamp">Отметка времени запиненного сообщения.</param>
    /// <returns>Действие, которое необходимо с закрепленным сообщением.</returns>
    private static async Task<MessageAction> GetPinedMessageAction(string messageTimeStamp)
    {
      var response = await client.GetAsync(slackApiLink + "conversations.replies?channel=" + channelID + "& ts=" + messageTimeStamp);
      var responseJson = await response.Content.ReadAsStringAsync();
      var responseObject = JsonConvert.DeserializeObject<RepliesResponse>(responseJson);

      var latest_message_number = responseObject.messages.Count - 1;
      return DefineActionByDateAndAuthorOfMessage(responseObject.messages[latest_message_number].ts,
        responseObject.messages[latest_message_number].user, responseObject.messages[latest_message_number].text
        );
    }

    /// <summary>
    /// Добавить эмодзи на открепляемое сообщение.
    /// </summary>
    /// <param name="messageTimeStamp">Отметка времени открепляемого сообщения.</param>
    private static async void AddEmoji(string messageTimeStamp)
    {
      var msg = new AddReactionMethod
      {
        channel = channelID,
        timestamp = messageTimeStamp,
        name = emojiName
      };

      var content = JsonConvert.SerializeObject(msg);
      var httpContent = new StringContent(
          content,
          Encoding.UTF8,
          "application/json"
      );

      var response = await client.PostAsync(slackApiLink + "reactions.add", httpContent);
      var responseJson = await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Определить действие над закрепленным сообщением по дате и автору последнего ответа.
    /// </summary>
    /// <param name="messageTimeStamp">Отметка времени последнего сообщения из треда.</param>
    /// <param name="userID">ID автора последнего сообщения из треда. </param>
    /// <returns>Действие над закрепленным сообщением.</returns>
    private static MessageAction DefineActionByDateAndAuthorOfMessage(string messageTimeStamp, string userID,
      string text)
    {
      if (messageTimeStamp != null)      
      {
        if ((userID != botID) & (IsOldPinedMessage(messageTimeStamp, daysBeforeWarning)))
        {
          return MessageAction.NeedWarning;
        }
        else 
        if ((userID == botID) & (IsOldPinedMessage(messageTimeStamp, daysBeforeUnpining)))
        {
          return MessageAction.NeedUnpin;
        }
      }
      return MessageAction.DoNothing;
    }

    /// <summary>
    /// Определить, является ли закрепленное сообщение старым.
    /// </summary>
    /// <param name="messageTimeStamp">Отметка времени сообщения.</param>
    /// <param name="DayCount">Период (в днях), по прошествию которого следует считать сообщение старым.</param>
    /// <returns>Признак, является ли закрепленное сообщение старым.</returns>
    private static bool IsOldPinedMessage(string messageTimeStamp, int DayCount)
    {
      double timeStampWithoutMicroSeconds =
          Convert.ToDouble(messageTimeStamp.Substring(0, messageTimeStamp.IndexOf('.')));
      DateTime messageDate = ConvertUnixTimeStampToDateTime(timeStampWithoutMicroSeconds);
      return messageDate.AddDays(DayCount) < DateTime.Now;
    }

    /// <summary>
    /// Конвертировать отметку времени в тип DateTime.
    /// </summary>
    /// <param name="unixTimeStamp">Отметка времени.</param>
    /// <returns>Дата и время.</returns>
    public static DateTime ConvertUnixTimeStampToDateTime(double unixTimeStamp)
    {
      var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
      dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
      return dateTime;
    }

    /// <summary>
    /// Отправить сообщение в тред закрепленного сообщения.
    /// </summary>
    /// <param name="textMessage">Текст отправляемого сообщения.</param>
    /// <param name="messageTimeStamp">Отметка времени закрепленного сообщения.</param>
    private static async void SendMessage(string textMessage, string messageTimeStamp)
    {
      var msg = new PostMessageMethod
      {
        channel = channelID,
        text = textMessage,
        thread_ts = messageTimeStamp
      };

      var content = JsonConvert.SerializeObject(msg);
      var httpContent = new StringContent(
          content,
          Encoding.UTF8,
          "application/json"
      );

      var response = await client.PostAsync(slackApiLink + "chat.postMessage", httpContent);
      var responseJson = await response.Content.ReadAsStringAsync();
    }
    #endregion
  }
}