# Guide d'utilisation - LoL Analyzer

## Introduction

LoL Analyzer est une application d'analyse de composition pour League of Legends qui vous aide à optimiser vos choix de champions pendant la phase de sélection. Grâce à son interface en overlay, l'application se superpose au client du jeu et vous fournit des informations précieuses en temps réel.

## Fonctionnalités principales

1. **Analyse de composition d'équipe**
   - Évaluation de l'équilibre des dégâts (physiques/magiques/vrais)
   - Analyse des synergies entre champions
   - Identification des forces et faiblesses de chaque composition
   - Évaluation de la performance par phase de jeu (early/mid/late)

2. **Suggestion de contre-picks**
   - Proposition de 3 champions optimaux pour contrer la composition ennemie
   - Affichage du taux de victoire estimé pour chaque suggestion
   - Explication des raisons de l'efficacité de chaque contre-pick

3. **Détection des picks problématiques**
   - Identification des sélections non-méta ou "troll" (comme Teemo jungle)
   - Mise en évidence visuelle de ces sélections dans l'interface
   - Suggestion d'alternatives plus viables

## Installation

1. Décompressez l'archive `LoLAnalyzer-CSharp-Final.zip` dans un dossier de votre choix
2. Ouvrez la solution `LoLAnalyzer.sln` dans Visual Studio
3. Compilez le projet (F5 ou Ctrl+F5)
4. Lancez l'application

## Configuration requise

- Windows 10 ou supérieur
- .NET Framework 4.7.2 ou supérieur
- Visual Studio 2019 ou supérieur (pour la compilation)
- League of Legends client

## Utilisation

### Démarrage

1. Lancez l'application LoL Analyzer
2. Entrez votre nom d'invocateur
3. Sélectionnez votre région
4. Sélectionnez votre rôle principal
5. Cliquez sur "Lancer l'overlay"

### Pendant la phase de sélection

1. L'overlay apparaît automatiquement et se superpose au client League of Legends
2. Vous pouvez déplacer l'overlay en cliquant et en faisant glisser la barre de titre
3. Vous pouvez redimensionner l'overlay en cliquant et en faisant glisser les bords
4. Naviguez entre les différents onglets pour accéder aux différentes fonctionnalités

### Analyse de composition

L'onglet "Analyse" affiche les informations suivantes pour chaque équipe :
- Liste des champions sélectionnés et leurs rôles
- Forces de la composition
- Faiblesses de la composition

### Contre-picks

L'onglet "Contre-picks" vous permet de :
1. Sélectionner votre rôle
2. Cliquer sur "Analyser" pour obtenir des suggestions de contre-picks
3. Voir les 3 meilleurs champions pour contrer la composition ennemie
4. Consulter les explications sur l'efficacité de chaque suggestion

### Détection des picks problématiques

L'onglet "Détection" met en évidence :
- Les picks non-méta ou "troll"
- Les raisons pour lesquelles ces picks sont problématiques
- Des alternatives plus viables

## Personnalisation

Vous pouvez personnaliser l'application en modifiant les paramètres suivants :
- Opacité de l'overlay
- Mode compact (taille réduite)
- Thème de l'interface

## Dépannage

### L'overlay ne s'affiche pas

1. Vérifiez que l'application est lancée en mode administrateur
2. Assurez-vous que le client League of Legends est ouvert
3. Redémarrez l'application

### L'API ne répond pas

1. Vérifiez votre connexion internet
2. Assurez-vous que la clé API est valide
3. Respectez les limites de requêtes (20/sec, 100/2min)

## Support

Pour toute question ou problème, veuillez contacter le support à l'adresse suivante : support@lolanalyzer.com

---

© 2025 LoL Analyzer. Tous droits réservés.
LoL Analyzer n'est pas approuvé par Riot Games et ne reflète pas les opinions ou opinions de Riot Games ou de toute personne impliquée officiellement dans la production ou la gestion de League of Legends. League of Legends et Riot Games sont des marques commerciales ou des marques déposées de Riot Games, Inc.
