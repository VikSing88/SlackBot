using System;
using System.Collections.Generic;
using System.Text;

namespace TestApp
{
  #region Классы парсинга ответа для команды "pins.list"
  public class PinedMessage
  {
    public string ts { get; set; }
  }

  public class PinItem
  {
    public PinedMessage message { get; set; }
  }

  public class PinsResponse
  {
    public bool ok { get; set; }
    public List<PinItem> items { get; set; }
  }

  #endregion

  #region Классы парсинга ответа для команды "conversations.replies"
 
  public class ReplyMessage
  {
    public string ts { get; set; }
    public string user { get; set; }
    public string text { get; set; }
  }

  public class RepliesResponse
  {
    public bool ok { get; set; }
    public List<ReplyMessage> messages { get; set; }
    public bool has_more { get; set; }
  }

  #endregion
}
