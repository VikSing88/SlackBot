namespace SlackBot
{
  class SlackChannelInfo
  {
    public SlackChannelInfo(string channelID, int daysBeforeWarning, int daysBeforeUnpining)
    {
      ChannelID = channelID;
      DaysBeforeWarning = daysBeforeWarning;
      DaysBeforeUnpining = daysBeforeUnpining;
    }

    /// <summary>
    /// ID канала.
    /// </summary>
    public string ChannelID { get; set; }
    /// <summary>
    /// Количество дней до предупреждения.
    /// </summary>
    public int DaysBeforeWarning { get; set; }
    /// <summary>
    /// Количество дней до открепления (отпинивания) сообщения.
    /// </summary>
    public int DaysBeforeUnpining { get; set; }
  }
}
