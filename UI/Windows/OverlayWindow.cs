using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.UI.Windows
{
    /// <summary>
    /// Fenêtre d'overlay transparente qui se superpose au client League of Legends
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private readonly Logger _logger;
        private Point _lastPosition;
        private bool _isDragging = false;
        private bool _isResizing = false;
        private ResizeDirection _resizeDirection;

        // Constantes pour la taille minimale de la fenêtre
        private const double MIN_WIDTH = 300;
        private const double MIN_HEIGHT = 200;

        // Constantes pour les marges de redimensionnement
        private const double RESIZE_MARGIN = 10;

        // Énumération pour les directions de redimensionnement
        private enum ResizeDirection
        {
            None,
            TopLeft,
            Top,
            TopRight,
            Right,
            BottomRight,
            Bottom,
            BottomLeft,
            Left
        }

        /// <summary>
        /// Constructeur de la fenêtre d'overlay
        /// </summary>
        public OverlayWindow(Logger logger)
        {
            InitializeComponent();

            _logger = logger;
            _logger.Log(LogLevel.Info, "Initialisation de la fenêtre d'overlay");

            // Configuration de la fenêtre pour qu'elle soit transparente et toujours au premier plan
            this.AllowsTransparency = true;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;
            this.ResizeMode = ResizeMode.NoResize;
            this.ShowInTaskbar = false;

            // Définition de la couleur de fond avec transparence
            this.Background = new SolidColorBrush(Color.FromArgb(230, 30, 30, 30));

            // Gestion des événements de souris pour le déplacement et le redimensionnement
            this.MouseLeftButtonDown += OverlayWindow_MouseLeftButtonDown;
            this.MouseLeftButtonUp += OverlayWindow_MouseLeftButtonUp;
            this.MouseMove += OverlayWindow_MouseMove;

            // Positionnement initial de la fenêtre
            this.Left = SystemParameters.PrimaryScreenWidth / 2 - this.Width / 2;
            this.Top = SystemParameters.PrimaryScreenHeight / 2 - this.Height / 2;

            // Chargement des contrôles d'interface utilisateur
            LoadUIControls();
        }

        /// <summary>
        /// Initialise les composants de la fenêtre
        /// </summary>
        private void InitializeComponent()
        {
            this.Width = 400;
            this.Height = 600;
            this.Title = "LoL Analyzer Overlay";
        }

        /// <summary>
        /// Charge les contrôles d'interface utilisateur
        /// </summary>
        private void LoadUIControls()
        {
            // Création du contenu de la fenêtre
            var grid = new System.Windows.Controls.Grid();
            
            // Définition des lignes
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(30) }); // Barre de titre
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(40) }); // Onglets
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Contenu principal
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(30) }); // Barre d'état

            // Barre de titre
            var titleBar = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 20, 20, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            var titlePanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            var titleLabel = new System.Windows.Controls.Label
            {
                Content = "LoL Analyzer",
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 0, 0, 0)
            };

            var closeButton = new System.Windows.Controls.Button
            {
                Content = "X",
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 10, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Foreground = new SolidColorBrush(Colors.White)
            };
            closeButton.Click += (sender, e) => this.Hide();

            var minimizeButton = new System.Windows.Controls.Button
            {
                Content = "_",
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 5, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Foreground = new SolidColorBrush(Colors.White)
            };
            minimizeButton.Click += (sender, e) => this.WindowState = WindowState.Minimized;

            var settingsButton = new System.Windows.Controls.Button
            {
                Content = "⚙",
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 5, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Foreground = new SolidColorBrush(Colors.White)
            };
            settingsButton.Click += (sender, e) => ShowSettings();

            var titleBarPanel = new System.Windows.Controls.DockPanel();
            titleBarPanel.Children.Add(titleLabel);
            
            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttonPanel.Children.Add(settingsButton);
            buttonPanel.Children.Add(minimizeButton);
            buttonPanel.Children.Add(closeButton);
            
            titleBarPanel.Children.Add(buttonPanel);
            System.Windows.Controls.DockPanel.SetDock(buttonPanel, System.Windows.Controls.Dock.Right);
            
            titleBar.Child = titleBarPanel;
            grid.Children.Add(titleBar);
            System.Windows.Controls.Grid.SetRow(titleBar, 0);

            // Onglets
            var tabControl = new System.Windows.Controls.TabControl
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
            };

            // Onglet Analyse
            var analysisTab = new System.Windows.Controls.TabItem
            {
                Header = "Analyse",
                Foreground = new SolidColorBrush(Colors.White)
            };
            var analysisContent = new System.Windows.Controls.TextBlock
            {
                Text = "Analyse de composition d'équipe",
                Foreground = new SolidColorBrush(Colors.White),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            analysisTab.Content = analysisContent;
            tabControl.Items.Add(analysisTab);

            // Onglet Contre-picks
            var counterPicksTab = new System.Windows.Controls.TabItem
            {
                Header = "Contre-picks",
                Foreground = new SolidColorBrush(Colors.White)
            };
            var counterPicksContent = new System.Windows.Controls.TextBlock
            {
                Text = "Suggestions de contre-picks",
                Foreground = new SolidColorBrush(Colors.White),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            counterPicksTab.Content = counterPicksContent;
            tabControl.Items.Add(counterPicksTab);

            // Onglet Détection
            var detectionTab = new System.Windows.Controls.TabItem
            {
                Header = "Détection",
                Foreground = new SolidColorBrush(Colors.White)
            };
            var detectionContent = new System.Windows.Controls.TextBlock
            {
                Text = "Détection des picks problématiques",
                Foreground = new SolidColorBrush(Colors.White),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            detectionTab.Content = detectionContent;
            tabControl.Items.Add(detectionTab);

            grid.Children.Add(tabControl);
            System.Windows.Controls.Grid.SetRow(tabControl, 1);
            System.Windows.Controls.Grid.SetRowSpan(tabControl, 2);

            // Barre d'état
            var statusBar = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 20, 20, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50)),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };

            var statusLabel = new System.Windows.Controls.Label
            {
                Content = "Prêt",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 10
            };

            statusBar.Child = statusLabel;
            grid.Children.Add(statusBar);
            System.Windows.Controls.Grid.SetRow(statusBar, 3);

            this.Content = grid;
        }

        /// <summary>
        /// Affiche la fenêtre de paramètres
        /// </summary>
        private void ShowSettings()
        {
            _logger.Log(LogLevel.Info, "Affichage des paramètres");
            MessageBox.Show("Paramètres non implémentés dans cette version", "Paramètres", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Gère l'événement MouseLeftButtonDown pour le déplacement et le redimensionnement
        /// </summary>
        private void OverlayWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastPosition = e.GetPosition(this);
            
            // Déterminer si l'utilisateur est en train de redimensionner ou de déplacer la fenêtre
            _resizeDirection = GetResizeDirection(_lastPosition);
            
            if (_resizeDirection != ResizeDirection.None)
            {
                _isResizing = true;
                _isDragging = false;
                this.CaptureMouse();
            }
            else if (e.GetPosition(this).Y < 30) // Clic dans la barre de titre
            {
                _isDragging = true;
                _isResizing = false;
                this.CaptureMouse();
            }
        }

        /// <summary>
        /// Gère l'événement MouseLeftButtonUp pour le déplacement et le redimensionnement
        /// </summary>
        private void OverlayWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _isResizing = false;
            this.ReleaseMouseCapture();
            this.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Gère l'événement MouseMove pour le déplacement et le redimensionnement
        /// </summary>
        private void OverlayWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(this);
                double deltaX = currentPosition.X - _lastPosition.X;
                double deltaY = currentPosition.Y - _lastPosition.Y;
                
                this.Left += deltaX;
                this.Top += deltaY;
            }
            else if (_isResizing)
            {
                Point currentPosition = e.GetPosition(this);
                double deltaX = currentPosition.X - _lastPosition.X;
                double deltaY = currentPosition.Y - _lastPosition.Y;
                
                switch (_resizeDirection)
                {
                    case ResizeDirection.TopLeft:
                        ResizeFromTopLeft(deltaX, deltaY);
                        break;
                    case ResizeDirection.Top:
                        ResizeFromTop(deltaY);
                        break;
                    case ResizeDirection.TopRight:
                        ResizeFromTopRight(deltaX, deltaY);
                        break;
                    case ResizeDirection.Right:
                        ResizeFromRight(deltaX);
                        break;
                    case ResizeDirection.BottomRight:
                        ResizeFromBottomRight(deltaX, deltaY);
                        break;
                    case ResizeDirection.Bottom:
                        ResizeFromBottom(deltaY);
                        break;
                    case ResizeDirection.BottomLeft:
                        ResizeFromBottomLeft(deltaX, deltaY);
                        break;
                    case ResizeDirection.Left:
                        ResizeFromLeft(deltaX);
                        break;
                }
                
                _lastPosition = currentPosition;
            }
            else
            {
                // Mettre à jour le curseur en fonction de la position de la souris
                _resizeDirection = GetResizeDirection(e.GetPosition(this));
                
                switch (_resizeDirection)
                {
                    case ResizeDirection.TopLeft:
                    case ResizeDirection.BottomRight:
                        this.Cursor = Cursors.SizeNWSE;
                        break;
                    case ResizeDirection.Top:
                    case ResizeDirection.Bottom:
                        this.Cursor = Cursors.SizeNS;
                        break;
                    case ResizeDirection.TopRight:
                    case ResizeDirection.BottomLeft:
                        this.Cursor = Cursors.SizeNESW;
                        break;
                    case ResizeDirection.Left:
                    case ResizeDirection.Right:
                        this.Cursor = Cursors.SizeWE;
                        break;
                    default:
                        this.Cursor = Cursors.Arrow;
                        break;
                }
            }
        }

        /// <summary>
        /// Détermine la direction de redimensionnement en fonction de la position de la souris
        /// </summary>
        private ResizeDirection GetResizeDirection(Point position)
        {
            if (position.X < RESIZE_MARGIN && position.Y < RESIZE_MARGIN)
                return ResizeDirection.TopLeft;
            else if (position.X > this.ActualWidth - RESIZE_MARGIN && position.Y < RESIZE_MARGIN)
                return ResizeDirection.TopRight;
            else if (position.X < RESIZE_MARGIN && position.Y > this.ActualHeight - RESIZE_MARGIN)
                return ResizeDirection.BottomLeft;
            else if (position.X > this.ActualWidth - RESIZE_MARGIN && position.Y > this.ActualHeight - RESIZE_MARGIN)
                return ResizeDirection.BottomRight;
            else if (position.X < RESIZE_MARGIN)
                return ResizeDirection.Left;
            else if (position.X > this.ActualWidth - RESIZE_MARGIN)
                return ResizeDirection.Right;
            else if (position.Y < RESIZE_MARGIN)
                return ResizeDirection.Top;
            else if (position.Y > this.ActualHeight - RESIZE_MARGIN)
                return ResizeDirection.Bottom;
            else
                return ResizeDirection.None;
        }

        /// <summary>
        /// Redimensionne la fenêtre depuis le coin supérieur gauche
        /// </summary>
        private void ResizeFromTopLeft(double deltaX, double deltaY)
        {
            double newWidth = Math.Max(MIN_WIDTH, this.Width - deltaX);
            double newHeight = Math.Max(MIN_HEIGHT, this.Height - deltaY);
            
            double widthDelta = this.Width - newWidth;
            double heightDelta = this.Height - newHeight;
            
            this.Left += widthDelta;
            this.Top += heightDelta;
            this.Width = newWidth;
            this.Height = newHeight;
        }

        /// <summary>
        /// Redimensionne la fenêtre depuis le haut
        /// </summary>
        private void ResizeFromTop(double deltaY)
        {
            double newHeight = Math.Max(MIN_HEIGHT, this.Height - deltaY);
            double heightDelta = this.Height - newHeight;
            
            this.Top += heightDelta;
            this.Height = newHeight;
        }

        /// <summary>
        /// Redimensionne la fenêtre depuis le coin supérieur droit
        /// </summary>
        private void ResizeFromTopRight(double deltaX, double deltaY)
        {
            double newWidth = Math.Max(MIN_WIDTH, this.Width + deltaX);
            double newHeight = Math.Max(MIN_HEIGHT, this.Height - deltaY);
            
            double heightDelta = this.Height - newHeight;
            
            this.Top += heightDelta;
            this.Width = newWidth;
            this.Height = newHeight;
        }

        /// <summary>
        /// Redimensionne la fenêtre depuis la droite
        /// </summary>
        private void ResizeFromRight(double deltaX)
        {
            this.Width = Math.Max(MIN_WIDTH, this.Width + deltaX);
        }

        /// <summary>
        /// Redimensionne la fenêtre depuis le coin inférieur droit
        /// </summary>
        private void ResizeFromBottomRight(double deltaX, double deltaY)
        {
            this.Width = Math.Max(MIN_WIDTH, this.Width + deltaX);
            this.Height = Math.Max(MIN_HEIGHT, this.Height + deltaY);
        }

        /// <summary>
        /// Redimensionne la fenêtre depuis le bas
        /// </summary>
        private void ResizeFromBottom(double deltaY)
        {
            this.Height = Math.Max(MIN_HEIGHT, this.Height + deltaY);
        }

        /// <summary>
        /// Redimensionne la fenêtre depuis le coin inférieur gauche
        /// </summary>
        private void ResizeFromBottomLeft(double deltaX, double deltaY)
        {
            double newWidth = Math.Max(MIN_WIDTH, this.Width - deltaX);
            double widthDelta = this.Width - newWidth;
            
            this.Left += widthDelta;
            this.Width = newWidth;
            this.Height = Math.Max(MIN_HEIGHT, this.Height + deltaY);
        }

        /// <summary>
        /// Redimensionne la fenêtre depuis la gauche
        /// </summary>
        private void ResizeFromLeft(double deltaX)
        {
            double newWidth = Math.Max(MIN_WIDTH, this.Width - deltaX);
            double widthDelta = this.Width - newWidth;
            
            this.Left += widthDelta;
            this.Width = newWidth;
        }

        /// <summary>
        /// Définit l'opacité de la fenêtre
        /// </summary>
        public void SetOpacity(double opacity)
        {
            this.Opacity = Math.Max(0.1, Math.Min(1.0, opacity));
        }

        /// <summary>
        /// Active ou désactive le mode compact
        /// </summary>
        public void SetCompactMode(bool isCompact)
        {
            if (isCompact)
            {
                this.Width = 250;
                this.Height = 300;
            }
            else
            {
                this.Width = 400;
                this.Height = 600;
            }
        }
    }
}
