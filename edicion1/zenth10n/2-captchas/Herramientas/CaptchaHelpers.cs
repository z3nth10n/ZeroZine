using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace jwt_api
{
    public static class CaptchaHelpers
    {
        private static Image DownloadReCaptcha(FlurlClient client, ref JObject challenge)
        {
            try
            {
                string res = client.HttpGet("http://localhost/z/api.php?action=captcha");

                challenge = JsonConvert.DeserializeObject<JObject>(res);

                byte[] byteArrayIn = Convert.FromBase64String(challenge["data"]["jpeg"].ToString());

                using (var ms = new MemoryStream(byteArrayIn))
                    return Image.FromStream(ms);
            }
            catch (Exception)
            {
                challenge = null;
                return null;
            }
        }

        public static string SolveCaptcha()
        {
            using (FlurlClient client = new FlurlClient().EnableCookies())
            {
                JObject challenge = null;

                int height = 5;
                for (int i = 0; i < height; ++i)
                    Console.WriteLine();

                Image img = DownloadReCaptcha(client, ref challenge);
                ImageConsoleDrawer.ConsoleDisplayImage(img, new Point(0, 0), new Size((int)(height * 4.5f), height));

                //Console.WriteLine(string.Join(Environment.NewLine, client.Cookies.Select(x => string.Format("Key: {0}; Value: {1}", x.Key, x.Value.Value))));

                Console.WriteLine();
                ImageConsoleDrawer.ConsoleWriteImage((Bitmap)img);
                Console.WriteLine();

                Console.Write("What's in the captcha?: ");
                string r = Console.ReadLine();

                return client.HttpPost(Helpers.ApiUrl, new { action = "resolve-captcha", input = r, PHPSESSID = client.Cookies.First().Value.Value });
            }
        }
    }
}