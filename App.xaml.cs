using System;
using System.Windows;

namespace LoLAnalyzer
{
    /// <summary>
    /// Classe principale de l'application
    /// </summary>
    public partial class App : Application
    {
        private static Core.Utils.Logger _logger;

        /// <summary>
        /// Logger global de l'application
        /// </summary>
        public static Core.Utils.Logger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new Core.Utils.Logger("LoLAnalyzer");
                }
                return _logger;
            }
        }

        /// <summary>
        /// Événement de démarrage de l'application
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            Logger.Info("Application démarrée");
            
            // Initialisation des services
            InitializeServices();
            
            // Gestion des exceptions non gérées
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        /// <summary>
        /// Initialise les services de l'application
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                Logger.Info("Initialisation des services");
                
                // Clé API Riot Games
                string apiKey = "RGAPI-cb458673-83bb-4e66-8d13-02dc0f66845a";
                
                // Création du service d'API Riot
                var riotApiService = new Core.Services.RiotApiService(apiKey, Logger);
                
                // TODO: Initialiser les autres services
                
                Logger.Info("Services initialisés avec succès");
            }
            catch (Exception ex)
            {
                Logger.LogException(Core.Utils.LogLevel.Error, "Erreur lors de l'initialisation des services", ex);
                MessageBox.Show($"Erreur lors de l'initialisation des services: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gère les exceptions non gérées dans le domaine d'application
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Logger.LogException(Core.Utils.LogLevel.Fatal, "Exception non gérée dans le domaine d'application", exception);
            
            MessageBox.Show($"Une erreur critique s'est produite: {exception?.Message}\n\nL'application va se fermer.", 
                "Erreur critique", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Gère les exceptions non gérées dans le dispatcher
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogException(Core.Utils.LogLevel.Error, "Exception non gérée dans le dispatcher", e.Exception);
            
            MessageBox.Show($"Une erreur s'est produite: {e.Exception.Message}", 
                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            
            e.Handled = true;
        }
    }
}
