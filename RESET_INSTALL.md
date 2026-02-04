# Réinitialisation complète - Test première installation

Ce guide permet de réinitialiser complètement l'application FormatiX pour tester comme une première installation.

## ⚠️ ATTENTION

Cette opération va **SUPPRIMER TOUTES LES DONNÉES** :
- Base de données SQLite (opagax.db)
- Tous les logs
- Tous les fichiers uploadés/générés
- Tous les fichiers temporaires

## 🚀 Utilisation

### Option 1 : Script PowerShell (recommandé)

```powershell
.\reset-fresh-install.ps1
```

### Option 2 : Script Batch (Windows)

Double-cliquez sur `reset-fresh-install.bat` ou exécutez :

```cmd
reset-fresh-install.bat
```

## 📋 Ce que fait le script

1. ✅ Arrête l'application si elle est en cours d'exécution
2. ✅ Supprime la base de données SQLite (opagax.db, opagax.db-shm, opagax.db-wal)
3. ✅ Supprime tous les fichiers de logs
4. ✅ Nettoie les dossiers uploads/generated/examples
5. ✅ Supprime les fichiers temporaires (temp_*.py, temp_*.pdf, etc.)

## 🔄 Après la réinitialisation

1. **Lancez l'application** :
   ```bash
   dotnet run
   ```

2. **La base de données sera recréée automatiquement** avec :
   - Toutes les migrations appliquées
   - Les données de seed (utilisateurs, formations, indicateurs Qualiopi)

3. **Connectez-vous** avec les identifiants par défaut :
   - **Email** : `admin@formationmanager.com`
   - **Mot de passe** : `Admin123!`

## 👥 Utilisateurs de démonstration créés

- **Admin** : `admin@formationmanager.com` / `Admin123!`
- **Formateur 1** : `formateur1@formationmanager.com` / `Formateur123!`
- **Formateur 2** : `formateur2@formationmanager.com` / `Formateur123!`
- **Responsable** : `responsable@formationmanager.com` / `Responsable123!`

## 📊 Données de seed

Par défaut, **les données de démonstration sont DÉSACTIVÉES** (`CreateDemoData: false`).

Après réinitialisation, vous aurez :
- ✅ **1 utilisateur admin** : `admin@formationmanager.com` / `Admin123!`
- ✅ **5 sites** configurés (SITE_01 à SITE_05)
- ✅ **160 indicateurs Qualiopi** (32 × 5 sites)
- ❌ **Aucune formation, session, client, stagiaire** (base vierge)

### Activer les données de démonstration (optionnel)

Si vous voulez tester avec des données de démo, modifiez `appsettings.json` :

```json
"AppSettings": {
  "CreateDemoData": true
}
```

Puis réinitialisez la base de données.

## 🔧 Dépannage

Si l'application ne démarre pas après la réinitialisation :

1. Vérifiez que tous les processus sont arrêtés :
   ```powershell
   Get-Process FormationManager -ErrorAction SilentlyContinue
   ```

2. Vérifiez que la base de données a bien été supprimée :
   ```powershell
   Test-Path opagax.db
   ```
   (Doit retourner `False`)

3. Relancez le build :
   ```bash
   dotnet clean
   dotnet build
   dotnet run
   ```

## 📝 Notes

- Les fichiers de migration sont **conservés** (dans le dossier `Migrations/`)
- Les fichiers source ne sont **pas modifiés**
- Seules les **données** et **fichiers générés** sont supprimés
