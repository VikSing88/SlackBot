using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TestApp;

namespace SlackBotAPI
{
  class SlackBot
  {
    #region Константы

    /// <summary>
    /// Токен бота.
    /// </summary>
    const string botToken = "xoxb-1593440270915-1578574815463-wjHDhugVnTmgwgLjFeJDlRvz";

    /// <summary>
    /// ID канала.
    /// </summary>
    const string channelID = "C01HMBYB4MS";

    /// <summary>
    /// 
    /// </summary>
    const string slackApiLink = "https://slack.com/api/";

    /// <summary>
    /// Тип отправляемого запроса.
    /// </summary>
    const string postRequestType = "POST";

    /// <summary>
    /// Текст предупреждения.
    /// </summary>
    const string WarningTextMessage = "Новых сообщений не было уже {0} дней. Закрываем консультацию?";

    /// <summary>
    /// Текст при откреплении (отпинивании) сообщения.
    /// </summary>
    const string UnpiningTextMessage = "Консультация закрыта.";

    /// <summary>
    /// Название эмодзи. 
    /// </summary>
    const string emojiName = "no_entry_sign";

    /// <summary>
    /// Количество дней до предупреждения.
    /// </summary>
    const int DaysBeforeWarning = 7;

    /// <summary>
    /// Количество дней до открепления (отпинивания) сообщения.
    /// </summary>
    const int DaysBeforeUnpining = 3;

    /// <summary>
    /// ID бота.
    /// </summary>
    const string BotID = "U01H0GWPZDM";

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

    public static void Main()
    {
      var webProxy = new WebProxy();
      webProxy.UseDefaultCredentials = true;
      client = new HttpClient(new HttpClientHandler() { Proxy = webProxy });
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);

      ProcessPinsList();
      Console.ReadKey();
    }

    /// <summary>
    /// Обработать список запиненных сообщений.
    /// </summary>
    private static async void ProcessPinsList()
    {
      var response = await client.GetAsync(slackApiLink + "pins.list?channel=C01HMBYB4MS");
      var responseJson = await response.Content.ReadAsStringAsync();
      var list = JsonConvert.DeserializeObject<PinsResponse>(responseJson);
      var oldMessageTSList = GetOldMessageList(list.items);
      ReplyMessageInOldThreads(oldMessageTSList);
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
          SendMessage(String.Format(WarningTextMessage, DaysBeforeWarning), messageData.timeStamp);
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
      var oldPinedMessageList = new List<MessageInfo> ();
      foreach (PinItem pinedMessage in pinedMessages)
      {
        if (IsOldPinedMessage(pinedMessage.message.ts, DaysCountBeforeWarning))
        {
          MessageAction msgAction = Task.Run(() => GetPinedMessageAction(pinedMessage.message.ts)).Result;
          oldPinedMessageList.Add(new MessageInfo() { timeStamp = pinedMessage.message.ts , 
            action = msgAction }) ;
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
      var response = await client.GetAsync(slackApiLink + "conversations.replies?channel=C01HMBYB4MS&ts=" + messageTimeStamp);
      var responseJson = await response.Content.ReadAsStringAsync();
      var responseObject = JsonConvert.DeserializeObject<RepliesResponse>(responseJson);

      var latest_message_number = responseObject.messages.Count - 1;
      return DefineActionByDateAndAuthorOfMessage(responseObject.messages[latest_message_number].ts,
        responseObject.messages[latest_message_number].user);
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
    private static MessageAction DefineActionByDateAndAuthorOfMessage(string messageTimeStamp, string userID)
    {
      if (messageTimeStamp != null)      
      {
        if ((userID != BotID) & (IsOldPinedMessage(messageTimeStamp, DaysBeforeWarning)))
        {
          return MessageAction.NeedWarning;
        }
        else 
        if ((userID == BotID) & (IsOldPinedMessage(messageTimeStamp, DaysBeforeUnpining)))
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