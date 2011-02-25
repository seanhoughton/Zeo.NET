using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Zeo.NET;

namespace ZeoUI
{
    class FrequencyBandsViewModel : DependencyObject
    {
        public ZeoController Controller
        {
            get { return (ZeoController)GetValue(ZeoControllerProperty); }
            set { SetValue(ZeoControllerProperty, value); }
        }

        public static readonly DependencyProperty ZeoControllerProperty =
            DependencyProperty.Register(
            "Controller", typeof(ZeoController), typeof(FrequencyBandsViewModel), new PropertyMetadata(null, OnControllerChanged));

        public double DeltaValue
        {
            get { return (double) GetValue(DeltaValueProperty); }
            set { SetValue(DeltaValueProperty, value); }
        }

        public static readonly DependencyProperty DeltaValueProperty =
            DependencyProperty.Register(
            "DeltaValue", typeof(double), typeof(FrequencyBandsViewModel), new PropertyMetadata((double)0));

        public double ThetaValue
        {
            get { return (double)GetValue(ThetaValueProperty); }
            set { SetValue(ThetaValueProperty, value); }
        }

        public static readonly DependencyProperty ThetaValueProperty =
            DependencyProperty.Register(
            "ThetaValue", typeof(double), typeof(FrequencyBandsViewModel), new PropertyMetadata((double)0));


        public double Beta1Value
        {
            get { return (double)GetValue(Beta1ValueProperty); }
            set { SetValue(Beta1ValueProperty, value); }
        }

        public static readonly DependencyProperty Beta1ValueProperty =
            DependencyProperty.Register(
            "Beta1Value", typeof(double), typeof(FrequencyBandsViewModel), new PropertyMetadata((double)0));

        public double Beta2Value
        {
            get { return (double)GetValue(Beta2ValueProperty); }
            set { SetValue(Beta2ValueProperty, value); }
        }

        public static readonly DependencyProperty Beta2ValueProperty =
            DependencyProperty.Register(
            "Beta2Value", typeof(double), typeof(FrequencyBandsViewModel), new PropertyMetadata((double)0));

        public double Beta3Value
        {
            get { return (double)GetValue(Beta3ValueProperty); }
            set { SetValue(Beta3ValueProperty, value); }
        }

        public static readonly DependencyProperty Beta3ValueProperty =
            DependencyProperty.Register(
            "Beta3Value", typeof(double), typeof(FrequencyBandsViewModel), new PropertyMetadata((double)0));


        public double GammaValue
        {
            get { return (double)GetValue(GammaValueProperty); }
            set { SetValue(GammaValueProperty, value); }
        }

        public static readonly DependencyProperty GammaValueProperty =
            DependencyProperty.Register(
            "GammaValue", typeof(double), typeof(FrequencyBandsViewModel), new PropertyMetadata((double)0));

        private void SubscribeToController()
        {
            Controller.Parser.SliceReceived += OnNewSlice;
        }

         public void OnNewSlice(object sender, SliceHandlerEventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DeltaValue = args.Slice.FrequencyBins[Utility.FrequencyBands[Utility.EFrequencyBand.Delta]];
                ThetaValue = args.Slice.FrequencyBins[Utility.FrequencyBands[Utility.EFrequencyBand.Theta]];
                Beta1Value = args.Slice.FrequencyBins[Utility.FrequencyBands[Utility.EFrequencyBand.Beta1]];
                Beta2Value = args.Slice.FrequencyBins[Utility.FrequencyBands[Utility.EFrequencyBand.Beta2]];
                Beta3Value = args.Slice.FrequencyBins[Utility.FrequencyBands[Utility.EFrequencyBand.Beta3]];
                GammaValue = args.Slice.FrequencyBins[Utility.FrequencyBands[Utility.EFrequencyBand.Gamma]];
            }));
        }

        public static void OnControllerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var vm = (FrequencyBandsViewModel)d;
            vm.SubscribeToController();
        }
    }
}
