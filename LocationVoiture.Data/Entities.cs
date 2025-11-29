using System;
using System.Collections.Generic;

namespace LocationVoiture.Data
{
    // --- ENUMERATIONS ---
    public enum RoleUtilisateur { Admin, Employe }
    public enum StatutLocation { EnCours, Terminee, Annulee }
    public enum MethodePaiement { CarteBancaire, Espece, Virement }

    // --- CLASSES ---

    public class Utilisateur
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string MotDePasse { get; set; }
        // Note: Dans la BDD MySQL, le role est stocké en VARCHAR ('Admin'), 
        // il faudra gérer la conversion string <-> enum dans le code si besoin.
        public string Role { get; set; }
    }

    public class Client
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }
        public string NumPermis { get; set; }
        public string MotDePasse { get; set; }
    }

    public class Categorie
    {
        public int Id { get; set; }
        public string Libelle { get; set; }
    }

    public class Voiture
    {
        public int Id { get; set; }
        public string Matricule { get; set; }
        public string Marque { get; set; }
        public string Modele { get; set; }
        public string Carburant { get; set; }
        public decimal PrixParJour { get; set; }
        public string ImageChemin { get; set; }
        public bool EstDisponible { get; set; }
        public int CategorieId { get; set; }

        // Entretien
        public int KilometrageActuel { get; set; }
        public int KmDernierEntretien { get; set; }
        public DateTime? DateProchainEntretien { get; set; }
    }

    public class Location
    {
        public int Id { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public decimal PrixTotal { get; set; }
        public string Statut { get; set; } // Changé en string pour correspondre facilement à la BDD
        public int ClientId { get; set; }
        public int VoitureId { get; set; }
    }

    public class Paiement
    {
        public int Id { get; set; }
        public decimal Montant { get; set; }
        public DateTime DatePaiement { get; set; }
        public string Methode { get; set; } // Changé en string
        public int LocationId { get; set; }
    }

    public class Entretien
    {
        public int Id { get; set; }
        public DateTime DateEntretien { get; set; }
        public string TypeEntretien { get; set; }
        public int Kilometrage { get; set; }
        public decimal Cout { get; set; }
        public string Description { get; set; }
        public int VoitureId { get; set; }
    }
}