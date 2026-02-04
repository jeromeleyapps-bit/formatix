# Veille RSS – Critère 6 (indicateurs 23 à 29) – Concept et faisabilité

## Contexte

Les **critères 23 à 29** (volet 6 : veille, handicap, sous-traitance, insertion, etc.) **ne se prouvent pas par des documents de formation** (programme, émargement, convocation…). Ils relèvent d’une **veille** (réglementaire, métiers, pédagogique, handicap, etc.) et d’actions continues.

L’idée : **une tuile dashboard** qui recense les **flux RSS** utiles au critère 6, affiche les **actualités**, et permet une **« validation de lecture »** par actualité. Chaque validation **valide explicitement un indicateur** (23, 24, 25, 26, 27, 28 ou 29). Une **analyse fine** par actualité permet de **suggérer** l’indicateur le plus pertinent (ex. 23 vs 26).

---

## Faisabilité

**Oui, c’est possible.** Résumé des briques :

| Brique | Faisable ? | Comment |
|--------|------------|--------|
| Flux RSS | Oui | `HttpClient` + `XmlReader` / `SyndicationFeed` (.NET) ou package type *CodeHollow.FeedReader* |
| Tuile dashboard | Oui | Nouvelle carte « Veille critère 6 » avec lien vers la page Veille |
| Liste des actualités | Oui | Stockage des items RSS (titre, lien, description, date) par flux |
| Validation de lecture | Oui | Bouton « Valider » par actualité → enregistrement d’une validation |
| Lien validation → critère | Oui | À chaque validation, on enregistre **quel** indicateur (23–29) est validé |
| Analyse fine (23 vs 26…) | Oui | Mots-clés par indicateur + scoring sur titre/description ; suggestion + choix utilisateur |

---

## Indicateurs concernés (critère 6)

| Id | Code | Libellé |
|----|------|---------|
| 23 | I23 | Veille légale et réglementaire |
| 24 | I24 | Veille des emplois et métiers |
| 25 | I25 | Veille pédagogique et technologique |
| 26 | I26 | Situation de handicap |
| 27 | I27 | Disposition sous-traitance |
| 28 | I28 | Formation en situation de travail |
| 29 | I29 | Insertion professionnelle |

---

## Analyse fine : 23 vs 26 vs … (mots-clés)

On peut **suggérer** l’indicateur le plus pertinent pour une actualité en analysant **titre + description** avec des **mots-clés par indicateur** (configurables plus tard). Exemples :

| Indicateur | Exemples de mots-clés (FR) |
|------------|----------------------------|
| **I23** – Veille légale et réglementaire | loi, décret, réglementation, obligation, CNEFOP, France Compétences, Code du travail, convention collective, accord |
| **I24** – Veille des emplois et métiers | métier, emploi, OPCO, certification professionnelle, référentiel, branches, orientations |
| **I25** – Veille pédagogique et technologique | pédagogie, formation, digital, numérique, MOOC, outil, innovation, modalités |
| **I26** – Situation de handicap | handicap, accessibilité, PCH, RQTH, inclusion, aménagement, travailleur handicapé |
| **I27** – Disposition sous-traitance | sous-traitance, prestataire, externalisation, sous-traitant |
| **I28** – Formation en situation de travail | alternance, FEST, entreprise, tutorat, terrain, situation de travail |
| **I29** – Insertion professionnelle | insertion, accompagnement, retour à l’emploi, évolution, reconversion |

**Fonctionnement :**

1. Pour chaque actualité RSS : on calcule un **score** par indicateur (nombre de mots-clés trouvés dans titre + description).
2. On **suggère** l’indicateur avec le meilleur score (ou « non déterminé » si aucun).
3. À la **validation**, l’utilisateur voit la suggestion mais **choisit lui-même** l’indicateur (liste 23–29). La validation enregistrée est donc **toujours explicite**.

On peut aussi associer **un indicateur par défaut** à chaque **flux** (ex. flux Légifrance → I23). Si la suggestion par mots-clés est absente, on pré-remplit avec ce défaut.

---

## Modèle de données proposé

### 1. `RssFeed` (flux RSS)

- `Id`, `Name`, `Url`
- `DefaultIndicateurId` (nullable) : indicateur 23–29 par défaut pour ce flux
- `SiteId` (optionnel), `IsActive`, dates de création/modif

### 2. `RssItem` (actualité mise en cache)

- `Id`, `RssFeedId`
- `ExternalId` (guid/lien de l’item dans le flux) pour éviter doublons
- `Title`, `Link`, `Description`, `PublishedUtc`, `FetchedAt`

Unicité : `(RssFeedId, ExternalId)`.

### 3. `VeilleValidation` (validation de lecture → critère)

- `Id`, `RssItemId`, `IndicateurQualiopiId` (un des 23–29)
- `ValidatedBy` (utilisateur), `ValidatedAt`, `SiteId`

Une validation = **une actualité** utilisée pour **un indicateur**. (On peut décider plus tard si une même actualité peut valider plusieurs indicateurs.)

---

## Flux fonctionnel

1. **Configuration (admin)**  
   - Création des flux RSS (nom, URL, éventuellement indicateur par défaut).  
   - Stockage en base (`RssFeed`).

2. **Rafraîchissement des flux**  
   - Job périodique ou bouton « Actualiser » sur la page Veille :  
     - pour chaque flux actif, fetch RSS, parse ;  
     - création/mise à jour des `RssItem`.

3. **Tuile dashboard**  
   - « Veille critère 6 » :  
     - nombre de flux, nombre d’actualités récentes, nombre de validations (sur une période à définir).  
   - Lien vers la **page Veille critère 6**.

4. **Page Veille critère 6**  
   - Liste des flux (avec option d’en ajouter/modifier).  
   - Liste des **actualités** (tous flux ou par flux), avec pour chacune :  
     - titre, date, lien, extrait ;  
     - **suggestion** d’indicateur (analyse fine) ;  
     - bouton **« Valider cette actualité »**.

5. **Validation**  
   - Clic « Valider » → ouverture d’un modal (ou page dédiée) :  
     - indication de l’**indicateur suggéré** (23–29) ;  
     - **liste déroulante** ou boutons pour choisir l’indicateur (obligatoire).  
   - Envoi → création d’une `VeilleValidation` (item + indicateur + user + date).  
   - Rafraîchissement de la liste (actualité marquée comme validée, etc.).

6. **Prise en compte dans l’application**  
   - Les **validations** (`VeilleValidation`) sont utilisées pour **remonter** le critère 6 dans :  
     - dashboard (ex. indicateurs 23–29 « validés par veille ») ;  
     - exports / rapports Qualiopi, en complément des preuves classiques (documents, etc.).

---

## Points à trancher

1. **Périmètre des flux**  
   - Gérés par admin seulement ?  
   - Par site (multisite) ou global ?

2. **Fréquence de rafraîchissement**  
   - Manuel uniquement (bouton « Actualiser ») au début, puis éventuellement job planifié (ex. 1×/jour).

3. **Rapport Qualiopi**  
   - Intégrer les `VeilleValidation` pour le critère 6 à part des `PreuveQualiopi` (session) ?  
   - Oui recommandé : on garde les preuves « formation » par session, et on ajoute une section « Veille C6 » basée sur les validations RSS.

4. **Une actualité = plusieurs indicateurs ?**  
   - Version simple : **1 validation = 1 actualité → 1 indicateur**.  
   - Si besoin : permettre plus tard de « valider » la même actualité pour un autre indicateur (2e validation).

---

## Plan de mise en œuvre (ordres de grandeur)

| Étape | Description | Complexité |
|-------|-------------|------------|
| 1 | Modèles `RssFeed`, `RssItem`, `VeilleValidation` + migration | Faible |
| 2 | Service de fetch RSS (fetch + parse + upsert `RssItem`) | Faible–moyenne |
| 3 | Config mots-clés par indicateur (I23–I29) + scoring | Faible |
| 4 | CRUD flux RSS (admin) + UI liste flux | Faible |
| 5 | Page « Veille critère 6 » : flux + actualités + bouton Actualiser | Moyenne |
| 6 | Modal/page validation : suggestion + choix indicateur → `VeilleValidation` | Moyenne |
| 7 | Tuile dashboard « Veille critère 6 » + lien vers la page | Faible |
| 8 | Intégration dans dashboard / rapports Qualiopi (C6) | Moyenne |

---

## Conclusion

- **Oui**, on peut avoir une **tuile dashboard** qui recense les **flux RSS** utiles au critère 6.  
- **Oui**, on peut afficher les **actualités** et permettre une **validation de lecture** par actualité.  
- **Oui**, chaque validation peut **valider explicitement un indicateur** (23–29).  
- **Oui**, une **analyse fine** (mots-clés → suggestion 23 vs 26 vs …) est possible ; l’utilisateur **confirme ou corrige** l’indicateur à la validation.

---

## Implémenté (double entrée + module finalisé)

- **Onglet « Veille »** dans le menu (après Qualiopi) → `VeilleController`, `Views/Veille/Index.cshtml`.  
- **Tuile « Veille critère 6 »** sur le **dashboard admin** : flux / actualités / validations (compteurs réels), lien « Voir la veille ».

### Module finalisé (étapes 1–7)

1. **Modèles** : `RssFeed`, `RssItem`, `VeilleValidation` + migration `AddVeilleRssModule`.  
2. **Service** : `IVeilleRssService` / `VeilleRssService` – fetch RSS (SyndicationFeed), parse, upsert `RssItem` ; chargement des flux depuis `Config/veille-rss-feeds.json` ; scoring par mots-clés (I23–I29) ; création de `VeilleValidation`.  
3. **Config** : `Config/veille-rss-feeds.json` (flux prêts à l’emploi). Mots-clés par indicateur dans le service.  
4. **UI** : Liste des flux, **Actualiser les flux**, liste des actualités (suggestion I23–I29), **Valider** (modal → choix indicateur), liste des validations.  
5. **Dashboard** : `VeilleFluxCount`, `VeilleActualitesCount`, `VeilleValidationsCount` alimentés depuis la base.

Suite logique : étape 8 (intégration des validations dans les rapports Qualiopi critère 6).
