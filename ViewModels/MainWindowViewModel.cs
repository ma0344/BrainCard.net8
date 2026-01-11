using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Media.Effects;
using System.Windows.Media;
using System.Windows;
using System.Threading;

namespace BrainCard.ViewModels
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            IsInEditMode = false;
            IsLoading = false;
            LoadingMessage = string.Empty;
            StringsResourceDictionary = new ResourceDictionary();
            StringsResourceDictionary.Source = new Uri($"pack://application:,,,/Properties/Resources.{Thread.CurrentThread.CurrentUICulture.Name}.xaml", UriKind.Absolute);
        }

        private Color _ShadowColor = Colors.Gray;
        public Color ShadowColor
        {
            get => _ShadowColor;
            set
            {
                if (_ShadowColor != value)
                {
                    _ShadowColor = value;
                    OnPropertyChanged(nameof(ShadowColor));
                }
            }
        }

        private ResourceDictionary _StringsResourceDictionary;
        public ResourceDictionary StringsResourceDictionary
        {
            get
            {
                return _StringsResourceDictionary;
            }
            set
            {
                if(value != _StringsResourceDictionary)
                {
                    _StringsResourceDictionary = value;
                    OnPropertyChanged(nameof(StringsResourceDictionary));
                }
            }
        }

        private Visibility _TestPictVisible = Visibility.Hidden;
        public Visibility TestPictVisible
        {
            get
            {
                return _TestPictVisible;
            }
            set
            {
                if (value != _TestPictVisible)
                {
                    _TestPictVisible = value;
                    OnPropertyChanged(nameof(TestPictVisible));
                }
            }
        }

        private Visibility _DebugTextVisible = Visibility.Collapsed;
        public Visibility DebugTextVisible
        {
            get
            {
                return _DebugTextVisible;
            }
            set
            {
                if (value != _DebugTextVisible)
                {
                    _DebugTextVisible = value;
                    OnPropertyChanged(nameof(DebugTextVisible));
                }
            }
        }

        private double _ShadowBlurRadius = 100;
        public double ShadowBlurRadius
        {
            get => _ShadowBlurRadius;
            set
            {
                if (_ShadowBlurRadius != value)
                {
                    _ShadowBlurRadius = value;
                    OnPropertyChanged(nameof(ShadowBlurRadius));
                }
            }
        }

        private double _ShadowDepth = 0;
        public double ShadowDepth
        {
            get => _ShadowDepth;
            set
            {
                if (_ShadowDepth != value)
                {
                    _ShadowDepth = value;
                    OnPropertyChanged(nameof(ShadowDepth));
                }
            }
        }

        private double _ShadowDirection = 0;
        public double ShadowDirection
        {
            get => _ShadowDirection;
            set
            {
                if (_ShadowDirection != value)
                {
                    _ShadowDirection = value;
                    OnPropertyChanged(nameof(ShadowDirection));
                }
            }
        }

        private double _ShadowOpacity = 1;
        public double ShadowOpacity
        {
            get => _ShadowOpacity;
            set
            {
                if (_ShadowOpacity != value)
                {
                    _ShadowOpacity = value;
                    OnPropertyChanged(nameof(ShadowOpacity));
                }
            }
        }

        private Point _SubWindowPosition = new Point();

        public Point SubWindowPosition
        {
            get
            {
                return _SubWindowPosition;
            }
            set
            {
                if(_SubWindowPosition != value)
                {
                    _SubWindowPosition = value;
                    OnPropertyChanged(nameof(SubWindowPosition));
                }

            }
        }

        private bool _SubwindowVisible = false;
        public bool SubwindowVisible
        {
            get
            {
                return _SubwindowVisible;
            }
            set
            {
                _SubwindowVisible = value;
                OnPropertyChanged(nameof(SubwindowVisible));
            }
        }

        private Effect _ColorCardShadow = Values.DefaultCardShadow;
        public Effect ColorCardShadow
        {
            get => _ColorCardShadow;
            set
            {
                if (_ColorCardShadow != value)
                {
                    _ColorCardShadow = value;
                    OnPropertyChanged(nameof(ColorCardShadow));
                }
            }
        }

        private ObservableCollection<Card> _cardList = new ObservableCollection<Card>();
        public ObservableCollection<Card> CardList
        {
            get => _cardList;
            set
            {
                if (_cardList != value)
                {
                    _cardList = value;
                    OnPropertyChanged(nameof(CardList));
                }
            }
        }

        private bool _isInEditMode;
        public bool IsInEditMode
        {
            get => _isInEditMode;
            set
            {
                if (_isInEditMode != value)
                {
                    _isInEditMode = value;
                    OnPropertyChanged(nameof(IsInEditMode));
                }
            }
        }

        private string _currentFileName;
        public string CurrentFileName
        {
            get => _currentFileName;
            set
            {
                if (_currentFileName != value)
                {
                    _currentFileName = value;
                    OnPropertyChanged(nameof(CurrentFileName));
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        private bool _isNewFile = true;
        public bool IsNewFile
        {
            get => _isNewFile;
            set
            {
                if (_isNewFile != value)
                {
                    _isNewFile = value;
                    OnPropertyChanged(nameof(IsNewFile));
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string WindowTitle
        {
            get
            {
                var name = string.IsNullOrWhiteSpace(CurrentFileName) ? "Untitled.bcf2" : Path.GetFileName(CurrentFileName);
                return IsNewFile ? $"BrainCard - {name}（新規）" : $"BrainCard - {name}";
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        private string _loadingMessage;
        public string LoadingMessage
        {
            get => _loadingMessage;
            set
            {
                if (_loadingMessage != value)
                {
                    _loadingMessage = value;
                    OnPropertyChanged(nameof(LoadingMessage));
                }
            }
        }

        private int _loadingTotal;
        public int LoadingTotal
        {
            get => _loadingTotal;
            set
            {
                if (_loadingTotal != value)
                {
                    _loadingTotal = value;
                    OnPropertyChanged(nameof(LoadingTotal));
                    OnPropertyChanged(nameof(LoadingProgressPercent));
                }
            }
        }

        private int _loadingCurrent;
        public int LoadingCurrent
        {
            get => _loadingCurrent;
            set
            {
                if (_loadingCurrent != value)
                {
                    _loadingCurrent = value;
                    OnPropertyChanged(nameof(LoadingCurrent));
                    OnPropertyChanged(nameof(LoadingProgressPercent));
                }
            }
        }

        public double LoadingProgressPercent
        {
            get
            {
                if (LoadingTotal <= 0) return 0;
                return (double)LoadingCurrent / LoadingTotal * 100.0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName.StartsWith("Shadow"))
            {
                ColorCardShadow = new DropShadowEffect()
                {
                    BlurRadius = ShadowBlurRadius,
                    Color = ShadowColor,
                    Direction = ShadowDirection,
                    Opacity = ShadowOpacity,
                    ShadowDepth = ShadowDepth
                };
            }
        }

        
    }
}
