using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using Newtonsoft.Json;

namespace VkStatDotTest
{
    class Program
    {
        static VkApi vkApi = new VkNet.VkApi();
        static ulong AppId = 6786680;
        static Dictionary<char, double> Statistic;

        static void Main(string[] args)
        {
            bool useToken = false;
            VkNet.Model.WallGetObject posts;
            
            if (!String.IsNullOrEmpty(Properties.Settings.Default.AccessToken))
            {
                Console.WriteLine("Использовать прошлые данные для входа? y/n");
                if (Console.ReadKey().KeyChar == 'y')
                {
                    useToken = true;
                    Console.WriteLine("\r\nПопытка входа по предыдущему токену");
                }
            }
            while (!Login(useToken))
            {
                Console.WriteLine("Попробуй еще раз");
                useToken = false;
            }
            //vabe access token to app settings
            Properties.Settings.Default.AccessToken = vkApi.Token;
            Properties.Settings.Default.Save();

            Console.WriteLine($"Успешный вход как {vkApi.Account.GetProfileInfo().FirstName} {vkApi.Account.GetProfileInfo().LastName}");

            //get statistic and post it
            while (true)
            {
                Console.WriteLine("Введи ID аккаунта для вывода статистики или не пиши ничего, чтобы выйти");
                var targetString = Console.ReadLine();
                if (String.IsNullOrEmpty(targetString))
                    break;
                var target = vkApi.Utils.ResolveScreenName(targetString);
                if (target == null)
                {
                    Console.WriteLine("Пользователь/группа не найдены");
                    continue;
                }

                try
                {
                    posts = vkApi.Wall.Get(new VkNet.Model.RequestParams.WallGetParams { Count = 5, OwnerId = target.Id });
                    string text = "";
                    foreach (var post in posts.WallPosts)
                    {
                        text += post.Text;
                    }
                    Statistic = GetFrequency(text);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"При получении постов произошла ошибка {e.Message}");
                    continue;
                }

                //serializing & post stat
                string jsonStatistic = JsonConvert.SerializeObject(Statistic);
                string message = $"Статистика {target.Type} id:{target.Id} за последние {posts.WallPosts.Count} постов:{jsonStatistic}";
                Console.WriteLine(message);
                Console.Write("Отправляем статистику на стену...");

                try
                {
                    vkApi.Wall.Post(new VkNet.Model.RequestParams.WallPostParams { Message = message, OwnerId = vkApi.UserId });
                    Console.WriteLine("Ok");
                }
                catch (VkNet.Exception.VkApiException e)
                {
                    Console.WriteLine($"Ошибка {e.Message}");
                }
            }
        }
        /// <summary>
        /// Log in VK by credentials or access token
        /// </summary>
        /// <param name="useAccessToken">If true use access token from settings.settings for log in</param>
        /// <returns></returns>
        static bool Login(bool useAccessToken)
        {
            bool authResult = false;
            VkNet.Model.ApiAuthParams autorizationParams = new VkNet.Model.ApiAuthParams();
            autorizationParams.Settings = VkNet.Enums.Filters.Settings.All;
            autorizationParams.ApplicationId = AppId;

            if (!useAccessToken)
            {
                Console.WriteLine("Логин:");
                autorizationParams.Login = Console.ReadLine();
                Console.WriteLine("Пароль:");
                autorizationParams.Password = Console.ReadLine();
            }
            else
            {
                autorizationParams.AccessToken = Properties.Settings.Default.AccessToken;
            }
            try
            {
                vkApi.Authorize(autorizationParams);
                vkApi.Account.GetInfo();//if access token is invalid or was used with another ip throws UserAutorizationFailException. 
                authResult = true;

            }
            catch (VkNet.Exception.VkApiAuthorizationException)
            {
                Console.WriteLine("Неверная пара логин\\пароль");
            }
            catch (VkNet.Exception.VkApiException e)
            {
                Console.WriteLine($"Ошибка ВК {e.Message}. Проверьте введенные данные");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка {e.Message}");
            }
            return authResult;
        }

        static Dictionary<char, double> GetFrequency(string text)
        {
            Dictionary<char, double> returns = new Dictionary<char, double>();
            text = new string(text.Where(l => Char.IsLetter(l)).ToArray());
            foreach (var letter in text)
            {
                if (!returns.ContainsKey(letter)&&Char.IsLetter(letter))
                {
                    var count = text.Count(c => c == letter);
                    returns.Add(letter, Math.Round((double)count / text.Length, 3));
                }
            }
            return returns;
        }


    }
}
