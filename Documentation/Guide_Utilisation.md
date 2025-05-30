# Guide d'utilisation - LoL Analyzer

## Introduction

Bienvenue dans LoL Analyzer, votre outil avancé d'analyse de composition pour League of Legends. Cette application a été conçue pour vous aider à optimiser vos parties en fournissant des analyses détaillées des compositions d'équipe, des suggestions de contre-picks, et en détectant les sélections problématiques comme Teemo en jungle.

Développée en C# avec WPF, cette application offre une interface minimaliste qui se superpose au client League of Legends, vous permettant d'accéder à des informations précieuses pendant la phase de sélection des champions sans perturber votre expérience de jeu.

## Installation

### Configuration requise

- Windows 10 ou supérieur
- .NET Framework 4.7.2 ou supérieur
- Résolution d'écran minimale : 1280x720
- League of Legends client

### Étapes d'installation

1. Téléchargez le fichier d'installation `LoLAnalyzer-Setup.exe`
2. Exécutez le fichier d'installation et suivez les instructions à l'écran
3. Une fois l'installation terminée, lancez l'application depuis le menu Démarrer ou le raccourci sur le bureau
4. Lors du premier démarrage, vous serez invité à entrer votre clé API Riot Games (voir section suivante)

### Obtention d'une clé API Riot Games

Pour utiliser toutes les fonctionnalités de LoL Analyzer, vous devez disposer d'une clé API Riot Games :

1. Rendez-vous sur [https://developer.riotgames.com/](https://developer.riotgames.com/)
2. Créez un compte ou connectez-vous
3. Générez une clé API de développement (valable 24h) ou demandez une clé de production pour une utilisation à long terme
4. Copiez la clé et entrez-la dans LoL Analyzer lorsque demandé

## Fonctionnalités principales

### 1. Analyse de composition d'équipe

L'analyse de composition vous permet d'évaluer les forces et faiblesses de votre équipe et de l'équipe adverse.

**Comment utiliser cette fonctionnalité :**
- Lancez LoL Analyzer avant ou pendant une partie de League of Legends
- L'application détectera automatiquement la phase de sélection des champions
- Cliquez sur l'onglet "Analyse de composition" dans l'overlay
- Les compositions d'équipe seront analysées en temps réel, avec des informations sur :
  - L'équilibre des dégâts (physiques/magiques/vrais)
  - Les synergies entre champions
  - Les forces et faiblesses de chaque composition
  - Les chances de victoire estimées
  - Les phases de jeu favorables (early/mid/late)

### 2. Suggestion de contre-picks

Cette fonctionnalité vous propose les 3 meilleurs champions pour contrer la composition adverse.

**Comment utiliser cette fonctionnalité :**
- Pendant la phase de sélection des champions, cliquez sur l'onglet "Contre-picks"
- Sélectionnez votre rôle dans le menu déroulant
- L'application analysera les champions adverses déjà sélectionnés
- Vous verrez les 3 meilleurs contre-picks pour votre rôle, avec :
  - Le taux de victoire estimé
  - Les raisons de l'efficacité du contre-pick
  - La difficulté de jeu du champion

### 3. Détection des picks problématiques

Cette fonctionnalité met en évidence les sélections non-méta ou "troll" dans votre équipe.

**Comment utiliser cette fonctionnalité :**
- Cette fonctionnalité est active par défaut pendant la phase de sélection
- Les picks problématiques (comme Teemo jungle) seront automatiquement mis en évidence
- Un indicateur visuel apparaîtra à côté du champion concerné
- Des informations sur les raisons pour lesquelles ce pick est considéré comme problématique seront affichées
- Des alternatives plus viables seront suggérées

### 4. Recommandations de runes et sorts d'invocateur

Cette fonctionnalité vous propose les meilleures configurations de runes et sorts d'invocateur pour votre champion.

**Comment utiliser cette fonctionnalité :**
- Après avoir sélectionné votre champion, cliquez sur l'onglet "Runes & Sorts"
- L'application analysera votre champion, votre rôle et la composition adverse
- Vous verrez jusqu'à 3 configurations de runes recommandées, avec :
  - Le taux de victoire associé
  - Des explications sur les choix de runes
  - Des conseils spécifiques contre la composition adverse
- De même, vous verrez les meilleures combinaisons de sorts d'invocateur

### 5. Profil utilisateur et statistiques personnelles

Cette fonctionnalité vous permet de suivre vos performances et d'obtenir des recommandations personnalisées.

**Comment utiliser cette fonctionnalité :**
- Cliquez sur l'onglet "Profil" dans l'interface principale
- Entrez votre nom d'invocateur et votre région
- L'application récupérera vos statistiques et créera votre profil
- Vous pourrez voir :
  - Vos champions les plus joués
  - Vos meilleurs taux de victoire
  - Des recommandations de champions basées sur votre style de jeu
  - Des statistiques détaillées par champion et par rôle

## Interface utilisateur

### Mode overlay

L'interface en mode overlay est conçue pour être minimaliste et non intrusive.

**Fonctionnalités de l'overlay :**
- **Déplacement :** Cliquez et maintenez la barre supérieure pour déplacer l'overlay
- **Redimensionnement :** Utilisez les coins de l'overlay pour le redimensionner
- **Transparence :** Ajustez la transparence avec le curseur dans les paramètres
- **Mode compact :** Activez le mode compact pour réduire la taille de l'overlay
- **Masquage automatique :** L'overlay peut se masquer automatiquement en dehors de la phase de sélection

### Raccourcis clavier

- **Ctrl+Shift+L :** Afficher/masquer l'overlay
- **Ctrl+Shift+C :** Basculer en mode compact
- **Ctrl+Shift+S :** Ouvrir les paramètres
- **Ctrl+Shift+Q :** Quitter l'application

## Paramètres et personnalisation

### Thèmes

LoL Analyzer propose plusieurs thèmes visuels :
- **Clair :** Thème lumineux avec des contrastes doux
- **Sombre :** Thème sombre pour réduire la fatigue oculaire
- **League :** Thème inspiré de l'interface de League of Legends
- **Personnalisé :** Créez votre propre thème en ajustant les couleurs

Pour changer de thème :
1. Cliquez sur l'icône d'engrenage dans l'overlay
2. Sélectionnez l'onglet "Apparence"
3. Choisissez le thème souhaité dans le menu déroulant

### Options d'affichage

Vous pouvez personnaliser l'affichage de l'overlay :
- **Taille :** Ajustez la taille de l'overlay
- **Opacité :** Réglez le niveau de transparence
- **Position :** Définissez la position par défaut
- **Comportement :** Configurez le comportement de l'overlay (toujours visible, masquage automatique, etc.)

### Préférences de données

Configurez les sources de données et les préférences d'analyse :
- **Région :** Sélectionnez votre région principale
- **Tier :** Définissez votre niveau de jeu pour des analyses adaptées
- **Mise à jour des données :** Configurez la fréquence de mise à jour des données
- **Filtres :** Personnalisez les filtres pour les suggestions de champions

## Mise à jour des données

LoL Analyzer met à jour automatiquement sa base de données pour rester en phase avec les derniers changements du jeu.

**Fréquence des mises à jour :**
- **Données de base :** Mises à jour à chaque patch de League of Legends
- **Statistiques de champions :** Mises à jour quotidiennement
- **Méta et tendances :** Mises à jour hebdomadairement

Pour forcer une mise à jour manuelle :
1. Ouvrez l'application
2. Cliquez sur l'icône d'engrenage
3. Sélectionnez l'onglet "Données"
4. Cliquez sur "Mettre à jour maintenant"

## Dépannage

### Problèmes courants et solutions

**L'application ne détecte pas le client League of Legends**
- Assurez-vous que le client LoL est en cours d'exécution
- Redémarrez LoL Analyzer
- Vérifiez que vous exécutez l'application en tant qu'administrateur

**L'overlay ne s'affiche pas**
- Utilisez le raccourci Ctrl+Shift+L pour forcer l'affichage
- Vérifiez les paramètres d'affichage
- Redémarrez l'application

**Les données semblent obsolètes**
- Forcez une mise à jour manuelle des données
- Vérifiez votre connexion internet
- Assurez-vous que votre clé API est valide

**L'application est lente ou instable**
- Fermez les applications inutiles en arrière-plan
- Vérifiez les journaux d'erreurs dans le dossier Logs
- Réinstallez l'application si le problème persiste

### Journaux d'erreurs

Les journaux d'erreurs sont stockés dans le dossier :
```
C:\Users\[Votre nom d'utilisateur]\AppData\Local\LoLAnalyzer\Logs
```

En cas de problème, ces journaux peuvent être utiles pour le support technique.

### Contact et support

Si vous rencontrez des problèmes ou avez des suggestions :
- Consultez la FAQ sur notre site web
- Rejoignez notre serveur Discord pour obtenir de l'aide
- Contactez-nous par email à support@lolanalyzer.com

## Mises à jour de l'application

LoL Analyzer vérifie automatiquement les mises à jour au démarrage.

**Pour mettre à jour manuellement :**
1. Ouvrez l'application
2. Cliquez sur l'icône d'engrenage
3. Sélectionnez l'onglet "À propos"
4. Cliquez sur "Vérifier les mises à jour"

## Confidentialité et sécurité

LoL Analyzer respecte votre vie privée :
- Aucune donnée personnelle n'est collectée sans votre consentement
- Les données de jeu sont utilisées uniquement pour améliorer vos analyses
- L'application n'interfère pas avec le client LoL d'une manière qui pourrait être détectée comme un logiciel tiers non autorisé

## Remerciements

LoL Analyzer utilise les données et API suivantes :
- API Riot Games
- Data Dragon
- Communautés LoLalytics, U.GG et Mobalytics pour les statistiques avancées

## Licence

LoL Analyzer est distribué sous licence propriétaire. Tous droits réservés.

---

Merci d'avoir choisi LoL Analyzer pour améliorer votre expérience de jeu sur League of Legends !
