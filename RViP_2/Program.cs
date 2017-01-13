using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RViP_2
{
	class Program
	{
		//*Парсим*теги*изображений
		private static readonly Regex ImgRegex = new Regex(@"\<img.+?src=\""(?<imgsrc>.+?)\"".+?\>",
			RegexOptions.ExplicitCapture | RegexOptions.Compiled);

		static void Main(string[] args)
		{
			var urls = new[] {
				"https://geekbrains.ru"
			};

			// Загружаем параллельно все сайты
			Parallel.ForEach(urls, DownloadFiles);

			Console.WriteLine("Загрузка закончена");
			Console.ReadKey();
		}

		private static void DownloadFiles(string site)
		{
			// Загрузка данных сайта
			string data;
			Console.WriteLine(site);
			Console.WriteLine("Загрузка страницы");
			using (WebClient client = new WebClient())
			{
				using (Stream stream = client.OpenRead(site))
				{
					using (StreamReader reader = new StreamReader(stream))
					{
						data = reader.ReadToEnd();
					}
				}
			}

			Console.WriteLine("Загрузка картинок");

			// Создаём директорию под картинки
			string directory = new Uri(site).Host;
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			// Парсинг
			ImgRegex.Matches(data)
				.Cast<Match>()
				//*Данный*из*группы*регулярного*выражения
				.Select(m => m.Groups["imgsrc"].Value.Trim())
				// Удаляем повторяющиеся
				.Distinct()
				//*Добавляем*название*сайта,*если*ссылки*относительные
				.Select(url =>
				{
					if (url.Contains("http://"))
					{
						return url;
					}
					
					if ( url.Contains("https://"))
					{
						return url;
					}

					return site + url;
					//url.Contains("https://") ? url : (site + url)
				})
				//*Получаем*название*картинки
				.Select(url => new { url, name = url.Split(new[] { '/' }).Last() })
				//*Проверяем*его
				.Where(arg => Regex.IsMatch(arg.name, @"[^\s\/]\.(jpg|png|gif|bmp)\z"))
				// Параллелим на 7 потоков
				.AsParallel()
				.WithDegreeOfParallelism(7)
				// Загружаем асинхронно
				.ForAll(value =>
				{
					string savePath = Path.Combine(directory, value.name);
					try
					{
						using (WebClient localClient = new WebClient())
						{
							localClient.DownloadFile(value.url, savePath);
						}
						Console.WriteLine("{0} загружен", value.name);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
					
				});
		}
	}
}

