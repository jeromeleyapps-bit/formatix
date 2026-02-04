using System;
using System.Collections.Generic;

namespace FormationManager.Models
{
    public class SystemDiagnosticsViewModel
    {
        // Base de données
        public bool DatabaseCanConnect { get; set; }
        public string DatabaseError { get; set; } = string.Empty;
        public string DatabasePath { get; set; } = string.Empty;
        public bool DatabaseFileExists { get; set; }
        public long? DatabaseFileSizeBytes { get; set; }

        // Dossiers fichiers
        public string UploadsPath { get; set; } = string.Empty;
        public bool UploadsFolderOk { get; set; }
        public string ExamplesPath { get; set; } = string.Empty;
        public bool ExamplesFolderOk { get; set; }
        public string GeneratedPath { get; set; } = string.Empty;
        public bool GeneratedFolderOk { get; set; }

        // Espace disque
        public string ContentRootPath { get; set; } = string.Empty;
        public long? ContentRootFreeSpaceBytes { get; set; }

        // Configuration inscription distante
        public string BaseUrlInscription { get; set; } = string.Empty;
        public bool BaseUrlInscriptionConfigured { get; set; }
        public bool BaseUrlInscriptionLooksPublic { get; set; }

        // Configuration multi-site / sync
        public string SyncSiteId { get; set; } = string.Empty;
        public bool SyncSiteIdDefined { get; set; }
        public bool SyncSiteIdKnownInSites { get; set; }

        // Résumé
        public List<string> Warnings { get; set; } = new();
        public bool HasWarnings => Warnings.Count > 0;
    }
}

