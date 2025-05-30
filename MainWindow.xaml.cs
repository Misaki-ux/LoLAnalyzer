using System;
using System.Windows;
using System.Windows.Input;
using LoLAnalyzer.UI.Windows;

namespace LoLAnalyzer
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OverlayWindow _overlayWindow;

        /// <summary>
        /// Constructeur de la fenêtre principale
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // Initialisation du statut
            UpdateStatus("Application prête");
        }

        /// <summary>
        /// Gère le clic sur le bouton "Lancer l'overlay"
        /// </summary>
        private void LaunchOverlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupération des informations de configuration
                string summonerName = SummonerNameTextBox.Text;
                string region = (RegionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                string mainRole = (MainRoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                
                // Validation des entrées
                if (string.IsNullOrWhiteSpace(summonerName))
                {
                    MessageBox.Show("Veuillez entrer un nom d'invocateur.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Mise à jour du statut
                UpdateStatus($"Lancement de l'overlay pour {summonerName} ({region})...");
                
                // Création et affichage de la fenêtre d'overlay
                if (_overlayWindow == null || !_overlayWindow.IsVisible)
                {
                    _overlayWindow = new OverlayWindow(App.Logger);
                    _overlayWindow.Show();
                    
                    // Positionnement de l'overlay
                    _overlayWindow.Left = SystemParameters.PrimaryScreenWidth / 2 - _overlayWindow.Width / 2;
                    _overlayWindow.Top = SystemParameters.PrimaryScreenHeight / 2 - _overlayWindow.Height / 2;
                    
                    UpdateStatus($"Overlay lancé pour {summonerName} ({region})");
                }
                else
                {
                    _overlayWindow.Activate();
                    UpdateStatus("Overlay déjà actif");
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogException(Core.Utils.LogLevel.Error, "Erreur lors du lancement de l'overlay", ex);
                MessageBox.Show($"Erreur lors du lancement de l'overlay: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Erreur lors du lancement de l'overlay");
            }
        }

        /// <summary>
        /// Gère le clic sur le bouton "Paramètres avancés"
        /// </summary>
        private void AdvancedSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Les paramètres avancés ne sont pas encore implémentés dans cette version.", 
                "Paramètres avancés", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Met à jour le statut affiché dans la barre d'état
        /// </summary>
        private void UpdateStatus(string status)
        {
            StatusTextBlock.Text = status;
            App.Logger.Info(status);
        }
    }
}
