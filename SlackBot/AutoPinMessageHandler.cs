using System.Linq;
using System.Threading.Tasks;
using SlackNet.Events;
using SlackNet.Blocks;
using SlackNet.WebApi;
using SlackNet;
using System.Collections.Generic;
using System;

namespace SlackBot
{
  class AutoPinMessageHandler : IEventHandler<MessageEvent>
  {
    private readonly ISlackApiClient slack;
    private readonly List<SlackChannelInfo> slackChannelsInfo;

    public AutoPinMessageHandler(ISlackApiClient slack, List<SlackChannelInfo> slackChannelsInfo)
    {
      this.slack = slack;
      this.slackChannelsInfo = slackChannelsInfo;
    }

    public Task Handle(MessageEvent newMessageEvent)
    {
      if (newMessageEvent.ThreadTs == null)
      {
        var chanel = newMessageEvent.Channel;
        var ts = newMessageEvent.Ts;
        foreach (var chanelInfo in slackChannelsInfo)
        {
          if ((chanel == chanelInfo.ChannelID) && chanelInfo.AutoPinNewMessage)
          {
            if (!string.IsNullOrEmpty(chanelInfo.WelcomeMessage))
            {
              slack.Chat.PostEphemeralMessageToUser(chanelInfo.WelcomeMessage, newMessageEvent.User, chanel);
            }
            return slack.Pins.AddMessage(chanel, ts);
          }
        }
      }
      return Task.CompletedTask;
    }
  }
}

