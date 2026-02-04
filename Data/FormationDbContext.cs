using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using FormationManager.Models;

namespace FormationManager.Data
{
    public class FormationDbContext : IdentityDbContext<Utilisateur, IdentityRole<int>, int>
    {
        public FormationDbContext(DbContextOptions<FormationDbContext> options)
            : base(options)
        {
        }

        // DbSets pour toutes les entités
        public DbSet<Formation> Formations { get; set; }
        public DbSet<FormationVersion> FormationVersions { get; set; }
        public DbSet<DocumentAudit> DocumentsAudit { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<SessionClient> SessionClients { get; set; }
        public DbSet<Stagiaire> Stagiaires { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<TemplateDocument> TemplatesDocument { get; set; }
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Formateur> Formateurs { get; set; }
        public DbSet<Salle> Salles { get; set; }
        public DbSet<ActionVeille> ActionsVeille { get; set; }
        public DbSet<IndicateurQualiopi> IndicateursQualiopi { get; set; }
        public DbSet<PreuveQualiopi> PreuvesQualiopi { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<RssFeed> RssFeeds { get; set; }
        public DbSet<RssItem> RssItems { get; set; }
        public DbSet<VeilleValidation> VeilleValidations { get; set; }
        public DbSet<Devis> Devis { get; set; }
        public DbSet<Facture> Factures { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des entités

            // Formation
            modelBuilder.Entity<Formation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Titre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PrixIndicatif).HasColumnType("decimal(10,2)");
                entity.Property(e => e.DureeHeures).HasColumnType("decimal(5,2)");
                entity.HasIndex(e => e.Titre);
            });

            // FormationVersion
            modelBuilder.Entity<FormationVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NumeroVersion).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RaisonModification).HasMaxLength(200);
                entity.Property(e => e.ModifiePar).HasMaxLength(200);
                entity.HasOne(e => e.Formation)
                      .WithMany()
                      .HasForeignKey(e => e.FormationId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.FormationId, e.NumeroVersion });
                entity.HasIndex(e => e.DateVersion);
            });

            // DocumentAudit
            modelBuilder.Entity<DocumentAudit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(200);
                entity.Property(e => e.TypeDocument).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CheminFichier).HasMaxLength(500);
                entity.Property(e => e.CritereQualiopi).HasMaxLength(50);
                entity.Property(e => e.Statut).HasMaxLength(50);
                entity.Property(e => e.Commentaire).HasMaxLength(200);
                entity.HasOne(e => e.Formation)
                      .WithMany()
                      .HasForeignKey(e => e.FormationId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.FormationId);
                entity.HasIndex(e => e.TypeDocument);
            });

            // Salle
            modelBuilder.Entity<Salle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Adresse).HasMaxLength(500);
                entity.HasIndex(e => e.Nom);
            });

            // Formateur
            modelBuilder.Entity<Formateur>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Prenom).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Telephone).HasMaxLength(50);
                entity.Property(e => e.StatutProfessionnel).HasMaxLength(100);
                entity.Property(e => e.NumeroFormateur).HasMaxLength(50);
                entity.Property(e => e.AntenneRattachement).HasMaxLength(100);
                entity.HasOne(e => e.Utilisateur)
                      .WithMany()
                      .HasForeignKey(e => e.UtilisateurId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.NumeroFormateur);
                entity.HasIndex(e => e.Actif);
            });

            // Session
            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Formation)
                      .WithMany(e => e.Sessions)
                      .HasForeignKey(e => e.FormationId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Formateur)
                      .WithMany(e => e.Sessions)
                      .HasForeignKey(e => e.FormateurId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Salle)
                      .WithMany(e => e.Sessions)
                      .HasForeignKey(e => e.SalleId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.Statut).HasDefaultValue("Programmée");
                entity.HasIndex(e => e.DateDebut);
                entity.HasIndex(e => e.Statut);
                entity.HasIndex(e => e.SalleId);
            });

            // Client
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.HasIndex(e => e.Nom);
                entity.HasIndex(e => e.SIRET).IsUnique();
            });

            // SessionClient (table de jointure)
            modelBuilder.Entity<SessionClient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Session)
                      .WithMany(e => e.SessionClients)
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Client)
                      .WithMany(e => e.SessionClients)
                      .HasForeignKey(e => e.ClientId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.SessionId, e.ClientId }).IsUnique();
            });

            // Stagiaire
            modelBuilder.Entity<Stagiaire>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Prenom).IsRequired().HasMaxLength(200);
                entity.HasOne(e => e.Client)
                      .WithMany(e => e.Stagiaires)
                      .HasForeignKey(e => e.ClientId)
                      .OnDelete(DeleteBehavior.SetNull);  // Changé de Cascade à SetNull pour permettre la suppression
                entity.HasOne(e => e.Session)
                      .WithMany(e => e.Stagiaires)
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => new { e.ClientId, e.Nom, e.Prenom });
            });

            // Document
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Template)
                      .WithMany()
                      .HasForeignKey(e => e.TemplateId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Valideur)
                      .WithMany()
                      .HasForeignKey(e => e.ValideurId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Session)
                      .WithMany()
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Client)
                      .WithMany()
                      .HasForeignKey(e => e.ClientId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Stagiaire)
                      .WithMany()
                      .HasForeignKey(e => e.StagiaireId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.TypeDocument);
                entity.HasIndex(e => e.StatutValidation);
            });

            // TemplateDocument
            modelBuilder.Entity<TemplateDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.TypeDocument);
            });

            // Utilisateur
            modelBuilder.Entity<Utilisateur>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Prenom).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SiteId).HasMaxLength(50);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Role);
            });

            modelBuilder.Entity<Site>(entity =>
            {
                entity.HasKey(e => e.SiteId);
                entity.Property(e => e.SiteId).HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.Name);
            });

            // ActionVeille
            modelBuilder.Entity<ActionVeille>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Formateur)
                      .WithMany(e => e.ActionsVeille)
                      .HasForeignKey(e => e.FormateurId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.FormateurId);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.DateAction);
            });

            // IndicateurQualiopi
            modelBuilder.Entity<IndicateurQualiopi>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CodeIndicateur).IsRequired();
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => new { e.SiteId, e.CodeIndicateur }).IsUnique();
                entity.HasIndex(e => e.Critere);
            });

            // PreuveQualiopi
            modelBuilder.Entity<PreuveQualiopi>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Indicateur)
                      .WithMany()
                      .HasForeignKey(e => e.IndicateurQualiopiId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Session)
                      .WithMany()
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.IndicateurQualiopiId, e.SessionId });
                entity.HasIndex(e => e.EstValide);
            });

            // RssFeed
            modelBuilder.Entity<RssFeed>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
                entity.HasOne(e => e.DefaultIndicateur)
                      .WithMany()
                      .HasForeignKey(e => e.DefaultIndicateurId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.Url);
                entity.HasIndex(e => e.IsActive);
            });

            // RssItem
            modelBuilder.Entity<RssItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Feed)
                      .WithMany(e => e.Items)
                      .HasForeignKey(e => e.RssFeedId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => new { e.RssFeedId, e.ExternalId }).IsUnique();
                entity.HasIndex(e => e.PublishedUtc);
                entity.HasIndex(e => e.FetchedAt);
            });

            modelBuilder.Entity<Devis>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Session).WithMany().HasForeignKey(e => e.SessionId).OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.Numero).HasMaxLength(50);
                entity.Property(e => e.Statut).HasMaxLength(50);
                entity.HasIndex(e => e.Numero);
                entity.HasIndex(e => e.SiteId);
            });

            modelBuilder.Entity<Facture>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Session).WithMany().HasForeignKey(e => e.SessionId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Devis).WithMany().HasForeignKey(e => e.DevisId).OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.Numero).HasMaxLength(50);
                entity.Property(e => e.Statut).HasMaxLength(50);
                entity.HasIndex(e => e.Numero);
                entity.HasIndex(e => e.SiteId);
            });

            // VeilleValidation
            modelBuilder.Entity<VeilleValidation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.RssItem)
                      .WithMany(e => e.Validations)
                      .HasForeignKey(e => e.RssItemId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Indicateur)
                      .WithMany()
                      .HasForeignKey(e => e.IndicateurQualiopiId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.SiteId);
                entity.HasIndex(e => e.ValidatedAt);
            });

            // Données initiales
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Création des indicateurs Qualiopi
            var seedTimestamp = new DateTime(2024, 1, 1);
            var indicateurs = new[]
            {
                new IndicateurQualiopi { Id = 1, CodeIndicateur = "1", Libelle = "Information du public", Critere = 1, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 2, CodeIndicateur = "2", Libelle = "Indicateurs de résultats", Critere = 1, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 3, CodeIndicateur = "3", Libelle = "Taux d'obtention des certifications", Critere = 1, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 4, CodeIndicateur = "4", Libelle = "Analyse du besoin", Critere = 2, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 5, CodeIndicateur = "5", Libelle = "Objectifs de la prestation", Critere = 2, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 6, CodeIndicateur = "6", Libelle = "Contenus et modalités", Critere = 2, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 7, CodeIndicateur = "7", Libelle = "Contenus et exigences", Critere = 2, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 8, CodeIndicateur = "8", Libelle = "Positionnement à l'entrée", Critere = 2, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 9, CodeIndicateur = "9", Libelle = "Conditions de déroulement", Critere = 3, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 10, CodeIndicateur = "10", Libelle = "Adaptation de la prestation", Critere = 3, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 11, CodeIndicateur = "11", Libelle = "Atteinte des objectifs", Critere = 3, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 12, CodeIndicateur = "12", Libelle = "Engagement des bénéficiaires", Critere = 3, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 13, CodeIndicateur = "13", Libelle = "Coordination des apprentis", Critere = 3, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 14, CodeIndicateur = "14", Libelle = "Exercice de la citoyenneté", Critere = 3, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 15, CodeIndicateur = "15", Libelle = "Droits et devoirs de l'apprenti", Critere = 3, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 16, CodeIndicateur = "16", Libelle = "Présentation à la certification", Critere = 3, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 17, CodeIndicateur = "17", Libelle = "Moyens humains et techniques", Critere = 4, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 18, CodeIndicateur = "18", Libelle = "Coordination des acteurs", Critere = 4, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 19, CodeIndicateur = "19", Libelle = "Ressources pédagogiques", Critere = 4, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 20, CodeIndicateur = "20", Libelle = "Personnels dédiés", Critere = 4, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 21, CodeIndicateur = "21", Libelle = "Compétences des acteurs", Critere = 5, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 22, CodeIndicateur = "22", Libelle = "Gestion de la compétence", Critere = 5, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 23, CodeIndicateur = "23", Libelle = "Veille légale et réglementaire", Critere = 6, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 24, CodeIndicateur = "24", Libelle = "Veille des emplois et métiers", Critere = 6, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 25, CodeIndicateur = "25", Libelle = "Veille pédagogique et technologique", Critere = 6, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 26, CodeIndicateur = "26", Libelle = "Situation de handicap", Critere = 6, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 27, CodeIndicateur = "27", Libelle = "Disposition sous-traitance", Critere = 6, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 28, CodeIndicateur = "28", Libelle = "Formation en situation de travail", Critere = 6, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 29, CodeIndicateur = "29", Libelle = "Insertion professionnelle", Critere = 6, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 30, CodeIndicateur = "30", Libelle = "Recueil des appréciations", Critere = 7, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 31, CodeIndicateur = "31", Libelle = "Traitement des réclamations", Critere = 7, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new IndicateurQualiopi { Id = 32, CodeIndicateur = "32", Libelle = "Amélioration continue", Critere = 7, NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" }
            };

            modelBuilder.Entity<IndicateurQualiopi>().HasData(indicateurs);

            // Templates de documents par défaut
            var templates = new[]
            {
                new TemplateDocument { Id = 1, Nom = "Convention de formation", TypeDocument = TypeDocument.Convention, Contenu = "<html><body><h1>Convention de formation</h1>{{formation.titre}}</body></html>", Variables = "{\"formation.titre\":\"Titre de la formation\"}", Styles = string.Empty, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new TemplateDocument { Id = 2, Nom = "Attestation de fin de formation", TypeDocument = TypeDocument.Attestation, Contenu = "<html><body><h1>Attestation</h1>{{stagiaire.nom}} {{stagiaire.prenom}}</body></html>", Variables = "{\"stagiaire.nom\":\"Nom du stagiaire\",\"stagiaire.prenom\":\"Prénom du stagiaire\"}", Styles = string.Empty, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" },
                new TemplateDocument { Id = 3, Nom = "Émargement", TypeDocument = TypeDocument.Emargement, Contenu = "<html><body><h1>Feuille d'émargement</h1>{{session.titre}}</body></html>", Variables = "{\"session.titre\":\"Titre de la session\"}", Styles = string.Empty, DateCreation = seedTimestamp, DateModification = seedTimestamp, CreePar = "system", ModifiePar = "system", SiteId = "SITE_01" }
            };

            modelBuilder.Entity<TemplateDocument>().HasData(templates);
        }
    }
}
