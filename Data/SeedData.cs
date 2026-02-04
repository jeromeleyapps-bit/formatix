using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FormationManager.Models;

namespace FormationManager.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, UserManager<Utilisateur> userManager, RoleManager<IdentityRole<int>> roleManager)
        {
            var context = serviceProvider.GetRequiredService<FormationDbContext>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            
            // S'assurer que la base de données est créée
            context.Database.EnsureCreated();

            // Créer les rôles
            await CreateRoles(roleManager);

            // Créer l'utilisateur administrateur par défaut
            await CreateDefaultUser(userManager, configuration);

            // Créer les sites par défaut
            await CreateDefaultSites(context, configuration);

            // S'assurer que la liste complète des indicateurs Qualiopi est présente
            await EnsureQualiopiIndicators(context);

            // Créer des données de démonstration (si activé dans la config)
            var createDemoData = configuration.GetValue<bool>("AppSettings:CreateDemoData", false);
            if (createDemoData)
            {
                await CreateDemoData(context, userManager);
            }
        }

        private static async Task CreateRoles(RoleManager<IdentityRole<int>> roleManager)
        {
            string[] roleNames = { "Administrateur", "ResponsableFormation", "ResponsableSite", "Formateur" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }
        }

        private static async Task CreateDefaultUser(UserManager<Utilisateur> userManager, IConfiguration configuration)
        {
            var siteId = configuration["Sync:SiteId"] ?? "SITE_01";
            var defaultUser = new Utilisateur
            {
                UserName = "admin@formationmanager.com",
                Email = "admin@formationmanager.com",
                Nom = "Administrateur",
                Prenom = "Système",
                Role = RoleUtilisateur.Administrateur,
                SiteId = siteId,
                Actif = true,
                EmailConfirmed = true
            };

            var user = await userManager.FindByEmailAsync(defaultUser.Email);

            if (user == null)
            {
                await userManager.CreateAsync(defaultUser, "Admin123!");
                await userManager.AddToRoleAsync(defaultUser, "Administrateur");
            }
        }

        private static async Task CreateDefaultSites(FormationDbContext context, IConfiguration configuration)
        {
            if (context.Sites.Any())
            {
                return;
            }

            var sites = configuration.GetSection("Sites").Get<List<Site>>();
            if (sites == null || sites.Count == 0)
            {
                var siteId = configuration["Sync:SiteId"] ?? "SITE_01";
                sites = new List<Site>
                {
                    new Site { SiteId = siteId, Name = siteId, IsActive = true }
                };
            }

            context.Sites.AddRange(sites.Select(s => new Site
            {
                SiteId = s.SiteId,
                Name = s.Name,
                IsActive = s.IsActive
            }));
            await context.SaveChangesAsync();
        }

        private static async Task CreateDemoData(FormationDbContext context, UserManager<Utilisateur> userManager)
        {
            // Vérifier si des données existent déjà
            if (context.Formations.Any())
            {
                return; // La base de données a déjà été initialisée
            }

            // Créer des utilisateurs de démonstration
            var siteId = context.Sites.AsNoTracking().Select(s => s.SiteId).FirstOrDefault() ?? "SITE_01";
            var formateur1 = await CreateDemoUser(userManager, "formateur1@formationmanager.com", "Martin", "Sophie", RoleUtilisateur.Formateur, "Formateur123!", siteId);
            var formateur2 = await CreateDemoUser(userManager, "formateur2@formationmanager.com", "Dubois", "Thomas", RoleUtilisateur.Formateur, "Formateur123!", siteId);
            var responsable = await CreateDemoUser(userManager, "responsable@formationmanager.com", "Petit", "Marie", RoleUtilisateur.ResponsableFormation, "Responsable123!", siteId);

            // Créer des formations
            var formations = new[]
            {
                new Formation
                {
                    Titre = "Communication Digitale",
                    Description = "Maîtriser les outils de communication digitale pour une meilleure visibilité en ligne.",
                    DureeHeures = 21,
                    PrixIndicatif = 1200,
                    Programme = "Module 1: Les fondamentaux du marketing digital\nModule 2: Réseaux sociaux\nModule 3: Création de contenu\nModule 4: Stratégie digitale",
                    Prerequis = "Connaissances de base en informatique",
                    ModalitesPedagogiques = "Formation en présentiel avec exercices pratiques",
                    ModalitesEvaluation = "Quiz final et projet pratique",
                    EstPublique = true,
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                },
                new Formation
                {
                    Titre = "Gestion de Projet",
                    Description = "Apprendre à planifier, exécuter et clôturer des projets avec succès.",
                    DureeHeures = 35,
                    PrixIndicatif = 1800,
                    Programme = "Module 1: Initiation à la gestion de projet\nModule 2: Planification\nModule 3: Exécution et suivi\nModule 4: Clôture et bilan",
                    Prerequis = "Expérience professionnelle souhaitée",
                    ModalitesPedagogiques = "Alternance théorie/pratique, études de cas",
                    ModalitesEvaluation = "Examen écrit et mise en situation",
                    EstPublique = true,
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                },
                new Formation
                {
                    Titre = "Marketing Digital",
                    Description = "Développer une stratégie marketing digitale complète pour votre entreprise.",
                    DureeHeures = 28,
                    PrixIndicatif = 1500,
                    Programme = "Module 1: Stratégie marketing\nModule 2: SEO et référencement\nModule 3: Publicité en ligne\nModule 4: Analyse et mesure",
                    Prerequis = "Connaissances de base en marketing",
                    ModalitesPedagogiques = "Formation interactive avec workshops",
                    ModalitesEvaluation = "Campagne marketing réelle",
                    EstPublique = true,
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                }
            };

            context.Formations.AddRange(formations);
            await context.SaveChangesAsync();

            // Créer des clients
            var clients = new[]
            {
                new Client
                {
                    Nom = "Entreprise ABC",
                    TypeClient = TypeClient.Entreprise,
                    Email = "contact@entreprise-abc.com",
                    Telephone = "0123456789",
                    Adresse = "123 Rue de l'Entreprise",
                    CodePostal = "75001",
                    Ville = "Paris",
                    SIRET = "12345678900012",
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                },
                new Client
                {
                    Nom = "Société XYZ",
                    TypeClient = TypeClient.Entreprise,
                    Email = "formation@societe-xyz.com",
                    Telephone = "0234567890",
                    Adresse = "456 Avenue du Commerce",
                    CodePostal = "69000",
                    Ville = "Lyon",
                    SIRET = "98765432100034",
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                },
                new Client
                {
                    Nom = "Durand Jean",
                    TypeClient = TypeClient.Particulier,
                    Email = "jean.durand@email.com",
                    Telephone = "0612345678",
                    Adresse = "789 Rue de la Maison",
                    CodePostal = "44000",
                    Ville = "Nantes",
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                }
            };

            context.Clients.AddRange(clients);
            await context.SaveChangesAsync();

            // Créer des sessions
            var sessions = new[]
            {
                new Session
                {
                    FormationId = formations[0].Id, // Communication Digitale
                    DateDebut = DateTime.Now.AddDays(7),
                    DateFin = DateTime.Now.AddDays(14),
                    Lieu = "Paris - Centre de formation",
                    EstPublique = true,
                    Statut = "Programmée",
                    NombreMaxStagiaires = 15,
                    FormateurId = formateur1.Id,
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                },
                new Session
                {
                    FormationId = formations[1].Id, // Gestion de Projet
                    DateDebut = DateTime.Now.AddDays(-7),
                    DateFin = DateTime.Now.AddDays(-1),
                    Lieu = "Lyon - Centre d'affaires",
                    EstPublique = false,
                    Statut = "Terminée",
                    NombreMaxStagiaires = 12,
                    FormateurId = formateur2.Id,
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                },
                new Session
                {
                    FormationId = formations[2].Id, // Marketing Digital
                    DateDebut = DateTime.Now.AddDays(21),
                    DateFin = DateTime.Now.AddDays(28),
                    Lieu = "Nantes - Campus numérique",
                    EstPublique = true,
                    Statut = "Programmée",
                    NombreMaxStagiaires = 20,
                    FormateurId = formateur1.Id,
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                }
            };

            context.Sessions.AddRange(sessions);
            await context.SaveChangesAsync();

            // Créer les associations Session-Client
            var sessionClients = new[]
            {
                new SessionClient
                {
                    SessionId = sessions[0].Id,
                    ClientId = clients[0].Id, // Entreprise ABC
                    TarifNegocie = 1000,
                    NombrePlaces = 5,
                    TypeFinancement = "OPCA",
                    SiteId = siteId
                },
                new SessionClient
                {
                    SessionId = sessions[1].Id,
                    ClientId = clients[1].Id, // Société XYZ
                    TarifNegocie = 1600,
                    NombrePlaces = 8,
                    TypeFinancement = "CPF",
                    SiteId = siteId
                },
                new SessionClient
                {
                    SessionId = sessions[2].Id,
                    ClientId = clients[2].Id, // Durand Jean
                    TarifNegocie = 1200,
                    NombrePlaces = 1,
                    TypeFinancement = "Personnel",
                    SiteId = siteId
                }
            };

            context.SessionClients.AddRange(sessionClients);
            await context.SaveChangesAsync();

            // Créer des stagiaires
            var stagiaires = new[]
            {
                new Stagiaire
                {
                    ClientId = clients[0].Id,
                    Nom = "Martin",
                    Prenom = "Pierre",
                    Email = "pierre.martin@entreprise-abc.com",
                    Telephone = "0612345678",
                    Poste = "Chef de projet",
                    SessionId = sessions[0].Id,
                    StatutInscription = "Inscrit",
                    HeuresPresence = 0,
                    EstPresent = true,
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                },
                new Stagiaire
                {
                    ClientId = clients[0].Id,
                    Nom = "Bernard",
                    Prenom = "Sophie",
                    Email = "sophie.bernard@entreprise-abc.com",
                    Telephone = "0623456789",
                    Poste = "Responsable marketing",
                    SessionId = sessions[0].Id,
                    StatutInscription = "Inscrit",
                    HeuresPresence = 0,
                    EstPresent = true,
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                },
                new Stagiaire
                {
                    ClientId = clients[1].Id,
                    Nom = "Petit",
                    Prenom = "Thomas",
                    Email = "thomas.petit@societe-xyz.com",
                    Telephone = "0634567890",
                    Poste = "Développeur",
                    SessionId = sessions[1].Id,
                    StatutInscription = "Terminé",
                    HeuresPresence = 35,
                    EstPresent = true,
                    EvaluationAChaud = 17,
                    EvaluationAFroid = 16,
                    AttestationGeneree = true,
                    DateAttestation = DateTime.Now.AddDays(-2),
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                },
                new Stagiaire
                {
                    ClientId = clients[2].Id,
                    Nom = "Durand",
                    Prenom = "Jean",
                    Email = "jean.durand@email.com",
                    Telephone = "0612345678",
                    Poste = "Consultant",
                    SessionId = sessions[2].Id,
                    StatutInscription = "Inscrit",
                    HeuresPresence = 0,
                    EstPresent = true,
                    CreePar = "admin@formationmanager.com",
                    SiteId = siteId
                }
            };

            context.Stagiaires.AddRange(stagiaires);
            await context.SaveChangesAsync();

            // Créer des actions de veille
            var actionsVeille = new[]
            {
                new ActionVeille
                {
                    FormateurId = formateur1.Id,
                    Type = ActionVeille.TypeVeille.MetierCompetences,
                    Titre = "Veille sur les nouvelles tendances du marketing digital",
                    Description = "Analyse des dernières innovations en matière de marketing digital et d'IA",
                    DateAction = DateTime.Now.AddDays(-10),
                    Duree = 3,
                    Preuves = "Articles lus, webinaires suivis",
                    CreePar = "formateur1@formationmanager.com",
                    SiteId = siteId
                },
                new ActionVeille
                {
                    FormateurId = formateur2.Id,
                    Type = ActionVeille.TypeVeille.Pedagogique,
                    Titre = "Formation aux nouvelles méthodes pédagogiques",
                    Description = "Participation à un atelier sur les méthodes d'apprentissage actif",
                    DateAction = DateTime.Now.AddDays(-15),
                    Duree = 7,
                    Preuves = "Certificat de formation",
                    CreePar = "formateur2@formationmanager.com",
                    SiteId = siteId
                }
            };

            context.ActionsVeille.AddRange(actionsVeille);
            await context.SaveChangesAsync();
        }

        private static async Task EnsureQualiopiIndicators(FormationDbContext context)
        {
            var sites = await context.Sites.AsNoTracking()
                .Select(s => s.SiteId)
                .ToListAsync();

            if (sites.Count == 0)
            {
                return;
            }

            var definitions = GetQualiopiIndicatorDefinitions();
            foreach (var siteId in sites)
            {
                var existing = await context.IndicateursQualiopi
                    .Where(i => i.SiteId == siteId)
                    .ToListAsync();

                var hasProofs = await context.PreuvesQualiopi
                    .AnyAsync(p => p.SiteId == siteId);

                var hasLegacyCodes = existing.Any(i => i.CodeIndicateur.Contains('.'));
                if (!hasProofs && (existing.Count == 0 || hasLegacyCodes))
                {
                    if (existing.Count > 0)
                    {
                        context.IndicateursQualiopi.RemoveRange(existing);
                        await context.SaveChangesAsync();
                    }

                    var now = DateTime.Now;
                    var allIndicators = definitions.Select(def => new IndicateurQualiopi
                    {
                        CodeIndicateur = def.Code,
                        Libelle = def.Label,
                        Critere = def.Critere,
                        NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen,
                        DateCreation = now,
                        DateModification = now,
                        CreePar = "system",
                        ModifiePar = "system",
                        SiteId = siteId
                    }).ToList();

                    context.IndicateursQualiopi.AddRange(allIndicators);
                    await context.SaveChangesAsync();
                    continue;
                }

                var existingCodes = existing
                    .Select(i => i.CodeIndicateur)
                    .ToHashSet();
                var missing = definitions
                    .Where(def => !existingCodes.Contains(def.Code))
                    .ToList();

                if (missing.Count == 0)
                {
                    continue;
                }

                var timestamp = DateTime.Now;
                var toAdd = missing.Select(def => new IndicateurQualiopi
                {
                    CodeIndicateur = def.Code,
                    Libelle = def.Label,
                    Critere = def.Critere,
                    NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen,
                    DateCreation = timestamp,
                    DateModification = timestamp,
                    CreePar = "system",
                    ModifiePar = "system",
                    SiteId = siteId
                }).ToList();

                context.IndicateursQualiopi.AddRange(toAdd);
                await context.SaveChangesAsync();
            }
        }

        private static List<(string Code, string Label, int Critere)> GetQualiopiIndicatorDefinitions()
        {
            return new List<(string Code, string Label, int Critere)>
            {
                ("1", "Information du public", 1),
                ("2", "Indicateurs de résultats", 1),
                ("3", "Taux d'obtention des certifications", 1),
                ("4", "Analyse du besoin", 2),
                ("5", "Objectifs de la prestation", 2),
                ("6", "Contenus et modalités", 2),
                ("7", "Contenus et exigences", 2),
                ("8", "Positionnement à l'entrée", 2),
                ("9", "Conditions de déroulement", 3),
                ("10", "Adaptation de la prestation", 3),
                ("11", "Atteinte des objectifs", 3),
                ("12", "Engagement des bénéficiaires", 3),
                ("13", "Coordination des apprentis", 3),
                ("14", "Exercice de la citoyenneté", 3),
                ("15", "Droits et devoirs de l'apprenti", 3),
                ("16", "Présentation à la certification", 3),
                ("17", "Moyens humains et techniques", 4),
                ("18", "Coordination des acteurs", 4),
                ("19", "Ressources pédagogiques", 4),
                ("20", "Personnels dédiés", 4),
                ("21", "Compétences des acteurs", 5),
                ("22", "Gestion de la compétence", 5),
                ("23", "Veille légale et réglementaire", 6),
                ("24", "Veille des emplois et métiers", 6),
                ("25", "Veille pédagogique et technologique", 6),
                ("26", "Situation de handicap", 6),
                ("27", "Disposition sous-traitance", 6),
                ("28", "Formation en situation de travail", 6),
                ("29", "Insertion professionnelle", 6),
                ("30", "Recueil des appréciations", 7),
                ("31", "Traitement des réclamations", 7),
                ("32", "Amélioration continue", 7)
            };
        }

        private static async Task<Utilisateur> CreateDemoUser(UserManager<Utilisateur> userManager, string email, string nom, string prenom, RoleUtilisateur role, string password, string siteId)
        {
            var user = new Utilisateur
            {
                UserName = email,
                Email = email,
                Nom = nom,
                Prenom = prenom,
                Role = role,
                SiteId = siteId,
                Actif = true,
                EmailConfirmed = true,
                CreePar = "admin@formationmanager.com"
            };

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                await userManager.CreateAsync(user, password);
                
                var roleName = role.ToString();
                await userManager.AddToRoleAsync(user, roleName);
            }

            return existingUser ?? user;
        }
    }
}
