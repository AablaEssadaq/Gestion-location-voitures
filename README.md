# 🚗 Application de Gestion de Location de Voitures (.NET)

Application complète de gestion d’une agence de location de voitures, développée en **.NET 8.0**, comprenant :
- un **Back-Office Desktop** pour l’administration interne
- un **Front-Office Web** permettant aux clients de réserver en ligne

Projet réalisé dans un cadre académique.

---

## 📌 Présentation du projet

**Objectif**  
Fournir une solution centralisée permettant :
- la gestion des véhicules, clients, employés et locations
- la réservation en ligne de véhicules par les clients
- le suivi financier et la maintenance de la flotte

**Architecture**  
- Architecture **N-Tier**
- Base de données **MySQL partagée**
- Séparation claire entre les couches métier, données et présentation

---

## 🖥️ Back-Office (Desktop – WPF)

Application destinée à l’administration de l’agence.

### Fonctionnalités principales

#### 🔐 Authentification & rôles
- Authentification sécurisée (hachage SHA256)
- Gestion des rôles : **Admin / Employé**

#### 📊 Tableau de bord
- Indicateurs clés (KPI) :
  - Locations en attente
  - Véhicules disponibles
  - Nombre de clients
- Accès rapide aux modules

#### 👥 Gestion des utilisateurs
- CRUD complet des employés
- Attribution des rôles
- Recherche, filtrage et tri
- Import / Export Excel

#### 🚘 Gestion des véhicules
- CRUD complet
- Suivi du kilométrage et de l’état
- Gestion des photos
- Alertes visuelles de maintenance
- Import / Export Excel

#### 👤 Gestion des clients
- Clients inscrits via le Web ou ajoutés manuellement
- Recherche, filtrage et tri
- Import / Export Excel

#### 📄 Gestion des locations
- Validation / refus des réservations web
- Génération automatique de contrats PDF avec **QR Code**
- Envoi automatique des documents par email
- Archivage en base de données
- Import / Export Excel

#### 💰 Gestion financière
- Encaissement des paiements
- Historique des transactions
- Filtres par date et mode de paiement
- Import / Export Excel

#### 🔧 Gestion de la maintenance
- Système d’alertes intelligent basé sur le kilométrage
- Historique des interventions
- Configuration dynamique des types d’entretien
- Import / Export Excel

---

## 🌐 Front-Office (Web – ASP.NET Core MVC)

Application web destinée aux clients.

### Fonctionnalités

- Catalogue des véhicules disponibles
- Recherche avancée (marque, modèle, catégorie)
- Inscription et connexion sécurisées
- Gestion du profil client
- Réservation en ligne avec :
  - Vérification de disponibilité en temps réel
  - Calcul automatique du prix
  - Suivi du statut de la réservation
- Génération et envoi automatique du bon de réservation PDF

---

## 🛠️ Stack technique

- **Langage** : C# (.NET 8.0)
- **Desktop** : WPF (XAML)
- **Web** : ASP.NET Core MVC
- **Base de données** : MySQL
- **Accès aux données** : ADO.NET

### Bibliothèques utilisées
- QuestPDF – Génération de PDF
- QRCoder – QR Codes
- ClosedXML – Export / Import Excel
- System.Net.Mail – Envoi d’emails SMTP

### Outils
- Visual Studio 2022
- Git & GitHub
- WAMP / XAMPP

---

## 👩‍💻 Équipe

Projet réalisé en groupe par :
- Ajabboune Rihab  
- Bellafrikh Zaynab  
- Essadaq Aabla  

---


## 📄 Licence

Projet académique – usage pédagogique.
