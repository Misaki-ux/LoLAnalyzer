using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LoLAnalyzer.UI.Windows
{
    /// <summary>
    /// Logique d'interaction pour OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private readonly Core.Utils.Logger _logger;
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
        public OverlayWindow(Core.Utils.Logger logger)
        {
            InitializeComponent();

            _logger = logger;
            _logger.Log(Core.Utils.LogLevel.Info, "Initialisation de la fenêtre d'overlay");

            // Chargement des données de test
            LoadTestData();
        }

        /// <summary>
        /// Charge des données de test pour la démonstration
        /// </summary>
        private void LoadTestData()
        {
            try
            {
                // Données de test pour l'équipe bleue
                var blueTeamMembers = new System.Collections.ObjectModel.ObservableCollection<object>
                {
                    new { ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Darius.png", ChampionName = "Darius", Role = "TOP" },
                    new { ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Amumu.png", ChampionName = "Amumu", Role = "JUNGLE" },
                    new { ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Ahri.png", ChampionName = "Ahri", Role = "MID" },
                    new { ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Jinx.png", ChampionName = "Jinx", Role = "ADC" },
                    new { ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Thresh.png", ChampionName = "Thresh", Role = "SUPPORT" }
                };
                BlueTeamList.ItemsSource = blueTeamMembers;

                // Forces de l'équipe bleue
                var blueTeamStrengths = new System.Collections.ObjectModel.ObservableCollection<string>
                {
                    "Forte présence en early game",
                    "Bon contrôle des objectifs",
                    "Excellente synergie entre Amumu et Ahri"
                };
                BlueTeamStrengths.ItemsSource = blueTeamStrengths;

                // Faiblesses de l'équipe bleue
                var blueTeamWeaknesses = new System.Collections.ObjectModel.ObservableCollection<string>
                {
                    "Manque de mobilité globale",
                    "Vulnérable aux compositions de poke"
                };
                BlueTeamWeaknesses.ItemsSource = blueTeamWeaknesses;

                // Données de test pour l'équipe rouge
                var redTeamMembers = new System.Collections.ObjectModel.ObservableCollection<object>
                {
                    new { ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Fiora.png", ChampionName = "Fiora", Role = "TOP" },
                    new { ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Teemo.png", ChampionName = "Teemo", Role = "JUNGLE" },
                    new { ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Yasuo.png", ChampionName = "Yasuo", Role = "MID" },
                    new { ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Ezreal.png", ChampionName = "Ezreal", Role = "ADC" },
                    new { ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Lulu.png", ChampionName = "Lulu", Role = "SUPPORT" }
                };
                RedTeamList.ItemsSource = redTeamMembers;

                // Forces de l'équipe rouge
                var redTeamStrengths = new System.Collections.ObjectModel.ObservableCollection<string>
                {
                    "Excellente mobilité",
                    "Fort potentiel de split push avec Fiora",
                    "Bonne synergie entre Yasuo et Lulu"
                };
                RedTeamStrengths.ItemsSource = redTeamStrengths;

                // Faiblesses de l'équipe rouge
                var redTeamWeaknesses = new System.Collections.ObjectModel.ObservableCollection<string>
                {
                    "Teemo jungle est un pick problématique",
                    "Composition fragile en teamfight",
                    "Manque de CC dur"
                };
                RedTeamWeaknesses.ItemsSource = redTeamWeaknesses;

                // Données de test pour les contre-picks
                var counterPicks = new System.Collections.ObjectModel.ObservableCollection<object>
                {
                    new { 
                        ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Malphite.png", 
                        ChampionName = "Malphite", 
                        WinRate = 0.58, 
                        Reason = "Excellent contre Fiora en lane et contre la composition à forte mobilité de l'équipe rouge. Son ultimate est particulièrement efficace contre Yasuo et Ezreal.",
                        Difficulty = "Difficulté: Facile"
                    },
                    new { 
                        ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Shen.png", 
                        ChampionName = "Shen", 
                        WinRate = 0.53, 
                        Reason = "Peut contrer Fiora en lane et apporter un soutien global à l'équipe avec son ultimate. Sa taunt est efficace contre les champions mobiles.",
                        Difficulty = "Difficulté: Moyenne"
                    },
                    new { 
                        ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Garen.png", 
                        ChampionName = "Garen", 
                        WinRate = 0.51, 
                        Reason = "Option solide contre Fiora, avec une bonne sustain en lane et des dégâts importants en mid-game. Simple à jouer et efficace.",
                        Difficulty = "Difficulté: Facile"
                    }
                };
                CounterPicksList.ItemsSource = counterPicks;

                // Données de test pour les picks problématiques
                var trollPicks = new System.Collections.ObjectModel.ObservableCollection<object>
                {
                    new { 
                        ChampionImageUrl = "https://ddragon.leagueoflegends.com/cdn/11.24.1/img/champion/Teemo.png", 
                        ChampionName = "Teemo", 
                        Role = "JUNGLE", 
                        TrollPickReason = "Teemo jungle a un taux de victoire de seulement 42% dans ce rôle. Sa clairance de jungle est lente et ses ganks sont faibles avant le niveau 6. Il est très vulnérable aux contre-jungling.",
                        Alternatives = new[] { "Lee Sin", "Elise", "Kha'Zix" }
                    }
                };
                TrollPicksList.ItemsSource = trollPicks;

                _logger.Log(Core.Utils.LogLevel.Info, "Données de test chargées avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogException(Core.Utils.LogLevel.Error, "Erreur lors du chargement des données de test", ex);
            }
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
        /// Gère le clic sur le bouton de fermeture
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            _logger.Log(Core.Utils.LogLevel.Info, "Overlay fermé");
        }

        /// <summary>
        /// Gère le clic sur le bouton de minimisation
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            _logger.Log(Core.Utils.LogLevel.Info, "Overlay minimisé");
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
        private void AnalyzeCounterPicks_Click(object sender, RoutedEventArgs e)
        {
            string role = (RoleSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
            _logger.Log(Core.Utils.LogLevel.Info, $"Analyse des contre-picks pour le rôle {role}");
            
            // Dans une version complète, cette méthode appellerait le service d'analyse
            // pour obtenir des contre-picks spécifiques au rôle sélectionné
            
            MessageBox.Show($"Analyse des contre-picks pour le rôle {role} effectuée.", 
                "Analyse", MessageBoxButton.OK, MessageBoxImage.Information);
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
