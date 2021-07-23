using SlackNet.Interaction;
using System.Threading.Tasks;

namespace SlackBot.DownloadFunctionality
{
  class DownloadHandler : IMessageShortcutHandler
  {
    private readonly IDownloader downloader;
    private readonly string botToken;
    private readonly string notification = "Загрузка началась";
    public DownloadHandler(string botToken, IDownloader downloader)
    {
      this.botToken = botToken;
      this.downloader = downloader;
    }

    public Task Handle(MessageShortcut request)
    {
      Task.Run(() =>
      {
        if (request.Message.ThreadTs != null)
        {
          SlackBot.PostEphemeralMessageToUser(notification, request.User.Id, request.Channel.Id);
          var thread = SlackBot.GetThread(request.Message.ThreadTs, request.Channel.Id);
          var pathToThread = downloader.DownloadThread(thread, request.Channel.Name, botToken);
          var message = $"Тред находится по ссылке: {pathToThread}";
          SlackBot.PostEphemeralMessageToUser(message, request.User.Id, request.Channel.Id);
        }
      });
      return Task.CompletedTask;
    }
  }
}