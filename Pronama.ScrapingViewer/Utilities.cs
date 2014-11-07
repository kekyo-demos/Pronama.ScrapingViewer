using Sgml;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Pronama.ScrapingViewer
{
	/// <summary>
	/// ユーティリティクラスです。
	/// </summary>
	public static class Utilities
	{
		#region Run
		/// <summary>
		/// 指定されたデリゲートの処理を、ワーカースレッドを実行します。
		/// </summary>
		/// <typeparam name="T">戻り値の型</typeparam>
		/// <param name="action">処理を示すデリゲート</param>
		/// <returns>戻り値を示すタスク</returns>
		/// <remarks>このメソッドで処理を実行すると、WPFのメモリリーク問題を回避出来ます。</remarks>
		private static Task<T> Run<T>(Func<T> action)
		{
			// BUG: Resource leaked by worker thread using DependencyObject.
			//  http://grabacr.net/archives/1851

			var tcs = new TaskCompletionSource<T>();

			var thread = new Thread(() =>
			{
				try
				{
					tcs.SetResult(action());
				}
				catch (Exception ex)
				{
					tcs.SetException(ex);
				}

				Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.SystemIdle);
				Dispatcher.Run();
			});

			thread.SetApartmentState(ApartmentState.STA);
			thread.IsBackground = true;
			thread.Start();

			return tcs.Task;
		}
		#endregion

		#region FetchHtmlFromUrlAsync
		/// <summary>
		/// 指定されたURLからHTMLをダウンロードしてXDocumentとして取得する、非同期メソッドです。
		/// </summary>
		/// <param name="url">URL</param>
		/// <returns>XDocumentの結果を返すタスク</returns>
		public static async Task<XDocument> FetchHtmlFromUrlAsync(Uri url)
		{
			// HttpClientを使って非同期でダウンロードします。
			using (var httpClient = new HttpClient())
			{
				// 非同期でダウンロードするよ
				using (var stream = await httpClient.GetStreamAsync(url).
					ConfigureAwait(false))	// ←この後の処理をワーカースレッドで実行するおまじない
				{
					// ストリームをUTF8のテキストとして読めるようにするよ
					using (var tr = new StreamReader(stream, Encoding.UTF8, true))
					{
						// スクレイピングの主役「SgmlReader」
						using (var sgmlReader = new SgmlReader(tr)
							{
								CaseFolding = CaseFolding.ToLower,	// タグ名とか、常に小文字にするよ
								DocType = "HTML",					// 常にHTMLとして読み取るよ
								IgnoreDtd = true,					// DTDを無視するよ
								WhitespaceHandling = false			// 空白を無視するよ
							})
						{
							// SgmlReaderを使って、XDocumentに変換！
							return XDocument.Load(sgmlReader);
						}
					}
				}
			}
		}
		#endregion

		#region SafeGetAttribute
		/// <summary>
		/// XElementに定義されているXML属性の取得を試みます。
		/// </summary>
		/// <param name="element">対象のXElement</param>
		/// <param name="attributeName">XML属性名</param>
		/// <returns>値が存在しない場合はnull</returns>
		public static string SafeGetAttribute(XElement element, string attributeName)
		{
			var attribute = element.Attribute(attributeName);
			if (attribute == null)
			{
				return null;
			}

			return attribute.Value;
		}
		#endregion

		#region ParseUrl
		/// <summary>
		/// URL文字列をパースしてUriに変換します。
		/// </summary>
		/// <param name="baseUrl">基準となるURLを示すUri</param>
		/// <param name="urlString">相対、又は絶対URLを示す文字列</param>
		/// <returns>パース出来ない場合はnull</returns>
		public static Uri ParseUrl(Uri baseUrl, string urlString)
		{
			Uri url;
			Uri.TryCreate(baseUrl, urlString, out url);

			return url;
		}
		#endregion

		#region FetchImageFromUrlAsync
		/// <summary>
		/// 指定されたURLからイメージをダウンロードしてImageSourceとして取得する、非同期メソッドです。
		/// </summary>
		/// <param name="url">URL</param>
		/// <returns>ImageSourceの結果を返すタスク</returns>
		public static Task<ImageSource> FetchImageFromUrlAsync(Uri url)
		{
			// WPFのメモリリーク問題を回避するために、処理全体をワーカースレッドで実行するよ
			return Run(() =>
				{
					// HttpClientを使って非同期でダウンロードします。
					using (var httpClient = new HttpClient())
					{
						// プロ生ちゃんサイトの壁紙コーナーからダウンロードするよ
						using (var stream = httpClient.GetStreamAsync(url).Result)
						{
							// デコーダーを使って、データをイメージに変換するよ
							var decoder = (url.AbsolutePath.EndsWith(".jpg") == true) ?
								(BitmapDecoder)new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.Default) :
								(BitmapDecoder)new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);

							// Freezeすると、別のスレッドでも使えるようになるよ
							var frame0 = decoder.Frames[0];
							frame0.Freeze();

							return (ImageSource)frame0;
						}
					}
				});
		}
		#endregion
	}
}
