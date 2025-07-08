# Plateforme de rendez-vous médicaux

Une application web développée avec ASP.NET Core MVC permettant la gestion de rendez-vous entre patients et administrateurs de santé. Cette plateforme est conçue pour offrir une expérience fluide, sécurisée et efficace dans la prise et la validation de rendez-vous.

### 📊 Aperçu

- Création de comptes patients et administrateurs
- Authentification sécurisée (avec option 2FA)
- Prise de rendez-vous avec vérification de la date et l’heure
- Limitation à 3 rendez-vous actifs par utilisateur
- Tableau de bord dynamique selon le rôle (Admin / Utilisateur)
- Approbation ou rejet des rendez-vous par l’administrateur
- Gestion de profil utilisateur

### 🚀 Fonctionnalités principales

Fonction

Description

Gestion des utilisateurs

Séparation entre rôles Admin et Utilisateur

Prise de rendez-vous

Formulaire pour choisir une date et une heure future

Approbation Admin

Admin peut approuver ou rejeter un rendez-vous en attente

Limite de rendez-vous

Chaque utilisateur peut avoir au max. 3 rendez-vous actifs

Navigation dynamique

Affichage adapté selon le rôle connecté

Authentification à 2 facteurs

Activation 2FA via app d’authentification (Microsoft / Google)

### 📚 Technologies

- ASP.NET Core MVC (.NET 6+)
- Entity Framework Core + SQL Server LocalDB
- Identity (gestion d’utilisateurs/rôles)
- Bootstrap 5
- Razor Pages pour la gestion du compte

### ⚖️ Installation locale

Cloner le projet :

git clone https://github.com/votre-utilisateur/medappointments.git

Configurer la base de données :

dotnet ef database update

Lancer le projet :

dotnet run

Accès :
Naviguer sur https://localhost:port/

### 🚫 Contraintes métiers intégrées

- L’utilisateur ne peut pas créer un rendez-vous dans le passé
- L’admin ne peut pas modifier un rendez-vous déjà approuvé ou rejeté
- Si un admin modifie son propre rôle, il est automatiquement déconnecté
- Possibilité de configurer les statuts depuis l’interface
