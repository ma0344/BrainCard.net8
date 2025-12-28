using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace MyUWPApp
{
    public sealed partial class CustomCard : UserControl
    {

        public InkCanvas InnerInkCanvas;
        public InkPresenter InnerPresenter;
        public Grid BaseGrid => baseGrid;
        public Thumb InnerThumb => CardThumb;
        public bool IsWritable;
        
        public CustomCard()
        {
            this.InitializeComponent();
            this.DataContext = new Values();
            CardThumb.ApplyTemplate();
            InnerInkCanvas = CardThumb.FindName("InkCanvas") as InkCanvas;
            var visualCount = VisualTreeHelper.GetChildrenCount(this);
            var childInkCanvas = VisualTreeHelper.GetChild(CardThumb, 0);
            if (childInkCanvas is InkCanvas inkCanvas)
            {
                InnerInkCanvas = inkCanvas;
                InnerPresenter = InnerInkCanvas.InkPresenter;
                InnerPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
                
            }
            Width = Values.cardWidth;
            Height = Values.cardHeight;
            CardThumb.Width = Values.cardWidth;
            CardThumb.Height = Values.cardHeight;
            InnerInkCanvas.IsHitTestVisible = true;
            InnerThumb.IsHitTestVisible = true;

        }



        public void InkCanvas_OnLoaded(object sender, RoutedEventArgs e)
        {
                InnerPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
        }

        public CustomCard Clone()
        {

            var clonedCard = new CustomCard();
            foreach (var stroke in InnerPresenter.StrokeContainer.GetStrokes())
            {
                clonedCard.InnerPresenter.StrokeContainer.AddStroke(stroke.Clone());
            }
            clonedCard.Width = Values.cardWidth;
            clonedCard.Height = Values.cardHeight;
            clonedCard.CardThumb.Width = Width;
            clonedCard.CardThumb.Height = Height;
            return clonedCard;
        }


    }

}







