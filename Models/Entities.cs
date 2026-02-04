using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace FormationManager.Models
{
    // Entité de base pour toutes les autres entités
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime DateModification { get; set; } = DateTime.Now;
        public string CreePar { get; set; } = string.Empty;
        public string ModifiePar { get; set; } = string.Empty;
        [StringLength(50)]
        public string SiteId { get; set; } = string.Empty;
    }

    // Formation (fiche de formation sur étagère)
    public class Formation : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Titre { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal DureeHeures { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal PrixIndicatif { get; set; }
        
        public bool EstPublique { get; set; } = true;
        
        // Programme détaillé
        public string Programme { get; set; } = string.Empty;
        
        // Prérequis
        public string Prerequis { get; set; } = string.Empty;
        
        // Modalités pédagogiques
        public string ModalitesPedagogiques { get; set; } = string.Empty;
        
        // Modalités d'évaluation
        public string ModalitesEvaluation { get; set; } = string.Empty;
        
        // Références Qualiopi
        public string ReferencesQualiopi { get; set; } = string.Empty;
        
        // Navigation
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    }

    // Session de formation programmée
    public class Session : BaseEntity
    {
        public int FormationId { get; set; }
        public virtual Formation Formation { get; set; } = null!;

        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        
        [StringLength(200)]
        public string Lieu { get; set; } = string.Empty;

        public int? SalleId { get; set; }
        public virtual Salle? Salle { get; set; }
        
        public bool EstPublique { get; set; } = true;
        
        // Statut : Programmée, En cours, Terminée, Annulée
        [StringLength(50)]
        public string Statut { get; set; } = "Programmée";
        
        // Nombre maximum de stagiaires
        public int NombreMaxStagiaires { get; set; } = 20;
        
        // Formateur responsable
        public int FormateurId { get; set; }
        public virtual Formateur Formateur { get; set; } = null!;
        
        // Navigation
        public virtual ICollection<SessionClient> SessionClients { get; set; } = new List<SessionClient>();
        public virtual ICollection<Stagiaire> Stagiaires { get; set; } = new List<Stagiaire>();
    }

    // Types de clients
    public enum TypeClient
    {
        Particulier,
        Entreprise,
        OrganismeFormation
    }

    // Client (entreprise, particulier, ou autre OF)
    public class Client : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Nom { get; set; } = string.Empty;

        public TypeClient TypeClient { get; set; }

        [Required]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string Telephone { get; set; } = string.Empty;

        public string Adresse { get; set; } = string.Empty;
        public string CodePostal { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;

        // Informations professionnelles
        public string SIRET { get; set; } = string.Empty;
        public string NumeroOPCA { get; set; } = string.Empty;

        // Navigation
        public virtual ICollection<SessionClient> SessionClients { get; set; } = new List<SessionClient>();
        public virtual ICollection<Stagiaire> Stagiaires { get; set; } = new List<Stagiaire>();
    }

    public class Devis
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public virtual Client Client { get; set; } = null!;
        public int? SessionId { get; set; }
        public virtual Session? Session { get; set; }
        [Required]
        [StringLength(50)]
        public string Numero { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; } = DateTime.Today;
        public DateTime? DateValidite { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal MontantHT { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal TauxTVA { get; set; }
        [StringLength(50)]
        public string Statut { get; set; } = "Brouillon";
        public string Designation { get; set; } = string.Empty;
        [StringLength(50)]
        public string SiteId { get; set; } = string.Empty;
    }

    public class Facture
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public virtual Client Client { get; set; } = null!;
        public int? SessionId { get; set; }
        public virtual Session? Session { get; set; }
        public int? DevisId { get; set; }
        public virtual Devis? Devis { get; set; }
        [Required]
        [StringLength(50)]
        public string Numero { get; set; } = string.Empty;
        public DateTime DateEmission { get; set; } = DateTime.Today;
        [Column(TypeName = "decimal(10,2)")]
        public decimal MontantHT { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal TauxTVA { get; set; }
        [StringLength(50)]
        public string Statut { get; set; } = "Brouillon";
        public string Designation { get; set; } = string.Empty;
        [StringLength(50)]
        public string SiteId { get; set; } = string.Empty;
    }

    // Association entre Session et Client (pour la sous-traitance)
    public class SessionClient : BaseEntity
    {
        public int SessionId { get; set; }
        public virtual Session Session { get; set; } = null!;

        public int ClientId { get; set; }
        public virtual Client Client { get; set; } = null!;

        // Tarif spécifique pour ce client
        [Column(TypeName = "decimal(10,2)")]
        public decimal TarifNegocie { get; set; }

        // Nombre de places réservées
        public int NombrePlaces { get; set; }

        // Type de financement
        public string TypeFinancement { get; set; } = string.Empty;
    }

    // Stagiaire
    public class Stagiaire : BaseEntity
    {
        public int? ClientId { get; set; }
        public virtual Client? Client { get; set; }

        [Required]
        [StringLength(200)]
        public string Nom { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Prenom { get; set; } = string.Empty;

        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string Telephone { get; set; } = string.Empty;

        public DateTime? DateNaissance { get; set; }

        // Informations professionnelles
        public string Poste { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;

        // Session associée
        public int? SessionId { get; set; }
        public virtual Session? Session { get; set; }

        // Statut de l'inscription
        [StringLength(50)]
        public string StatutInscription { get; set; } = "Inscrit";

        // Suivi de présence
        public decimal HeuresPresence { get; set; } = 0;
        public bool EstPresent { get; set; } = true;

        // Évaluations
        public decimal? EvaluationAChaud { get; set; } // Sur 20
        public decimal? EvaluationAFroid { get; set; } // Sur 20
        public string CommentairesEvaluation { get; set; } = string.Empty;

        // Attestation
        public bool AttestationGeneree { get; set; } = false;
        public DateTime? DateAttestation { get; set; }
    }

    // Types de documents
    public enum TypeDocument
    {
        Convention,
        Contrat,
        Attestation,
        Emargement,
        Evaluation,
        Facture,
        Devis,
        PreuveQualiopi
    }

    // Document administratif
    public class Document : BaseEntity
    {
        public TypeDocument TypeDocument { get; set; }

        // Template utilisé
        public int TemplateId { get; set; }
        public virtual TemplateDocument Template { get; set; } = null!;

        // Données du document (JSON)
        public string Donnees { get; set; } = string.Empty;

        // Statut de validation
        [StringLength(50)]
        public string StatutValidation { get; set; } = "En attente";

        // Validateur
        public int? ValideurId { get; set; }
        public virtual Utilisateur? Valideur { get; set; }

        public DateTime? DateValidation { get; set; }

        // Fichier généré
        public string CheminFichier { get; set; } = string.Empty;
        public string NomFichier { get; set; } = string.Empty;

        // Associations
        public int? SessionId { get; set; }
        public virtual Session? Session { get; set; }

        public int? ClientId { get; set; }
        public virtual Client? Client { get; set; }

        public int? StagiaireId { get; set; }
        public virtual Stagiaire? Stagiaire { get; set; }
    }

    // Template de document
    public class TemplateDocument : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Nom { get; set; } = string.Empty;

        public TypeDocument TypeDocument { get; set; }

        // Contenu du template (HTML avec placeholders)
        public string Contenu { get; set; } = string.Empty;

        // Styles CSS
        public string Styles { get; set; } = string.Empty;

        // Variables disponibles
        public string Variables { get; set; } = string.Empty; // JSON

        public bool Actif { get; set; } = true;
    }

    // Utilisateurs et rôles
    public enum RoleUtilisateur
    {
        Formateur,
        ResponsableFormation,
        Administrateur,
        ResponsableSite
    }

    public class Utilisateur : IdentityUser<int>
    {
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime DateModification { get; set; } = DateTime.Now;
        public string CreePar { get; set; } = string.Empty;
        public string ModifiePar { get; set; } = string.Empty;
        [StringLength(50)]
        public string SiteId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Nom { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Prenom { get; set; } = string.Empty;

        public RoleUtilisateur Role { get; set; }

        // Profil du formateur
        public string Biographie { get; set; } = string.Empty;
        public string Competences { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string Formations { get; set; } = string.Empty;

        public string Telephone { get; set; } = string.Empty;

        // Actif ou non
        public bool Actif { get; set; } = true;

        // Dernière connexion
        public DateTime? DerniereConnexion { get; set; }
    }

    // Formateur (pool de formateurs)
    public class Formateur : BaseEntity
    {
        // Lien vers l'utilisateur (optionnel - un formateur peut exister sans compte utilisateur)
        public int? UtilisateurId { get; set; }
        public virtual Utilisateur? Utilisateur { get; set; }

        [Required]
        [StringLength(200)]
        public string Nom { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Prenom { get; set; } = string.Empty;

        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string Telephone { get; set; } = string.Empty;

        // Statut professionnel (Salarié, Intermittent, Auto-entrepreneur, etc.)
        [StringLength(100)]
        public string StatutProfessionnel { get; set; } = string.Empty;

        // Numéro de formateur (si existant)
        [StringLength(50)]
        public string NumeroFormateur { get; set; } = string.Empty;

        // Antenne de rattachement
        [StringLength(100)]
        public string AntenneRattachement { get; set; } = string.Empty;

        // Biographie et compétences
        public string Biographie { get; set; } = string.Empty;
        public string Competences { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string Formations { get; set; } = string.Empty;

        // Actif ou non
        public bool Actif { get; set; } = true;

        // Navigation
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
        public virtual ICollection<ActionVeille> ActionsVeille { get; set; } = new List<ActionVeille>();
    }

    // Salle de formation
    public class Salle : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Nom { get; set; } = string.Empty;

        public int? Capacite { get; set; }

        [StringLength(500)]
        public string Adresse { get; set; } = string.Empty;

        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    }

    // Actions de veille (Qualiopi)
    public class ActionVeille : BaseEntity
    {
        public int FormateurId { get; set; }
        public virtual Formateur Formateur { get; set; } = null!;

        // Types de veille
        public enum TypeVeille
        {
            MetierCompetences,
            JuridiqueAdministratif,
            Pedagogique
        }

        public TypeVeille Type { get; set; }

        [Required]
        [StringLength(200)]
        public string Titre { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime DateAction { get; set; } = DateTime.Now;

        // Durée en heures
        public decimal Duree { get; set; }

        // Preuves (liens vers documents, etc.)
        public string Preuves { get; set; } = string.Empty;
    }

    // Indicateurs Qualiopi
    public class IndicateurQualiopi : BaseEntity
    {
        [Required]
        public string CodeIndicateur { get; set; } = string.Empty; // Ex: "1.1", "2.3.a"

        [Required]
        [StringLength(500)]
        public string Libelle { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // Critère associé (1 à 7)
        public int Critere { get; set; }

        // Niveau de preuve requis
        public enum NiveauPreuve
        {
            Faible,
            Moyen,
            Eleve
        }

        public NiveauPreuve NiveauPreuveRequis { get; set; }
    }

    // Preuves Qualiopi
    public class PreuveQualiopi : BaseEntity
    {
        public int IndicateurQualiopiId { get; set; }
        public virtual IndicateurQualiopi Indicateur { get; set; } = null!;

        public int SessionId { get; set; }
        public virtual Session Session { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Titre { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // Type de preuve
        public enum TypePreuve
        {
            Document,
            Temoignage,
            Photo,
            Video,
            Autre
        }

        public TypePreuve Type { get; set; }

        // Fichier ou lien
        public string CheminFichier { get; set; } = string.Empty;
        public string URL { get; set; } = string.Empty;

        // Validité
        public bool EstValide { get; set; } = false;
        public DateTime? DateValidation { get; set; }
        public string CommentaireValidation { get; set; } = string.Empty;
    }

    // Veille RSS – critère 6 (indicateurs 23–29)
    public class RssFeed
    {
        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [StringLength(500)]
        public string Url { get; set; } = string.Empty;
        public int? DefaultIndicateurId { get; set; }
        public virtual IndicateurQualiopi? DefaultIndicateur { get; set; }
        [StringLength(50)]
        public string SiteId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime DateModification { get; set; } = DateTime.UtcNow;

        public virtual ICollection<RssItem> Items { get; set; } = new List<RssItem>();
    }

    public class RssItem
    {
        public int Id { get; set; }
        public int RssFeedId { get; set; }
        public virtual RssFeed Feed { get; set; } = null!;
        [Required]
        [StringLength(500)]
        public string ExternalId { get; set; } = string.Empty;
        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;
        [StringLength(1000)]
        public string Link { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? PublishedUtc { get; set; }
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<VeilleValidation> Validations { get; set; } = new List<VeilleValidation>();
    }

    public class VeilleValidation
    {
        public int Id { get; set; }
        public int RssItemId { get; set; }
        public virtual RssItem RssItem { get; set; } = null!;
        public int IndicateurQualiopiId { get; set; }
        public virtual IndicateurQualiopi Indicateur { get; set; } = null!;
        [StringLength(200)]
        public string ValidatedBy { get; set; } = string.Empty;
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        [Required]
        [StringLength(50)]
        public string SiteId { get; set; } = string.Empty;
    }

    public class Site
    {
        [Key]
        [StringLength(50)]
        public string SiteId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
