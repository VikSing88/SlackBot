using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TestApp;

namespace TestingSlackAPI
{
  class TestBot
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
    const string WarningTextMessage = "Сейчас как отпиню!";

    /// <summary>
    /// Текст при откреплении (отпинивании) сообщения.
    /// </summary>
    const string UnpiningTextMessage = "Все, отпиниваю!";

    /// <summary>
    /// Название эмодзи. 
    /// </summary>
    const string emojiName = "no_entry_sign";

    /// <summary>
    /// Количество дней до предупреждения.
    /// </summary>
    const int DaysCountBeforeWarning = 7;

    /// <summary>
    /// Количество дней до открепления (отпинивания) сообщения.
    /// </summary>
    const int DaysCountBeforeUnpining = 3;

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
      Nothing
    }

    #endregion

    #region Вложенные типы

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

    private static WebClient client;

    #endregion

    #region Методы

    public static void Main()
    {
      client = new WebClient();
      var webProxy = new WebProxy();
      webProxy.UseDefaultCredentials = true;
      client.Proxy = webProxy;

      ProcessPinsList();
    }

    /// <summary>
    /// Обработать список запиненных сообщений.
    /// </summary>
    private static void ProcessPinsList()
    {
      var dataForRequest = new NameValueCollection();
      dataForRequest["token"] = botToken;
      dataForRequest["channel"] = channelID;

      var response = client.UploadValues(slackApiLink + "pins.list", postRequestType, dataForRequest);
      string responseInString = Encoding.UTF8.GetString(response);
      var list = JsonConvert.DeserializeObject<PinsResponse>(responseInString);
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
          SendMessage(WarningTextMessage, messageData.timeStamp);
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
    private static void UnpinMessage(string messageTimeStamp)
    {
      var dataForRequest = new NameValueCollection();
      dataForRequest["token"] = botToken;
      dataForRequest["channel"] = channelID;
      dataForRequest["timestamp"] = messageTimeStamp;

      var response = client.UploadValues(slackApiLink + "pins.remove", postRequestType,
        dataForRequest);
      string responseInString = Encoding.UTF8.GetString(response);
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
        if (IsOldPinedMessage(pinedMessage.message.ts, DaysCountBeforeWarning))
        {
          oldPinedMessageList.Add(new MessageInfo()
          {
            timeStamp = pinedMessage.message.ts,
            action = GetPinedMessageAction(pinedMessage.message.ts)
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
    private static MessageAction GetPinedMessageAction(string messageTimeStamp)
    {
      // Получаем список ответов из треда закрепленного сообщения.
      var dataForRequest = new NameValueCollection();
      dataForRequest["token"] = botToken;
      dataForRequest["channel"] = channelID;
      dataForRequest["ts"] = messageTimeStamp;

      var response = client.UploadValues(slackApiLink + "conversations.replies", postRequestType,
        dataForRequest);
      string responseInString = Encoding.UTF8.GetString(response);
      var responseObject = JsonConvert.DeserializeObject<RepliesResponse>(responseInString);

      var latest_message_number = responseObject.messages.Count - 1;
      return DefineActionByDateAndAuthorOfMessage(responseObject.messages[latest_message_number].ts,
        responseObject.messages[latest_message_number].user);
    }

    /// <summary>
    /// Добавить эмодзи на открепляемое сообщение.
    /// </summary>
    /// <param name="messageTimeStamp">Отметка времени открепляемого сообщения.</param>
    private static void AddEmoji(string messageTimeStamp)
    {
      var dataForRequest = new NameValueCollection();
      dataForRequest["token"] = botToken;
      dataForRequest["channel"] = channelID;
      dataForRequest["timestamp"] = messageTimeStamp;
      dataForRequest["name"] = emojiName;

      var response = client.UploadValues(slackApiLink + "reactions.add", postRequestType,
        dataForRequest);
      string responseInString = Encoding.UTF8.GetString(response);
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
        if ((userID != BotID) & (IsOldPinedMessage(messageTimeStamp, DaysCountBeforeWarning)))
        {
          return MessageAction.NeedWarning;
        }
        else
        if ((userID == BotID) & (IsOldPinedMessage(messageTimeStamp, DaysCountBeforeUnpining)))
        {
          return MessageAction.NeedUnpin;
        }
      }
      return MessageAction.Nothing;
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
      return messageDate.AddMinutes(DayCount) < DateTime.Now;
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
    private static void SendMessage(string textMessage, string messageTimeStamp)
    {
      var dataForMessage = new NameValueCollection();
      dataForMessage["token"] = botToken;
      dataForMessage["channel"] = channelID;
      dataForMessage["text"] = textMessage;
      dataForMessage["thread_ts"] = messageTimeStamp;

      var response = client.UploadValues(slackApiLink + "chat.postMessage", postRequestType,
        dataForMessage);
      string responseInString = Encoding.UTF8.GetString(response);
      Console.WriteLine(responseInString);
    }
  }

  #endregion
}