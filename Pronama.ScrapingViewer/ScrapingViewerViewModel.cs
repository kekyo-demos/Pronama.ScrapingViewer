using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Sgml;

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

		#region FetchHtmlFromUrlAsync
		/// <summary>
		/// 指定されたURLからHTMLをダウンロードしてXElementとして取得する、非同期メソッドです。
		/// </summary>
		/// <param name="url">URL</param>
		/// <returns>XElementの結果を返すタスク</returns>
		private static async Task<XElement> FetchHtmlFromUrlAsync(Uri url)
		{
			// HttpClientを使って非同期でダウンロードします。
			using (var httpClient = new HttpClient())
			{
				// プロ生ちゃんサイトの壁紙コーナーから非同期でダウンロードするよ
				using (var stream = await httpClient.GetStreamAsync(url).
					ConfigureAwait(false))	// ←この後の処理をワーカースレッドで実行するおまじない
				{
					using (var tr = new StreamReader(stream, Encoding.UTF8, true))
					{
						using (var sgmlReader = new SgmlReader(tr)
							{
								CaseFolding = CaseFolding.ToLower,	// タグ名とか、常に小文字にするよ
								DocType = "HTML",	// 常にHTMLとして読み取るよ
								IgnoreDtd = true,	// DTDを無視するよ
								WhitespaceHandling = false	// 空白を無視するよ
							})
						{
							// SgmlReaderを使って、XElementに変換！
							return XElement.Load(sgmlReader);
						}
					}
				}
			}
		}
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
				var html = await FetchHtmlFromUrlAsync(wallpaperUrl_);

			}
			finally
			{
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
