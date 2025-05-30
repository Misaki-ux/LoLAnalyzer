using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Services;
using LoLAnalyzer.UI.ViewModels;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.UI.Windows
{
    /// <summary>
    /// Fenêtre d'overlay avancée qui se superpose au client League of Legends
    /// </summary>
    public partial class AdvancedOverlayWindow : Window, INotifyPropertyChanged
    {
        #region Importation des API Windows
        
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        #endregion

        #region Propriétés privées
        
        private readonly OverlayViewModel _viewModel;
        private readonly Logger _logger;
        private readonly DispatcherTimer _clientDetectionTimer;
        private readonly DispatcherTimer _animationTimer;
        private readonly DispatcherTimer _autoUpdateTimer;
        private readonly ConfigManager _configManager;
        private IntPtr _lolClientHandle = IntPtr.Zero;
        private bool _isDragging = false;
        private Point _startPoint;
        private double _originalWidth;
        private double _originalHeight;
        private double _originalLeft;
        private double _originalTop;
        private bool _isExpanded = true;
        private bool _isAutoDetectEnabled = true;
        private bool _isAlwaysOnTop = true;
        private double _opacityValue = 0.9;
        private string _selectedTheme = "Dark";
        private string _selectedPosition = "TopRight";
        private bool _isCompactMode = false;
        
        #endregion

        #region Propriétés publiques
        
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                    AnimateExpandCollapse();
                }
            }
        }

        public bool IsAutoDetectEnabled
        {
            get => _isAutoDetectEnabled;
            set
            {
                if (_isAutoDetectEnabled != value)
                {
                    _isAutoDetectEnabled = value;
                    OnPropertyChanged(nameof(IsAutoDetectEnabled));
                    
                    if (_isAutoDetectEnabled)
                        _clientDetectionTimer.Start();
                    else
                        _clientDetectionTimer.Stop();
                }
            }
        }

        public bool IsAlwaysOnTop
        {
            get => _isAlwaysOnTop;
            set
            {
                if (_isAlwaysOnTop != value)
                {
                    _isAlwaysOnTop = value;
                    this.Topmost = value;
                    OnPropertyChanged(nameof(IsAlwaysOnTop));
                }
            }
        }

        public double OpacityValue
        {
            get => _opacityValue;
            set
            {
                if (Math.Abs(_opacityValue - value) > 0.01)
                {
                    _opacityValue = value;
                    this.Opacity = value;
                    OnPropertyChanged(nameof(OpacityValue));
                }
            }
        }

        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme != value)
                {
                    _selectedTheme = value;
                    OnPropertyChanged(nameof(SelectedTheme));
                    ApplyTheme();
                }
            }
        }

        public string SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                if (_selectedPosition != value)
                {
                    _selectedPosition = value;
                    OnPropertyChanged(nameof(SelectedPosition));
                    PositionOverlay();
                }
            }
        }

        public bool IsCompactMode
        {
            get => _isCompactMode;
            set
            {
                if (_isCompactMode != value)
                {
                    _isCompactMode = value;
                    OnPropertyChanged(nameof(IsCompactMode));
                    ApplyCompactMode();
                }
            }
        }
        
        public ObservableCollection<string> AvailableThemes { get; } = new ObservableCollection<string> { "Dark", "Light", "Blue", "Red", "Green" };
        public ObservableCollection<string> AvailablePositions { get; } = new ObservableCollection<string> { "TopLeft", "TopRight", "BottomLeft", "BottomRight", "Center" };
        
        #endregion

        #region Constructeur et initialisation
        
        /// <summary>
        /// Constructeur de la fenêtre d'overlay avancée
        /// </summary>
        /// <param name="viewModel">ViewModel de l'overlay</param>
        /// <param name="logger">Logger pour les messages de diagnostic</param>
        /// <param name="configManager">Gestionnaire de configuration</param>
        public AdvancedOverlayWindow(OverlayViewModel viewModel, Logger logger, ConfigManager configManager)
        {
            InitializeComponent();
            
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            
            DataContext = _viewModel;
            
            // Configuration de la fenêtre pour l'overlay
            this.AllowsTransparency = true;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;
            this.Background = new SolidColorBrush(Colors.Transparent);
            
            // Chargement des paramètres de configuration
            LoadConfiguration();
            
            // Initialisation des timers
            _clientDetectionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _clientDetectionTimer.Tick += ClientDetectionTimer_Tick;
            
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            
            _autoUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _autoUpdateTimer.Tick += AutoUpdateTimer_Tick;
            
            // Événements pour le déplacement et le redimensionnement de la fenêtre
            this.MouseLeftButtonDown += OverlayWindow_MouseLeftButtonDown;
            this.MouseMove += OverlayWindow_MouseMove;
            this.MouseLeftButtonUp += OverlayWindow_MouseLeftButtonUp;
            this.SizeChanged += OverlayWindow_SizeChanged;
            
            // Démarrage des timers
            if (IsAutoDetectEnabled)
                _clientDetectionTimer.Start();
            
            _autoUpdateTimer.Start();
            
            // Positionnement initial
            PositionOverlay();
            
            // Application du thème
            ApplyTheme();
            
            // Application du mode compact si nécessaire
            if (IsCompactMode)
                ApplyCompactMode();
            
            _logger.Log(LogLevel.Info, "Fenêtre d'overlay avancée initialisée");
        }
        
        /// <summary>
        /// Chargement des paramètres de configuration
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                IsAutoDetectEnabled = _configManager.GetBoolSetting("Overlay", "AutoDetect", true);
                IsAlwaysOnTop = _configManager.GetBoolSetting("Overlay", "AlwaysOnTop", true);
                OpacityValue = _configManager.GetDoubleSetting("Overlay", "Opacity", 0.9);
                SelectedTheme = _configManager.GetStringSetting("Overlay", "Theme", "Dark");
                SelectedPosition = _configManager.GetStringSetting("Overlay", "Position", "TopRight");
                IsCompactMode = _configManager.GetBoolSetting("Overlay", "CompactMode", false);
                
                double width = _configManager.GetDoubleSetting("Overlay", "Width", 400);
                double height = _configManager.GetDoubleSetting("Overlay", "Height", 500);
                double left = _configManager.GetDoubleSetting("Overlay", "Left", 100);
                double top = _configManager.GetDoubleSetting("Overlay", "Top", 100);
                
                this.Width = width;
                this.Height = height;
                this.Left = left;
                this.Top = top;
                
                _originalWidth = width;
                _originalHeight = height;
                _originalLeft = left;
                _originalTop = top;
                
                _logger.Log(LogLevel.Info, "Configuration de l'overlay chargée");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors du chargement de la configuration: {ex.Message}");
                
                // Valeurs par défaut
                this.Width = 400;
                this.Height = 500;
                this.Left = 100;
                this.Top = 100;
                
                _originalWidth = 400;
                _originalHeight = 500;
                _originalLeft = 100;
                _originalTop = 100;
            }
        }
        
        /// <summary>
        /// Sauvegarde des paramètres de configuration
        /// </summary>
        private void SaveConfiguration()
        {
            try
            {
                _configManager.SetBoolSetting("Overlay", "AutoDetect", IsAutoDetectEnabled);
                _configManager.SetBoolSetting("Overlay", "AlwaysOnTop", IsAlwaysOnTop);
                _configManager.SetDoubleSetting("Overlay", "Opacity", OpacityValue);
                _configManager.SetStringSetting("Overlay", "Theme", SelectedTheme);
                _configManager.SetStringSetting("Overlay", "Position", SelectedPosition);
                _configManager.SetBoolSetting("Overlay", "CompactMode", IsCompactMode);
                
                _configManager.SetDoubleSetting("Overlay", "Width", this.Width);
                _configManager.SetDoubleSetting("Overlay", "Height", this.Height);
                _configManager.SetDoubleSetting("Overlay", "Left", this.Left);
                _configManager.SetDoubleSetting("Overlay", "Top", this.Top);
                
                _configManager.SaveSettings();
                
                _logger.Log(LogLevel.Info, "Configuration de l'overlay sauvegardée");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la sauvegarde de la configuration: {ex.Message}");
            }
        }
        
        #endregion

        #region Gestion de la détection du client LoL
        
        /// <summary>
        /// Détection du client LoL et positionnement de l'overlay
        /// </summary>
        private void ClientDetectionTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Recherche du client LoL
                IntPtr lolClientHandle = FindWindow("RCLIENT", "League of Legends");
                
                if (lolClientHandle != IntPtr.Zero)
                {
                    // Client trouvé
                    if (_lolClientHandle != lolClientHandle)
                    {
                        _lolClientHandle = lolClientHandle;
                        _logger.Log(LogLevel.Info, "Client League of Legends détecté");
                        
                        // Positionnement de l'overlay
                        PositionOverClientIfAvailable();
                        
                        // Mise à jour des données
                        _viewModel.RefreshData();
                    }
                }
                else
                {
                    // Recherche du client en jeu
                    lolClientHandle = FindWindow(null, "League of Legends (TM) Client");
                    
                    if (lolClientHandle != IntPtr.Zero)
                    {
                        // Client en jeu trouvé
                        if (_lolClientHandle != lolClientHandle)
                        {
                            _lolClientHandle = lolClientHandle;
                            _logger.Log(LogLevel.Info, "Client League of Legends en jeu détecté");
                            
                            // Positionnement de l'overlay
                            PositionOverClientIfAvailable();
                            
                            // Mise à jour des données
                            _viewModel.RefreshData();
                        }
                    }
                    else if (_lolClientHandle != IntPtr.Zero)
                    {
                        // Client fermé
                        _lolClientHandle = IntPtr.Zero;
                        _logger.Log(LogLevel.Info, "Client League of Legends fermé");
                    }
                }
                
                // Vérification de la phase de sélection des champions
                CheckChampionSelectPhase();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la détection du client: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Vérifie si le client est en phase de sélection des champions
        /// </summary>
        private void CheckChampionSelectPhase()
        {
            try
            {
                if (_lolClientHandle == IntPtr.Zero)
                    return;
                
                // Vérification du titre de la fenêtre active
                IntPtr foregroundWindow = GetForegroundWindow();
                var title = GetWindowTitle(foregroundWindow);
                
                bool isChampionSelect = title.Contains("Champion Select") || title.Contains("Sélection des champions");
                
                if (isChampionSelect)
                {
                    // Phase de sélection des champions détectée
                    _viewModel.IsChampionSelectPhase = true;
                    
                    // Mise à jour des données
                    _viewModel.RefreshData();
                    
                    _logger.Log(LogLevel.Info, "Phase de sélection des champions détectée");
                }
                else
                {
                    _viewModel.IsChampionSelectPhase = false;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors de la vérification de la phase de sélection: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Récupère le titre d'une fenêtre
        /// </summary>
        private string GetWindowTitle(IntPtr hWnd)
        {
            const int nChars = 256;
            var buff = new System.Text.StringBuilder(nChars);
            
            if (GetWindowText(hWnd, buff, nChars) > 0)
            {
                return buff.ToString();
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Positionne l'overlay par rapport au client LoL
        /// </summary>
        private void PositionOverClientIfAvailable()
        {
            if (_lolClientHandle == IntPtr.Zero)
                return;
            
            try
            {
                // Récupération de la position et de la taille du client
                RECT clientRect;
                if (GetWindowRect(_lolClientHandle, out clientRect))
                {
                    int clientWidth = clientRect.Right - clientRect.Left;
                    int clientHeight = clientRect.Bottom - clientRect.Top;
                    
                    // Positionnement de l'overlay en fonction de la position sélectionnée
                    switch (SelectedPosition)
                    {
                        case "TopLeft":
                            this.Left = clientRect.Left + 10;
                            this.Top = clientRect.Top + 10;
                            break;
                        
                        case "TopRight":
                            this.Left = clientRect.Right - this.Width - 10;
                            this.Top = clientRect.Top + 10;
                            break;
                        
                        case "BottomLeft":
                            this.Left = clientRect.Left + 10;
                            this.Top = clientRect.Bottom - this.Height - 10;
                            break;
                        
                        case "BottomRight":
                            this.Left = clientRect.Right - this.Width - 10;
                            this.Top = clientRect.Bottom - this.Height - 10;
                            break;
                        
                        case "Center":
                            this.Left = clientRect.Left + (clientWidth - this.Width) / 2;
                            this.Top = clientRect.Top + (clientHeight - this.Height) / 2;
                            break;
                    }
                    
                    // Sauvegarde de la position
                    _originalLeft = this.Left;
                    _originalTop = this.Top;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Erreur lors du positionnement de l'overlay: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Positionne l'overlay en fonction de la position sélectionnée
        /// </summary>
        private void PositionOverlay()
        {
            if (_lolClientHandle != IntPtr.Zero)
            {
                // Si le client est détecté, positionner par rapport au client
                PositionOverClientIfAvailable();
                return;
            }
            
            // Sinon, positionner par rapport à l'écran
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            
            switch (SelectedPosition)
            {
                case "TopLeft":
                    this.Left = 10;
                    this.Top = 10;
                    break;
                
                case "TopRight":
                    this.Left = screenWidth - this.Width - 10;
                    this.Top = 10;
                    break;
                
                case "BottomLeft":
                    this.Left = 10;
                    this.Top = screenHeight - this.Height - 10;
                    break;
                
                case "BottomRight":
                    this.Left = screenWidth - this.Width - 10;
                    this.Top = screenHeight - this.Height - 10;
                    break;
                
                case "Center":
                    this.Left = (screenWidth - this.Width) / 2;
                    this.Top = (screenHeight - this.Height) / 2;
                    break;
            }
            
            // Sauvegarde de la position
            _originalLeft = this.Left;
            _originalTop = this.Top;
        }
        
        #endregion

        #region Gestion des animations et de l'interface
        
        /// <summary>
        /// Animation d'expansion/réduction de l'overlay
        /// </summary>
        private void AnimateExpandCollapse()
        {
            if (IsExpanded)
            {
                // Animation d'expansion
                var animation = new DoubleAnimation
                {
                    From = 50,
                    To = _originalHeight,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                
                this.BeginAnimation(HeightProperty, animation);
                
                // Affichage du contenu
                ContentPanel.Visibility = Visibility.Visible;
                
                // Changement de l'icône du bouton
                ExpandCollapseIcon.Text = "▼";
            }
            else
            {
                // Sauvegarde de la hauteur originale
                _originalHeight = this.Height;
                
                // Animation de réduction
                var animation = new DoubleAnimation
                {
                    From = this.Height,
                    To = 50,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                
                this.BeginAnimation(HeightProperty, animation);
                
                // Masquage du contenu
                ContentPanel.Visibility = Visibility.Collapsed;
                
                // Changement de l'icône du bouton
                ExpandCollapseIcon.Text = "▲";
            }
        }
        
        /// <summary>
        /// Animation de mise à jour des données
        /// </summary>
        private void AnimateDataUpdate()
        {
            // Animation de fondu
            var animation = new DoubleAnimation
            {
                From = 0.5,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            ContentPanel.BeginAnimation(OpacityProperty, animation);
        }
        
        /// <summary>
        /// Application du thème sélectionné
        /// </summary>
        private void ApplyTheme()
        {
            ResourceDictionary resourceDict = new ResourceDictionary();
            
            switch (SelectedTheme)
            {
                case "Light":
                    resourceDict.Source = new Uri("/LoLAnalyzer;component/UI/Styles/LightTheme.xaml", UriKind.Relative);
                    break;
                
                case "Blue":
                    resourceDict.Source = new Uri("/LoLAnalyzer;component/UI/Styles/BlueTheme.xaml", UriKind.Relative);
                    break;
                
                case "Red":
                    resourceDict.Source = new Uri("/LoLAnalyzer;component/UI/Styles/RedTheme.xaml", UriKind.Relative);
                    break;
                
                case "Green":
                    resourceDict.Source = new Uri("/LoLAnalyzer;component/UI/Styles/GreenTheme.xaml", UriKind.Relative);
                    break;
                
                default: // Dark
                    resourceDict.Source = new Uri("/LoLAnalyzer;component/UI/Styles/DarkTheme.xaml", UriKind.Relative);
                    break;
            }
            
            // Application du thème
            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(resourceDict);
            
            _logger.Log(LogLevel.Info, $"Thème {SelectedTheme} appliqué");
        }
        
        /// <summary>
        /// Application du mode compact
        /// </summary>
        private void ApplyCompactMode()
        {
            if (IsCompactMode)
            {
                // Réduction de la taille des éléments
                FontSize = 11;
                
                // Masquage des éléments non essentiels
                foreach (var element in CompactHideElements.Children)
                {
                    if (element is FrameworkElement frameworkElement)
                    {
                        frameworkElement.Visibility = Visibility.Collapsed;
                    }
                }
                
                // Réduction de la largeur
                _originalWidth = this.Width;
                this.Width = 300;
            }
            else
            {
                // Restauration de la taille des éléments
                FontSize = 12;
                
                // Affichage des éléments masqués
                foreach (var element in CompactHideElements.Children)
                {
                    if (element is FrameworkElement frameworkElement)
                    {
                        frameworkElement.Visibility = Visibility.Visible;
                    }
                }
                
                // Restauration de la largeur
                this.Width = _originalWidth;
            }
            
            _logger.Log(LogLevel.Info, $"Mode compact: {IsCompactMode}");
        }
        
        /// <summary>
        /// Mise à jour automatique des données
        /// </summary>
        private void AutoUpdateTimer_Tick(object sender, EventArgs e)
        {
            _viewModel.RefreshData();
            AnimateDataUpdate();
        }
        
        /// <summary>
        /// Animation en cours
        /// </summary>
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Animation en cours
        }
        
        #endregion

        #region Gestion des événements de la fenêtre
        
        /// <summary>
        /// Gestion du déplacement de la fenêtre
        /// </summary>
        private void OverlayWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.OriginalSource is FrameworkElement element && element.Name == "DragHandle")
            {
                _isDragging = true;
                _startPoint = e.GetPosition(this);
                this.CaptureMouse();
                e.Handled = true;
            }
        }
        
        /// <summary>
        /// Gestion du déplacement de la fenêtre
        /// </summary>
        private void OverlayWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(this);
                Vector offset = currentPosition - _startPoint;
                
                this.Left += offset.X;
                this.Top += offset.Y;
                
                _originalLeft = this.Left;
                _originalTop = this.Top;
            }
        }
        
        /// <summary>
        /// Gestion du déplacement de la fenêtre
        /// </summary>
        private void OverlayWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.ReleaseMouseCapture();
                e.Handled = true;
                
                // Sauvegarde de la position
                SaveConfiguration();
            }
        }
        
        /// <summary>
        /// Gestion du redimensionnement de la fenêtre
        /// </summary>
        private void OverlayWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsExpanded)
            {
                _originalWidth = this.Width;
                _originalHeight = this.Height;
                
                // Sauvegarde de la taille
                SaveConfiguration();
            }
        }
        
        /// <summary>
        /// Gestion du bouton d'expansion/réduction
        /// </summary>
        private void ExpandCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            IsExpanded = !IsExpanded;
        }
        
        /// <summary>
        /// Gestion du bouton de fermeture
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Sauvegarde de la configuration
            SaveConfiguration();
            
            // Fermeture de la fenêtre
            this.Close();
        }
        
        /// <summary>
        /// Gestion du bouton de paramètres
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Affichage/masquage du panneau de paramètres
            SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
        
        /// <summary>
        /// Gestion du bouton de rafraîchissement
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Mise à jour des données
            _viewModel.RefreshData();
            AnimateDataUpdate();
        }
        
        /// <summary>
        /// Gestion du bouton de minimisation
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        
        /// <summary>
        /// Gestion du bouton d'aide
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            // Affichage de l'aide
            _viewModel.ShowHelp();
        }
        
        /// <summary>
        /// Gestion du bouton de profil
        /// </summary>
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            // Affichage du profil
            _viewModel.ShowProfile();
        }
        
        #endregion

        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
}
