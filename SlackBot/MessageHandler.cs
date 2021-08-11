using System.Linq;
using System.Threading.Tasks;
using SlackNet.Events;
using SlackNet.Blocks;
using SlackNet.WebApi;
using SlackNet;

namespace SlackBot
{
  public class MessageHandler : IEventHandler<MessageEvent>
  {
    private readonly ISlackApiClient slack;
    public MessageHandler(ISlackApiClient slack)
    {
      this.slack = slack;
    }

    public Task Handle(MessageEvent slackEvent)
    {
      Task.Run(() =>
      {
        if (slackEvent.ThreadTs == null) 
        {
          var chanel = slackEvent.Channel;
          var ts = slackEvent.Ts;
          slack.Pins.AddMessage(chanel, ts);
        }
      });
      return Task.CompletedTask;
    }
  }
}

