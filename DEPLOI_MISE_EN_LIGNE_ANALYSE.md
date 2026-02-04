# Déploiement FormatiX en ligne – Analyse Heroku + MongoDB et alternatives

## La proposition reçue

> « J'utilise également le niveau gratuit Heroku et Mongo pour mon backend (niveau gratuit) »

Ce schéma (Heroku + MongoDB) est souvent utilisé pour des apps **Node.js / JavaScript** avec une base **document**. FormatiX est une application **ASP.NET Core** avec une base **relationnelle (SQLite + Entity Framework Core)**. Voici ce qui est **réaliste** et ce qui ne l’est pas.

---

## 1. Stack actuelle de FormatiX

| Composant | Techno |
|-----------|--------|
| Backend | ASP.NET Core 9, C# |
| Base de données | **SQLite** (fichier `opagax.db`) + **Entity Framework Core** |
| Auth | ASP.NET Core Identity (stocké en base) |
| Sync multisites | API centralisée (`Sync.CentralUrl`), `SiteId` par site |
| Fichiers | `wwwroot/uploads`, `wwwroot/examples` (disque local) |
| Optionnel | Tesseract, Ollama, Ghostscript (binaires externes) |

Modèle **relationnel** : formations, sessions, stagiaires, documents, preuves Qualiopi, etc., avec clés étrangères et migrations EF.

---

## 2. Heroku

### Offre actuelle

- **Plus de tier gratuit** depuis fin 2022. Le plus abordable est l’**Eco dyno** (~5 $/mois).
- Heroku **supporte .NET** (buildpacks, déploiement Git).
- **Disque éphémère** : le système de fichiers est réinitialisé à chaque redéploiement.  
  → **SQLite (fichier local) n’est pas utilisable** en production sur Heroku. Il faut une base **externe** (PostgreSQL, etc.).

### Conclusion Heroku

- Héberger FormatiX sur Heroku est **possible** (Eco dyno + base gérée).
- **Impossible** de rester en SQLite seul : il faut migrer vers une base gérée (ex. **Heroku Postgres**).

---

## 3. MongoDB

### Offre gratuite

- **MongoDB Atlas M0** : cluster gratuit (512 Mo), pas de carte bancaire exigée.

### Compatibilité avec FormatiX

- FormatiX repose sur un **modèle relationnel** (EF Core, `DbContext`, migrations, clés étrangères, jointures).
- MongoDB est une base **document**. Il existe un **provider EF Core pour MongoDB** (`MongoDB.EntityFrameworkCore`), mais :
  - le modèle actuel (tables, relations, Identity) est pensé pour du SQL ;
  - l’adapter à MongoDB implique **refonte du modèle**, des requêtes et éventuellement d’Identity.

**Faire tourner FormatiX sur MongoDB est donc possible en théorie, mais demande une réécriture importante du data layer.** Ce n’est pas un simple changement de connexion.

### Conclusion MongoDB

- **MongoDB gratuit** : oui, pour une *nouvelle* app ou un backend déjà prévu pour du document.
- **FormatiX actuel** : garder EF Core + SQL/PostgreSQL est **beaucoup plus simple** que de passer à MongoDB.

---

## 4. Ce qui est réellement possible

### Option A : Heroku + PostgreSQL (recommandé si vous restez sur Heroku)

- **Heroku** : Eco dyno (~5 $/mois).
- **Heroku Postgres** : add-on (il existe un niveau gratuit / peu cher selon l’offre actuelle Heroku).
- FormatiX : **remplacer SQLite par PostgreSQL** :
  - Ajouter `Npgsql` (EF Core pour PostgreSQL).
  - Adapter `ConnectionStrings` et config (variables d’environnement).
  - Conserver le même `DbContext` et les migrations (avec adaptations éventuelles pour Postgres).
- **Fichiers** (`uploads`, `examples`) : sur Heroku le disque est éphémère → il faut un **stockage externe** (ex. S3, stockage cloud) pour les pièces jointes et documents.

**Effort** : modéré (config + migration de données + stockage fichiers).

### Option B : Railway + PostgreSQL

- **Railway** : déploiement depuis GitHub, pas de carte bancaire pour commencer, facturation à l’usage.
- **PostgreSQL** : proposé nativement sur Railway.
- Même idée qu’option A : FormatiX avec **PostgreSQL** au lieu de SQLite, et **stockage externe** pour les fichiers.

**Effort** : comparable à l’option A.

### Option C : Fly.io + PostgreSQL

- **Fly.io** : support .NET, offre gratuite limitée.
- **Postgres** : géré par Fly (ou base externe).
- Même principe : EF Core + Postgres, fichiers hors disque local.

**Effort** : comparable.

### Option D : Azure App Service (niveau gratuit) + Base SQL

- **Azure App Service** : tier gratuit (F1) pour une app .NET.
- Base : **Azure SQL** ou **PostgreSQL** (attention, les bases managées Azure ont souvent un coût même en « free tier » limité).
- FormatiX : configurer la connexion vers la base Azure.

**Effort** : modéré, avec une logique Azure (paramètres, key vault, etc.) à prendre en main.

---

## 5. Multisites

FormatiX gère déjà **plusieurs sites** :

- `Sites` (SITE_01, SITE_02, …), `SiteId` par entité.
- **Sync** : `CentralUrl`, `SiteId`, API centralisée.

**Déploiement « multisites »** peut vouloir dire :

1. **Une seule app en ligne** qui sert tous les sites (rôle actuel de `SiteId` + Sync).  
   → Un déploiement Heroku / Railway / Fly / Azure suffit. Les « multisites » restent dans la config et les données.

2. **Plusieurs instances** (une par site ou par région).  
   → Plusieurs déploiements (ex. plusieurs dynos/apps) et une configuration Sync adaptée (`CentralUrl`, etc.).

Les options A–D ci‑dessus permettent au moins le cas (1). Le cas (2) se règle par l’architecture (plusieurs apps, une centrale, etc.).

---

## 6. Récapitulatif

| Proposition | Réaliste pour FormatiX ? | Commentaire |
|-------------|---------------------------|-------------|
| **Heroku gratuit** | Non | Plus de tier gratuit Heroku. |
| **Heroku (payant) + MongoDB** | Possible mais lourd | Grosse refonte du data layer (EF → document). |
| **Heroku (payant) + PostgreSQL** | Oui | Adapter EF à Postgres + stockage fichiers externe. |
| **MongoDB gratuit (M0)** | Possible pour une *nouvelle* app | Peu adapté à FormatiX tel quel (modèle relationnel). |
| **Railway / Fly.io / Azure + Postgres** | Oui | Variantes réalistes pour mettre FormatiX en ligne. |

---

## 7. Recommandation pratique

- **Garder un modèle relationnel (EF Core + SQL)** : rester sur **PostgreSQL** (ou SQL Server si Azure) plutôt que MongoDB.
- **Hébergement** :
  - **Budget limité** : **Railway** ou **Fly.io** + Postgres (souvent moins cher / plus souple qu’Heroku).
  - **Environnement Microsoft** : **Azure App Service** + base SQL/Postgres.
  - **Préférence Heroku** : **Heroku Eco + Heroku Postgres**, en prévoyant un **stockage cloud** pour les fichiers.

Dans tous les cas, **prévoir** :

1. Remplacement de SQLite par **PostgreSQL** (ou équivalent).
2. **Stockage externe** pour `uploads` et `examples` (S3, Azure Blob, etc.).
3. **Variables d’environnement** pour chaînes de connexion, `Sync.CentralUrl`, clés API, etc.
4. **Pas de Tesseract/Ollama** sur les petits tiers gratuits (ou les désactiver / les héberger séparément si besoin).

Si vous précisez la cible (Heroku, Railway, Fly, Azure), on peut détailler les étapes concrètes (config EF, connexion, migrations, stockage fichiers) pour FormatiX.
