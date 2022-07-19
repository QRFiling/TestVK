using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using VkNet;
using VkNet.AudioBypassService.Extensions;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace TestVK
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        async void button1_Click(object sender, EventArgs e)
        {
            //создаём ссылку на список постов с сабреддита в json формате 
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            string url = "https://www.reddit.com/r/Minecraft/hot/.json?limit=" + (int)numericUpDown1.Value;

            //создаём запрос и ждём ответ с сервера
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

            //читаем ответ
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                //приводим простой текст в формате json к экзэмпляру класса RedditResponce
                RedditResponce reddit = JsonConvert.DeserializeObject<RedditResponce>(reader.ReadToEnd());

                //перебираем посты и для каждого поста, который подходит под наше условие, создаём дубликат в нашем паблике
                foreach (var item in reddit.data.children)
                {
                    //если в посте есть класс preview (в нём ссылка на картинку) и пост не закреплён админами в ленте
                    if (item.data.preview != null && !item.data.stickied)
                    {
                        //достаём ссылку на картинку (немного её видоизменяем для корректной работы)
                        string image = item.data.preview.images[0].source.url.Replace("amp;", "");
                        //достаём ссылку на оригинальный пост
                        string copyright = "reddit.com/" + item.data.permalink;

                        //достаём и тут же переводим на русский язык заголовок поста
                        string title = Translate(item.data.title);
                        //постим всю полученную инфу в ВК
                        PostToVK(title, image, copyright);
                    }
                }
            }

            //уведомление о том, что всё кайфово
            MessageBox.Show("Готово", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //взял со stackoverflow. Простое использование api гугл переводчика
        string Translate(string word)
        {
            var toLanguage = "ru";
            var fromLanguage = "en";
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(word)}";

            var webClient = new WebClient() { Encoding = Encoding.UTF8 };
            var result = webClient.DownloadString(url);

            try
            {
                result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
                return result;
            }
            catch { return word; }
        }

        async void PostToVK(string title, string imageURL, string copyright)
        {
            long groupID = 0; //тут айди паблика (можно найти в ссылке на группу. Напр: https://vk.com/public123 - "123" это айди)

            //создаём список сервисов для авторизации (без этого зарегаться не получится)
            var services = new ServiceCollection();
            services.AddAudioBypass();

            VkApi vk = new VkApi(services);
            //входим в акк в ВК
            vk.Authorize(new ApiAuthParams()
            {
                Login = "******", //логин вашего аккаунта
                Password = "******", //пароль вашего аккаунта
                ApplicationId = 0, //айди приложения ВК. На нём и будет работать вход и любые действия от лица аккаунта (делаем тут: https://vk.com/apps?act=manage)
                Settings = Settings.All //доступ ко всем дейстриям аккаунта, какие только можно
            });

            //получаем сервер ВК для загрузки фото конкретно для нашего паблика
            var uploadServer = vk.Photo.GetWallUploadServer(groupID);
            //заливаем на полученный сервер картинку, ссылку на которую мы получили с реддит-поста
            string response = await UploadFile(uploadServer.UploadUrl, imageURL, "png");
            //получаем объект с фотографией, который уже можно прикрепить к ВК посту
            var attachment = vk.Photo.SaveWallPhoto(response, null, (ulong)groupID);

            //постим
            vk.Wall.Post(new WallPostParams()
            {
                OwnerId = -groupID, //айди, на чью стену нужно сделать пост. Если это пользователь, передаём просто айди, а если это паблик (как в нашем случае) передаём айди со знаком минус
                FromGroup = true, //говорим, что пост должен быть от имени группы
                Message = title, //заголовок поста
                Attachments = attachment, //прикрепляем картинку
                Copyright = copyright //добавляем источник (сслыка на оригинальный реддит-пост)
            });
        }

        //метод, который заливает фото по сслыке на другой сервер (брал отсюда: https://github.com/vknet/vk/discussions/1104)
        async Task<string> UploadFile(string serverUrl, string file, string fileExtension)
        {
            using (var client = new HttpClient())
            {
                using (var webClient = new WebClient())
                {
                    var requestContent = new MultipartFormDataContent();
                    var content = new ByteArrayContent(webClient.DownloadData(file));
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    requestContent.Add(content, "file", $"file.{fileExtension}");

                    var response = client.PostAsync(serverUrl, requestContent).Result;
                    return Encoding.Default.GetString(await response.Content.ReadAsByteArrayAsync());
                }
            }
        }
    }
}
