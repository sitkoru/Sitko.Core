using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Sitko.Core.Web.Components
{
    public class Flasher
    {
        private readonly HttpContext _httpContext;
        private const string Key = "messages";

        public Flasher(IHttpContextAccessor accessor)
        {
            _httpContext = accessor.HttpContext;
        }

        public async Task AddFlashAsync(FlashType type, string title, string text)
        {
            await _httpContext.Session.LoadAsync();
            var message = new FlashMessage(type, title, text);
            var messages = await GetMessagesAsync();
            messages.Add(message);
            await SaveMessagesAsync(messages);
        }

        private async Task SaveMessagesAsync(IEnumerable<FlashMessage> messages)
        {
            await _httpContext.Session.LoadAsync();
            _httpContext.Session.SetString(Key, JsonConvert.SerializeObject(messages));
        }

        private async Task<List<FlashMessage>> GetMessagesAsync(FlashType? type = null, bool delete = false)
        {
            await _httpContext.Session.LoadAsync();
            var json = _httpContext.Session.GetString(Key);
            if (!string.IsNullOrEmpty(json))
            {
                var messages = JsonConvert.DeserializeObject<List<FlashMessage>>(json);
                var result = messages.Where(m => type == null || m.Type == type.Value).ToList();
                if (delete)
                {
                    foreach (var message in result)
                    {
                        messages.Remove(message);
                    }
                    await SaveMessagesAsync(messages);
                }
                return result;
            }
            return new List<FlashMessage>();
        }

        public async Task<List<FlashMessage>> TakeMessageAsync(FlashType? type = null)
        {
            return await GetMessagesAsync(type, true);
        }
    }

    public class FlashMessage
    {
        public FlashType Type { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }

        public FlashMessage(FlashType type, string title, string text)
        {
            Type = type;
            Title = title;
            Text = text;
        }
    }

    public enum FlashType
    {
        Success = 1,
        Warning = 2,
        Error = 3,
        Info = 4
    }
}
