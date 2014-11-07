using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace Pronama.ScrapingViewer
{
	/// <summary>
	/// メインビューに対応するビューモデルのクラスです。
	/// </summary>
	/// <remarks>このクラスにロジックを書きます。
	/// 本当はロジックはモデルクラスに書くべきですが、このサンプルコードでは単純化しています。</remarks>
	public sealed class ScrapingViewerViewModel
		: INotifyPropertyChanged
	{
		#region Fields
		/// <summary>
		/// プロ生ちゃんサイトの壁紙ページURLです。
		/// </summary>
		private static readonly Uri wallpaperUrl_ = new Uri("http://pronama.azurewebsites.net/pronama/wallpaper/");

		/// <summary>
		/// 実行可能かどうかを格納するフィールドです。
		/// </summary>
		private bool isReady_ = true;
		#endregion

		#region Constructors
		/// <summary>
		/// コンストラクタです。
		/// </summary>
		public ScrapingViewerViewModel()
		{
			// イメージを格納するコレクションを準備
			this.Images = new ObservableCollection<ImageViewModel>();

			// コマンドを準備
			this.FireLoad = new DelegatedCommand(this.OnFireLoad);
		}
		#endregion

		#region Properties
		/// <summary>
		/// ビューにバインディングする、イメージのコレクションです。
		/// </summary>
		public ObservableCollection<ImageViewModel> Images
		{
			get;
			private set;
		}

		/// <summary>
		/// ビューにバインディングする、実行可能である事を示すプロパティです。
		/// </summary>
		public bool IsReady
		{
			get
			{
				return isReady_;
			}
			set
			{
				// 値が変わったときだけ
				if (value != isReady_)
				{
					// 保存
					isReady_ = value;

					// イベントをフックしているインスタンスに、このプロパティが変更されたことを通知するよ。
					// もっと複雑なアプリを作るときには、こんなインフラが整ったフレームワークを使ったほうがいいね。
					var propertyChanged = this.PropertyChanged;
					if (propertyChanged != null)
					{
						propertyChanged(this, new PropertyChangedEventArgs("IsReady"));
					}
				}
			}
		}

		/// <summary>
		/// ビューにバインディングする、コマンドです。
		/// </summary>
		public ICommand FireLoad
		{
			get;
			private set;
		}
		#endregion

		#region Events
		/// <summary>
		/// プロパティが変更されたことを通知するイベントです。
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region OnFireLoad
		/// <summary>
		/// コマンド（ビューのボタン）の実行時に、ここに遷移します。
		/// </summary>
		private async void OnFireLoad()
		{
			// 実行中は準備完了状態を落としておくと、ボタンが無効化されるよ
			this.IsReady = false;

			try
			{
				// プロ生ちゃん壁紙サイトから、HTMLを非同期でダウンロードするよ
				var document = await Utilities.FetchHtmlFromUrlAsync(wallpaperUrl_);

				// LINQで抽出しよう！ ターゲットは...
				// 「html→body→div(container)→div(row)→div(hl_links)→div→a(liimagelink)→img」
				// for文でいちいち回していると大変だけど、LINQなら超簡単！ 直線的に書くだけだよ。
				var urls =
					from html in document.Elements("html")									// htmlタグを全部抽出（1コだけ）
					from body in html.Elements("body")										// html配下のbodyタグを全部抽出（1コだけ）
					from divContainer in body.Elements("div")								// body配下のdivタグを全部抽出
					where Utilities.SafeGetAttribute(divContainer, "class") == "container"	// 上のdivタグに「class="container"」があれば、次の式へ
					from divRow in divContainer.Elements("div")								// 上のdiv配下のdivタグを全部抽出
					where Utilities.SafeGetAttribute(divRow, "class") == "row"				// 上のdivタグに「class="row"」があれば、次へ
					from divHlLinks in divRow.Elements("div")								// 上のdiv配下のdivタグを全部抽出
					where Utilities.SafeGetAttribute(divHlLinks, "id") == "hl_links"		// 上のdivタグに「id="hl_links"」があれば、次へ（ここまで、ページに変更がなければどれも1コだけ取れるはず）
					from div in divHlLinks.Elements("div")									// 上のdiv配下のdivタグを全部抽出（このdivタグはチェックしないよ）
					from a in div.Elements("a")												// 上のdiv配下のaタグを全部抽出
					where
						(Utilities.SafeGetAttribute(a, "class") == "liimagelink") &&		// 上のaタグに「class="liimagelink"」があり、
						(a.Elements("img").Any() == true)									// かつ、aタグ配下に一つ以上imgタグがあれば、次へ
					let href = Utilities.SafeGetAttribute(a, "href")						// 上のaタグの「href="・・・"」を取得するよ
					let url = Utilities.ParseUrl(wallpaperUrl_, href)						// Uriクラスに変換してみる
					where url != null														// 変換出来たら
					select url;																// 変換したURLを返すよ

#if true
				// ザックザックと全部非同期でダウンロードしちゃう！
				await Task.WhenAll(urls.Select(async url =>
					{
						// URLを指定してダウンロードするよ
						var image = await Utilities.FetchImageFromUrlAsync(url);

						// コレクションに追加すれば、データバインディングで自動的に表示される！
						this.Images.Add(new ImageViewModel { ImageData = image });
					}));
#else
				// シーケンシャルにダウンロードするとどうなるか、試してみて。
				foreach (var url in urls)
				{
					// URLを指定してダウンロードするよ
					var image = await Utilities.FetchImageFromUrlAsync(url);

					// コレクションに追加すれば、データバインディングで自動的に表示される！
					this.Images.Add(new ImageViewModel { ImageData = image });
				}
#endif
			}
			finally
			{
				// 全部終わったら、準備完了状態に戻すよ
				this.IsReady = true;
			}
		}
		#endregion

		#region ImageViewModel
		/// <summary>
		/// コレクションが保持する、イメージ用のビューモデルです。
		/// </summary>
		/// <remarks>取得対象がイメージデータだけなので、ビューモデルを定義する必要はないのですが、
		/// XAMLのバインディング式と対比しやすくするため、敢えて定義しました。</remarks>
		public sealed class ImageViewModel
		{
			/// <summary>
			/// イメージを取得・設定します。
			/// </summary>
			public ImageSource ImageData
			{
				get;
				set;
			}
		}
		#endregion
	}
}
