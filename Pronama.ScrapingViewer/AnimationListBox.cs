using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Pronama.ScrapingViewer
{
	/// <summary>
	/// アイテムの追加をアニメーションするリストボックスです。
	/// </summary>
	/// <remarks>kaorunさんの元ネタコードを、WPFに移植しました:  http://d.hatena.ne.jp/kaorun/20111219/1324288358 </remarks>
	public sealed class AnimationListBox : ListBox
	{
		private static readonly Duration duration = new Duration(TimeSpan.FromSeconds(1.0));
		private static readonly double itemInterval = 0.2;

		private readonly List<FrameworkElement> appendingItems = new List<FrameworkElement>();

		public AnimationListBox()
		{
			this.CacheMode = new BitmapCache();
		}

		protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);

			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				appendingItems.Clear();
			}
			else if (e.Action == NotifyCollectionChangedAction.Add)
			{
				foreach (var item in e.NewItems)
				{
					this.UpdateLayout();
					var elm = item as FrameworkElement;
					if (elm == null)
						elm = base.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
					if (elm != null)
					{
						elm.Opacity = 0.0;
						appendingItems.Add(elm);
						this.Dispatcher.BeginInvoke(new Action(AppendToList));
					}
				}
			}
		}

		private void AppendToList()
		{
			for (int i = 0; i < appendingItems.Count; i++)
			{
				var item = appendingItems[i];
				AnimateItem(item, i);
			}

			appendingItems.Clear();
		}

		private static void AnimateItem(FrameworkElement element, int delayCount)
		{
			var storyboard = new Storyboard();

			var opaAni = new DoubleAnimation();
			opaAni.From = 0.0;
			opaAni.To = 1.0;
			opaAni.Duration = duration;
			opaAni.EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseIn, Exponent = 10.0 };

			Storyboard.SetTarget(opaAni, element);
			Storyboard.SetTargetProperty(opaAni, new PropertyPath(UIElement.OpacityProperty));

			element.Opacity = 0;
			storyboard.Children.Add(opaAni);

			var transform = new TranslateTransform();
			element.RenderTransform = transform;

			NameScope.SetNameScope(element, new NameScope());
			element.RegisterName("SlideTransform", transform);

			var slideAni = new DoubleAnimation
			{
				From = 100.0,
				To = 0,
				Duration = duration,
				EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseIn, Exponent = 5.0 }
			};

			Storyboard.SetTargetName(slideAni, "SlideTransform");
			Storyboard.SetTargetProperty(slideAni, new PropertyPath(TranslateTransform.XProperty));
	
			storyboard.Children.Add(slideAni);

			var delay = itemInterval * delayCount;

			storyboard.BeginTime = TimeSpan.FromSeconds(delay);
			storyboard.Begin(element);
		}
	}
}
