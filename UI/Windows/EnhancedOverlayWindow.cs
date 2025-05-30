using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using LoLAnalyzer.Core.Models;
using LoLAnalyzer.Core.Services;
using LoLAnalyzer.Core.Data;
using LoLAnalyzer.Core.Utils;

namespace LoLAnalyzer.UI.Windows
{
    /// <summary>
    /// Logique d'interaction pour EnhancedOverlayWindow.xaml
    /// </summary>
    public partial class EnhancedOverlayWindow : Window
    {
        private readonly Logger _logger;
        private readonly EnhancedDatabaseManager _dbManager;
        private readonly EnhancedCompositionAnalyzer _compositionAnalyzer;
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
        /// Constructeur de la fenêtre d'overlay améliorée
        /// </summary>
        public EnhancedOverlayWindow(Logger logger)
        {
            InitializeComponent();

            _logger = logger;
            _logger.Log(LogLevel.Info, "Initialisation de la fenêtre d'overlay améliorée");

            // Initialiser la base de données
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LoLAnalyzer",
                "Database",
                "lolanalyzer.db");

            _dbManager = new EnhancedDatabaseManager(dbPath, _logger);
            _compositionAnalyzer = new EnhancedCompositionAnalyzer(_dbManager, _logger);

            // Initialiser la base de données de manière asynchrone
            InitializeDatabaseAsync();
        }

        /// <summary>
        /// Initialise la base de données de manière asynchrone
        /// </summary>
        private async void InitializeDatabaseAsync()
        {
            try
            {
                StatusText.Text = "Initialisation de la base de données...";
                await _dbManager.InitializeAsync();
                StatusText.Text = "Base de données initialisée avec succès";

                // Charger les données de test
                await LoadTestDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'initialisation de la base de données", ex);
                StatusText.Text = "Erreur lors de l'initialisation de la base de données";
                MessageBox.Show($"Erreur lors de l'initialisation de la base de données: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Charge des données de test pour la démonstration
        /// </summary>
        private async Task LoadTestDataAsync()
        {
            try
            {
                StatusText.Text = "Chargement des données de test...";

                // Créer les champions pour l'équipe bleue
                var blueTeamMembers = new List<TeamMember>
                {
                    new TeamMember { Champion = new Champion { Id = 3, Name = "Darius", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Darius.png" }, Role = "TOP" },
                    new TeamMember { Champion = new Champion { Id = 2, Name = "Amumu", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Amumu.png" }, Role = "JUNGLE" },
                    new TeamMember { Champion = new Champion { Id = 1, Name = "Ahri", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Ahri.png" }, Role = "MID" },
                    new TeamMember { Champion = new Champion { Id = 7, Name = "Jinx", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Jinx.png" }, Role = "ADC" },
                    new TeamMember { Champion = new Champion { Id = 13, Name = "Thresh", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Thresh.png" }, Role = "SUPPORT" }
                };

                // Créer les champions pour l'équipe rouge
                var redTeamMembers = new List<TeamMember>
                {
                    new TeamMember { Champion = new Champion { Id = 5, Name = "Fiora", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Fiora.png" }, Role = "TOP" },
                    new TeamMember { Champion = new Champion { Id = 12, Name = "Teemo", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Teemo.png" }, Role = "JUNGLE" },
                    new TeamMember { Champion = new Champion { Id = 14, Name = "Yasuo", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Yasuo.png" }, Role = "MID" },
                    new TeamMember { Champion = new Champion { Id = 4, Name = "Ezreal", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Ezreal.png" }, Role = "ADC" },
                    new TeamMember { Champion = new Champion { Id = 9, Name = "Lulu", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Lulu.png" }, Role = "SUPPORT" }
                };

                // Analyser les compositions
                var blueTeamAnalysis = await _compositionAnalyzer.AnalyzeTeamCompositionAsync(blueTeamMembers);
                var redTeamAnalysis = await _compositionAnalyzer.AnalyzeTeamCompositionAsync(redTeamMembers);

                // Afficher les données de l'équipe bleue
                BlueTeamList.ItemsSource = blueTeamMembers;
                BlueTeamStrengths.ItemsSource = blueTeamAnalysis.Strengths;
                BlueTeamWeaknesses.ItemsSource = blueTeamAnalysis.Weaknesses;

                // Afficher les données de l'équipe rouge
                RedTeamList.ItemsSource = redTeamMembers;
                RedTeamStrengths.ItemsSource = redTeamAnalysis.Strengths;
                RedTeamWeaknesses.ItemsSource = redTeamAnalysis.Weaknesses;

                // Afficher les données de distribution des dégâts
                UpdateDamageDistribution(BlueTeamDamageDistribution, blueTeamAnalysis.DamageDistribution);
                UpdateDamageDistribution(RedTeamDamageDistribution, redTeamAnalysis.DamageDistribution);

                // Afficher les données de performance par phase de jeu
                UpdatePhasePerformance(BlueTeamPhasePerformance, blueTeamAnalysis.PhasePerformance);
                UpdatePhasePerformance(RedTeamPhasePerformance, redTeamAnalysis.PhasePerformance);

                // Afficher les picks problématiques
                TrollPicksList.ItemsSource = redTeamAnalysis.TrollPicks;

                // Suggérer des contre-picks pour le rôle TOP
                var counterPicks = await _compositionAnalyzer.SuggestCounterPicksAsync(redTeamMembers, "TOP");
                CounterPicksList.ItemsSource = counterPicks;

                StatusText.Text = "Données de test chargées avec succès";
                _logger.Log(LogLevel.Info, "Données de test chargées avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors du chargement des données de test", ex);
                StatusText.Text = "Erreur lors du chargement des données de test";
                MessageBox.Show($"Erreur lors du chargement des données de test: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Met à jour l'affichage de la distribution des dégâts
        /// </summary>
        private void UpdateDamageDistribution(StackPanel panel, DamageDistribution distribution)
        {
            panel.Children.Clear();

            // Créer les barres de distribution des dégâts
            AddDistributionBar(panel, "Physique", distribution.Physical, Brushes.IndianRed);
            AddDistributionBar(panel, "Magique", distribution.Magical, Brushes.DodgerBlue);
            AddDistributionBar(panel, "Vrai", distribution.True, Brushes.White);
        }

        /// <summary>
        /// Ajoute une barre de distribution des dégâts
        /// </summary>
        private void AddDistributionBar(StackPanel panel, string label, double value, Brush color)
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            var labelText = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 5, 0)
            };
            Grid.SetColumn(labelText, 0);

            var barBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Height = 15,
                CornerRadius = new CornerRadius(2)
            };
            Grid.SetColumn(barBorder, 1);

            var barFill = new Border
            {
                Background = color,
                Width = value * barBorder.ActualWidth,
                HorizontalAlignment = HorizontalAlignment.Left,
                Height = 15,
                CornerRadius = new CornerRadius(2)
            };

            barBorder.Child = barFill;
            barBorder.Loaded += (s, e) => { barFill.Width = value * barBorder.ActualWidth; };

            var percentText = new TextBlock
            {
                Text = $"{value:P0}",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Margin = new Thickness(5, 0, 0, 0)
            };
            Grid.SetColumn(percentText, 2);

            grid.Children.Add(labelText);
            grid.Children.Add(barBorder);
            grid.Children.Add(percentText);

            panel.Children.Add(grid);
        }

        /// <summary>
        /// Met à jour l'affichage des performances par phase de jeu
        /// </summary>
        private void UpdatePhasePerformance(StackPanel panel, PhasePerformance performance)
        {
            panel.Children.Clear();

            // Créer les barres de performance par phase de jeu
            AddPerformanceBar(panel, "Early", performance.Early, GetPerformanceColor(performance.Early));
            AddPerformanceBar(panel, "Mid", performance.Mid, GetPerformanceColor(performance.Mid));
            AddPerformanceBar(panel, "Late", performance.Late, GetPerformanceColor(performance.Late));
        }

        /// <summary>
        /// Ajoute une barre de performance par phase de jeu
        /// </summary>
        private void AddPerformanceBar(StackPanel panel, string label, double value, Brush color)
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

            var labelText = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 5, 0)
            };
            Grid.SetColumn(labelText, 0);

            var barBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Height = 15,
                CornerRadius = new CornerRadius(2)
            };
            Grid.SetColumn(barBorder, 1);

            var barFill = new Border
            {
                Background = color,
                Width = value * barBorder.ActualWidth,
                HorizontalAlignment = HorizontalAlignment.Left,
                Height = 15,
                CornerRadius = new CornerRadius(2)
            };

            barBorder.Child = barFill;
            barBorder.Loaded += (s, e) => { barFill.Width = value * barBorder.ActualWidth; };

            var scoreText = new TextBlock
            {
                Text = $"{value:F2}",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Margin = new Thickness(5, 0, 0, 0)
            };
            Grid.SetColumn(scoreText, 2);

            grid.Children.Add(labelText);
            grid.Children.Add(barBorder);
            grid.Children.Add(scoreText);

            panel.Children.Add(grid);
        }

        /// <summary>
        /// Obtient la couleur en fonction du score de performance
        /// </summary>
        private Brush GetPerformanceColor(double value)
        {
            if (value >= 0.7)
                return Brushes.LimeGreen;
            else if (value >= 0.5)
                return Brushes.Yellow;
            else
                return Brushes.Red;
        }

        /// <summary>
        /// Gère l'événement MouseLeftButtonDown pour le déplacement et le redimensionnement
        /// </summary>
        private void OverlayWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
        private void OverlayWindow_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = false;
            _isResizing = false;
            this.ReleaseMouseCapture();
            this.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        /// <summary>
        /// Gère l'événement MouseMove pour le déplacement et le redimensionnement
        /// </summary>
        private void OverlayWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
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
                        this.Cursor = System.Windows.Input.Cursors.SizeNWSE;
                        break;
                    case ResizeDirection.Top:
                    case ResizeDirection.Bottom:
                        this.Cursor = System.Windows.Input.Cursors.SizeNS;
                        break;
                    case ResizeDirection.TopRight:
                    case ResizeDirection.BottomLeft:
                        this.Cursor = System.Windows.Input.Cursors.SizeNESW;
                        break;
                    case ResizeDirection.Left:
                    case ResizeDirection.Right:
                        this.Cursor = System.Windows.Input.Cursors.SizeWE;
                        break;
                    default:
                        this.Cursor = System.Windows.Input.Cursors.Arrow;
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
        /// Gère le clic sur le bouton de fermeture
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            _logger.Log(LogLevel.Info, "Overlay fermé");
        }

        /// <summary>
        /// Gère le clic sur le bouton de minimisation
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            _logger.Log(LogLevel.Info, "Overlay minimisé");
        }

        /// <summary>
        /// Gère le clic sur le bouton des paramètres
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Les paramètres ne sont pas encore implémentés dans cette version.", 
                "Paramètres", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Gère le clic sur le bouton d'analyse des contre-picks
        /// </summary>
        private async void AnalyzeCounterPicks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string role = (RoleSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (string.IsNullOrEmpty(role))
                {
                    MessageBox.Show("Veuillez sélectionner un rôle.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _logger.Log(LogLevel.Info, $"Analyse des contre-picks pour le rôle {role}");
                StatusText.Text = $"Analyse des contre-picks pour le rôle {role}...";

                // Récupérer l'équipe rouge (ennemie)
                var redTeamMembers = RedTeamList.ItemsSource as List<TeamMember>;
                if (redTeamMembers == null || redTeamMembers.Count == 0)
                {
                    MessageBox.Show("Aucune équipe ennemie n'est définie.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Suggérer des contre-picks
                var counterPicks = await _compositionAnalyzer.SuggestCounterPicksAsync(redTeamMembers, role);
                CounterPicksList.ItemsSource = counterPicks;

                StatusText.Text = $"Analyse des contre-picks pour le rôle {role} terminée";
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'analyse des contre-picks", ex);
                StatusText.Text = "Erreur lors de l'analyse des contre-picks";
                MessageBox.Show($"Erreur lors de l'analyse des contre-picks: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gère le clic sur le bouton d'analyse des alternatives aux picks problématiques
        /// </summary>
        private async void AnalyzeTrollPickAlternatives_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var trollPick = TrollPicksList.SelectedItem as TeamMember;
                if (trollPick == null)
                {
                    MessageBox.Show("Veuillez sélectionner un pick problématique.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _logger.Log(LogLevel.Info, $"Analyse des alternatives pour {trollPick.Champion.Name} {trollPick.Role}");
                StatusText.Text = $"Analyse des alternatives pour {trollPick.Champion.Name} {trollPick.Role}...";

                // Suggérer des alternatives
                var alternatives = await _compositionAnalyzer.SuggestTrollPickAlternativesAsync(trollPick);
                TrollPickAlternativesList.ItemsSource = alternatives;

                StatusText.Text = $"Analyse des alternatives pour {trollPick.Champion.Name} {trollPick.Role} terminée";
            }
            catch (Exception ex)
            {
                _logger.LogException(LogLevel.Error, "Erreur lors de l'analyse des alternatives", ex);
                StatusText.Text = "Erreur lors de l'analyse des alternatives";
                MessageBox.Show($"Erreur lors de l'analyse des alternatives: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                this.Width = 800;
                this.Height = 600;
            }
        }
    }
}
