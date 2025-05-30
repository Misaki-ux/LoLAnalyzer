using System;

namespace LoLAnalyzer.Core.Utils
{
    /// <summary>
    /// Niveaux de log
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    /// <summary>
    /// Système de journalisation pour l'application
    /// </summary>
    public class Logger
    {
        private readonly string _source;

        /// <summary>
        /// Constructeur avec source
        /// </summary>
        public Logger(string source)
        {
            _source = source;
        }

        /// <summary>
        /// Écrit un message de log
        /// </summary>
        public void Log(LogLevel level, string message)
        {
            string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{_source}] {message}";
            
            // Affichage dans la console
            Console.WriteLine(formattedMessage);
            
            // Écriture dans un fichier de log
            try
            {
                string logDirectory = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LoLAnalyzer",
                    "Logs"
                );
                
                // Créer le répertoire s'il n'existe pas
                if (!System.IO.Directory.Exists(logDirectory))
                {
                    System.IO.Directory.CreateDirectory(logDirectory);
                }
                
                string logFile = System.IO.Path.Combine(
                    logDirectory,
                    $"log_{DateTime.Now:yyyy-MM-dd}.txt"
                );
                
                // Écrire dans le fichier
                using (var writer = new System.IO.StreamWriter(logFile, true))
                {
                    writer.WriteLine(formattedMessage);
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur d'écriture dans le fichier, afficher dans la console
                Console.WriteLine($"[ERROR] Impossible d'écrire dans le fichier de log: {ex.Message}");
            }
        }

        /// <summary>
        /// Écrit un message de log de niveau Debug
        /// </summary>
        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// Écrit un message de log de niveau Info
        /// </summary>
        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// Écrit un message de log de niveau Warning
        /// </summary>
        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// Écrit un message de log de niveau Error
        /// </summary>
        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        /// <summary>
        /// Écrit un message de log de niveau Fatal
        /// </summary>
        public void Fatal(string message)
        {
            Log(LogLevel.Fatal, message);
        }

        /// <summary>
        /// Écrit un message de log avec une exception
        /// </summary>
        public void LogException(LogLevel level, string message, Exception exception)
        {
            string exceptionDetails = $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";
            Log(level, $"{message}\n{exceptionDetails}");
        }
    }
}
