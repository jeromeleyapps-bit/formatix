using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;

namespace FormationManager.Services
{
    public interface IQualiopiAutoProofService
    {
        Task AutoCreatePreuvesForFormationAsync(int formationId);
        Task AutoCreatePreuvesForSessionAsync(int sessionId);
        Task AutoCreatePreuvesForStagiaireAsync(int stagiaireId);
        Task<string> GetQualiopiGuideAsync();
    }

    public class QualiopiAutoProofService : IQualiopiAutoProofService
    {
        private readonly FormationDbContext _context;
        private readonly ILogger<QualiopiAutoProofService> _logger;

        public QualiopiAutoProofService(FormationDbContext context, ILogger<QualiopiAutoProofService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AutoCreatePreuvesForFormationAsync(int formationId)
        {
            var formation = await _context.Formations
                .Include(f => f.Sessions)
                .FirstOrDefaultAsync(f => f.Id == formationId);

            if (formation == null) return;

            // Les preuves de formation seront créées lors de la création de la première session
            // car PreuveQualiopi nécessite un SessionId
            // On stocke juste l'info pour référence future
            _logger.LogInformation($"Formation {formationId} créée - Les preuves seront générées lors de la création de sessions");
        }

        public async Task AutoCreatePreuvesForSessionAsync(int sessionId)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.Formateur)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return;

            // Critère 1 : Information du public
            // Indicateur 1 : Information du public (via la formation)
            if (session.Formation != null)
            {
                await CreatePreuveIfNotExistsAsync(
                    session.SiteId,
                    sessionId,
                    "1", // Information du public
                    $"Formation : {session.Formation.Titre}",
                    $"Formation '{session.Formation.Titre}' - Description : {session.Formation.Description}",
                    PreuveQualiopi.TypePreuve.Document
                );

                // Critère 2 : Analyse du besoin, Contenus
                if (!string.IsNullOrWhiteSpace(session.Formation.Prerequis))
                {
                    await CreatePreuveIfNotExistsAsync(
                        session.SiteId,
                        sessionId,
                        "4", // Analyse du besoin
                        $"Prérequis : {session.Formation.Titre}",
                        $"Prérequis : {session.Formation.Prerequis}",
                        PreuveQualiopi.TypePreuve.Document
                    );
                }

                if (!string.IsNullOrWhiteSpace(session.Formation.Programme))
                {
                    await CreatePreuveIfNotExistsAsync(
                        session.SiteId,
                        sessionId,
                        "6", // Contenus et modalités
                        $"Programme : {session.Formation.Titre}",
                        $"Programme : {session.Formation.Programme}",
                        PreuveQualiopi.TypePreuve.Document
                    );
                }

                if (!string.IsNullOrWhiteSpace(session.Formation.ModalitesPedagogiques))
                {
                    await CreatePreuveIfNotExistsAsync(
                        session.SiteId,
                        sessionId,
                        "6", // Contenus et modalités
                        $"Modalités pédagogiques : {session.Formation.Titre}",
                        $"Modalités : {session.Formation.ModalitesPedagogiques}",
                        PreuveQualiopi.TypePreuve.Document
                    );
                }
            }

            // Critère 2 : Objectifs de la prestation
            // Indicateur 5
            await CreatePreuveIfNotExistsAsync(
                session.SiteId,
                sessionId,
                "5", // Objectifs de la prestation
                $"Session programmée : {session.Formation?.Titre ?? "Formation"}",
                $"Session du {session.DateDebut:dd/MM/yyyy} au {session.DateFin:dd/MM/yyyy} - Lieu : {session.Lieu}",
                PreuveQualiopi.TypePreuve.Document
            );

            // Critère 3 : Conditions de déroulement
            // Indicateur 9
            await CreatePreuveIfNotExistsAsync(
                session.SiteId,
                sessionId,
                "9", // Conditions de déroulement
                $"Conditions de déroulement - Session {session.Formation?.Titre ?? "Formation"}",
                $"Dates : {session.DateDebut:dd/MM/yyyy} - {session.DateFin:dd/MM/yyyy}, Lieu : {session.Lieu}, Max stagiaires : {session.NombreMaxStagiaires}",
                PreuveQualiopi.TypePreuve.Document
            );

            // Critère 4 : Moyens humains et techniques
            // Indicateur 17
            if (session.Formateur != null)
            {
                await CreatePreuveIfNotExistsAsync(
                    session.SiteId,
                    sessionId,
                    "17", // Moyens humains et techniques
                    $"Formateur assigné : {session.Formateur.Prenom} {session.Formateur.Nom}",
                    $"Formateur : {session.Formateur.Prenom} {session.Formateur.Nom} - Statut : {session.Formateur.StatutProfessionnel}",
                    PreuveQualiopi.TypePreuve.Document
                );
            }

            // Critère 5 : Compétences des acteurs
            // Indicateur 21
            if (session.Formateur != null && !string.IsNullOrWhiteSpace(session.Formateur.Competences))
            {
                await CreatePreuveIfNotExistsAsync(
                    session.SiteId,
                    sessionId,
                    "21", // Compétences des acteurs
                    $"Compétences du formateur : {session.Formateur.Prenom} {session.Formateur.Nom}",
                    $"Compétences : {session.Formateur.Competences}",
                    PreuveQualiopi.TypePreuve.Document
                );
            }

            _logger.LogInformation($"Preuves Qualiopi auto-créées pour la session {sessionId}");
        }

        public async Task AutoCreatePreuvesForStagiaireAsync(int stagiaireId)
        {
            var stagiaire = await _context.Stagiaires
                .Include(s => s.Session)
                    .ThenInclude(s => s!.Formation)
                .Include(s => s.Client)
                .FirstOrDefaultAsync(s => s.Id == stagiaireId);

            if (stagiaire == null || stagiaire.Session == null) return;

            var sessionId = stagiaire.Session.Id;

            // Critère 2 : Positionnement à l'entrée
            // Indicateur 8
            await CreatePreuveIfNotExistsAsync(
                stagiaire.SiteId,
                sessionId,
                "8", // Positionnement à l'entrée
                $"Inscription stagiaire : {stagiaire.Prenom} {stagiaire.Nom}",
                $"Stagiaire inscrit : {stagiaire.Prenom} {stagiaire.Nom} - Poste : {stagiaire.Poste} - Service : {stagiaire.Service}",
                PreuveQualiopi.TypePreuve.Document
            );

            // Critère 3 : Engagement des bénéficiaires
            // Indicateur 12
            await CreatePreuveIfNotExistsAsync(
                stagiaire.SiteId,
                sessionId,
                "12", // Engagement des bénéficiaires
                $"Inscription validée : {stagiaire.Prenom} {stagiaire.Nom}",
                $"Stagiaire inscrit avec statut : {stagiaire.StatutInscription}",
                PreuveQualiopi.TypePreuve.Document
            );

            // Critère 7 : Recueil des appréciations
            // Indicateur 30
            // (Sera complété lors de la génération d'évaluations)

            _logger.LogInformation($"Preuves Qualiopi auto-créées pour le stagiaire {stagiaireId}");
        }

        private async Task CreatePreuveIfNotExistsAsync(
            string siteId,
            int sessionId,
            string codeIndicateur,
            string titre,
            string description,
            PreuveQualiopi.TypePreuve type)
        {
            var indicateur = await _context.IndicateursQualiopi
                .FirstOrDefaultAsync(i => i.CodeIndicateur == codeIndicateur && 
                                         (string.IsNullOrEmpty(i.SiteId) || i.SiteId == siteId));

            if (indicateur == null)
            {
                _logger.LogWarning($"Indicateur Qualiopi {codeIndicateur} non trouvé pour le site {siteId}");
                return;
            }

            var exists = await _context.PreuvesQualiopi.AnyAsync(p =>
                p.SessionId == sessionId &&
                p.IndicateurQualiopiId == indicateur.Id &&
                p.Titre == titre);

            if (exists) return;

            var preuve = new PreuveQualiopi
            {
                SessionId = sessionId,
                IndicateurQualiopiId = indicateur.Id,
                Titre = titre,
                Description = description,
                Type = type,
                SiteId = siteId,
                EstValide = false, // À valider manuellement
                DateCreation = DateTime.Now,
                DateModification = DateTime.Now,
                CreePar = "system",
                ModifiePar = "system"
            };

            _context.PreuvesQualiopi.Add(preuve);
            await _context.SaveChangesAsync();
        }


        public async Task<string> GetQualiopiGuideAsync()
        {
            return @"# Guide Qualiopi - Lien entre vos actions et les critères

## Comment vos actions génèrent des preuves Qualiopi

### 1. Création d'une Formation
**Critères couverts :**
- **Critère 1** (Information du public) : La formation est enregistrée dans le système
- **Critère 2** (Analyse du besoin) : Si vous remplissez les prérequis
- **Critère 2** (Contenus et modalités) : Si vous remplissez le programme et les modalités pédagogiques

**Preuves générées automatiquement :**
- Fiche formation avec description
- Programme de formation (si renseigné)
- Modalités pédagogiques (si renseignées)
- Prérequis (si renseignés)

### 2. Création d'une Session
**Critères couverts :**
- **Critère 2** (Objectifs de la prestation) : Session programmée avec dates et lieu
- **Critère 3** (Conditions de déroulement) : Dates, lieu, nombre de stagiaires
- **Critère 4** (Moyens humains) : Formateur assigné
- **Critère 5** (Compétences) : Compétences du formateur (si renseignées)

**Preuves générées automatiquement :**
- Planning de session
- Attribution formateur
- Conditions de déroulement

### 3. Inscription d'un Stagiaire
**Critères couverts :**
- **Critère 2** (Positionnement à l'entrée) : Inscription avec informations professionnelles
- **Critère 3** (Engagement des bénéficiaires) : Statut d'inscription

**Preuves générées automatiquement :**
- Fiche d'inscription stagiaire
- Positionnement professionnel

### 4. Génération de Documents
**Critères couverts :**
- **Critère 2** (Contenus) : Convention de formation
- **Critère 3** (Suivi) : Feuille d'émargement
- **Critère 3** (Atteinte des objectifs) : Évaluation
- **Critère 7** (Recueil des appréciations) : Attestation et évaluation

**Preuves générées automatiquement :**
- Convention de formation (PDF généré)
- Feuille d'émargement (PDF généré)
- Attestation de formation (PDF généré)
- Évaluation (PDF généré)

### 5. Upload de Documents
**Critères couverts :**
- Tous les critères selon le type de document et l'analyse IA

**Preuves générées automatiquement :**
- Analyse OCR + IA du document
- Liaison automatique aux critères détectés

## Processus de validation pour l'audit

1. **Création automatique** : Les preuves sont créées automatiquement lors de vos actions
2. **Validation manuelle** : Vous devez valider chaque preuve dans l'onglet 'Preuves Qualiopi'
3. **Vérification** : Consultez le rapport de conformité pour voir les critères couverts
4. **Complétion** : Ajoutez des preuves manuellement si nécessaire (photos, témoignages, etc.)

## Indicateurs de conformité

- **Vert** : Critère conforme (toutes les preuves requises sont validées)
- **Rouge** : Critère non conforme (preuves manquantes ou non validées)
- **Barre de progression** : Pourcentage global de conformité

## Conseils pour l'audit

1. **Complétez toutes les informations** lors de la création (programme, modalités, prérequis)
2. **Générez tous les documents** (convention, émargement, attestation, évaluation)
3. **Validez toutes les preuves** dans l'onglet 'Preuves'
4. **Consultez régulièrement** le rapport de conformité
5. **Ajoutez des preuves complémentaires** si nécessaire (photos de sessions, témoignages, etc.)";
        }
    }
}
