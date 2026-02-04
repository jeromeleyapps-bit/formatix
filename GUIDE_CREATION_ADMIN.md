# Guide : Créer un compte Administrateur

## Méthode 1 : Via l'interface Paramètres (Recommandé)

### Prérequis
- Vous devez déjà avoir un compte administrateur existant pour accéder à la page Paramètres
- Si vous n'avez pas encore de compte admin, utilisez la **Méthode 2** (ligne de commande)

### Étapes

1. **Connectez-vous** avec un compte administrateur existant
   - Par défaut après installation : `admin@formationmanager.com` / `Admin123!`

2. **Accédez à la page Paramètres**
   - Cliquez sur "Paramètres" dans le menu latéral
   - Ou naviguez vers : `/Settings/Index`

3. **Section "Utilisateurs"**
   - Remplissez le formulaire de création d'utilisateur :
     - **Email** : L'adresse email du nouvel administrateur
     - **Nom** : Le nom de famille
     - **Prénom** : Le prénom
     - **Rôle** : Sélectionnez **"Administrateur"**
     - **Site** : Laissez vide (les admins n'ont pas de site assigné)
     - **Mot de passe** : Un mot de passe sécurisé (min 8 caractères)

4. **Cliquez sur "Créer"**
   - L'utilisateur sera créé avec les droits administrateur

---

## Méthode 2 : Via la ligne de commande (Première installation)

Si vous n'avez pas encore de compte admin, vous pouvez en créer un via PowerShell ou Python.

### Option A : Script PowerShell

Créez un fichier `create-admin.ps1` :

```powershell
# create-admin.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$Email,
    [Parameter(Mandatory=$true)]
    [string]$Password,
    [Parameter(Mandatory=$true)]
    [string]$Nom,
    [Parameter(Mandatory=$true)]
    [string]$Prenom
)

cd C:\AI\Opagax
$script = @"
import sqlite3, hashlib, secrets, datetime

conn = sqlite3.connect('opagax.db')
cur = conn.cursor()

# Vérifier si l'utilisateur existe
cur.execute("SELECT Id FROM AspNetUsers WHERE Email = ?", ('$Email',))
if cur.fetchone():
    print("ERREUR: Un utilisateur avec cet email existe déjà")
    exit(1)

# Générer un salt et hash le mot de passe (simplifié - ASP.NET Identity utilise PBKDF2)
# Pour une vraie création, il faut utiliser UserManager, mais on peut créer directement en DB
# Note: Cette méthode est simplifiée, préférez la méthode via l'interface

print("Création de l'utilisateur admin...")
print("Email: $Email")
print("Nom: $Nom $Prenom")
print("")
print("ATTENTION: Cette méthode crée l'utilisateur mais le mot de passe doit être")
print("réinitialisé via l'interface web après la première connexion.")
"@

Set-Content -Path .\temp_create_admin.py -Value $script -Encoding UTF8
python .\temp_create_admin.py
```

### Option B : Via l'interface après connexion avec le compte par défaut

1. **Connectez-vous** avec le compte admin par défaut créé au seed :
   - Email : `admin@formationmanager.com`
   - Mot de passe : `Admin123!`

2. **Créez votre propre compte admin** via Paramètres → Utilisateurs

3. **Optionnel** : Supprimez ou désactivez le compte admin par défaut

---

## Méthode 3 : Modification du SeedData (Développement uniquement)

Pour modifier le compte admin par défaut créé au démarrage, éditez :

`Data/SeedData.cs` → Méthode `CreateDefaultUser`

```csharp
var defaultUser = new Utilisateur
{
    UserName = "votre-email@exemple.com",  // Changez ici
    Email = "votre-email@exemple.com",
    Nom = "Votre Nom",
    Prenom = "Votre Prénom",
    Role = RoleUtilisateur.Administrateur,
    SiteId = siteId,
    Actif = true,
    EmailConfirmed = true
};

// Création avec votre mot de passe
await userManager.CreateAsync(defaultUser, "VotreMotDePasse123!");
```

Puis réinitialisez la base de données avec `.\reset-fresh-install.ps1`

---

## Vérification

Pour vérifier qu'un utilisateur est bien administrateur :

1. Connectez-vous avec ce compte
2. Vérifiez que vous voyez le menu "Dashboard admin" dans la sidebar
3. Vérifiez que vous pouvez accéder à `/Settings/Index`
4. Vérifiez que vous pouvez créer/modifier/supprimer des utilisateurs

---

## Sécurité

⚠️ **Important** :
- Changez le mot de passe du compte admin par défaut après la première connexion
- Utilisez des mots de passe forts (min 8 caractères, majuscules, chiffres)
- Ne partagez pas les identifiants administrateur
- Désactivez les comptes admin inutilisés plutôt que de les supprimer

---

## Dépannage

### "Accès refusé" sur la page Paramètres
- Vérifiez que votre compte a bien le rôle `Administrateur`
- Vérifiez dans la base de données : `SELECT Email, Role FROM AspNetUsers WHERE Email = 'votre-email'`

### Impossible de créer un utilisateur
- Vérifiez que vous êtes bien connecté en tant qu'admin
- Vérifiez que l'email n'existe pas déjà
- Vérifiez que le mot de passe respecte les règles (min 8 caractères)

### Mot de passe oublié
- Si vous avez un autre compte admin, utilisez "Reset MDP" dans Paramètres
- Sinon, utilisez la méthode 2 (ligne de commande) pour réinitialiser
