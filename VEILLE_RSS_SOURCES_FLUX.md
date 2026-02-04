# Recensement des flux RSS – Veille critère 6 (indicateurs 23–29)

Ce document recense les **sources fiables et pertinentes** pour alimenter l’onglet Veille et le dashboard : veille réglementaire, emplois/métiers, pédagogie, handicap, insertion, formation en situation de travail. Chaque source est alignée, quand c’est pertinent, avec un **indicateur par défaut** (I23–I29).

---

## 1. Flux avec URL RSS/Atom vérifiée ou bien documentée

| Source | URL du flux | Indicateur suggéré | Commentaire |
|--------|-------------|--------------------|-------------|
| **Service-public (pro)** | `https://www.service-public.fr/abonnements/rss/actu-actu-pro.rss` | I23 | Actualités droits des pros, travail, formation, réglementation. |
| **Service-public (particuliers)** | `https://www.service-public.fr/abonnements/rss/actu-actualites-particuliers.rss` | I23 | Dernières actus particuliers (dont formation, emploi). |
| **data.gouv.fr (datasets récents)** | `https://www.data.gouv.fr/fr/datasets/recent.atom` | I23 / I24 | Nouveaux jeux de données (formation, emploi, etc.). Format Atom. |
| **France Travail – offres d’emploi (RSS)** | `https://recrute.francetravail.org/offre-de-emploi/tous-les-flux-rss.aspx` | I24 / I29 | Page listant les flux RSS par région, métier, etc. À utiliser selon besoin (veille emploi, insertion). |

**Usage** : ces URLs peuvent être utilisées telles quelles pour configurer les flux dans l’appli (table `RssFeed`, etc.) dès que le module veille RSS sera implémenté.

---

## 1b. Formateurs, experts Qualiopi, législation (sites / blogs) – avec flux RSS

| Source | URL du flux | Indicateur suggéré | Commentaire |
|--------|-------------|--------------------|-------------|
| **Centre Inffo – Le Quotidien de la formation** | `https://www.centre-inffo.fr/category/site-centre-inffo/actualites-centre-inffo/le-quotidien-de-la-formation-actualite-formation-professionnelle-apprentissage/feed` | I23 / I24 / I25 | Quotidien formation pro et apprentissage. Référent sectoriel. |
| **Centre Inffo – Droit** | `https://www.centre-inffo.fr/category/site-droit-formation/actualites-droit/feed` | I23 | Veille juridique formation. |
| **Centre Inffo – Réforme** | `https://www.centre-inffo.fr/category/site-reforme/feed` | I23 | Réformes formation, réglementation. |
| **Centre Inffo – Innovation** | `https://www.centre-inffo.fr/category/innovation-formation/feed` | I25 | Innovation pédagogique et technologique. |
| **Centre Inffo – Régions** | `https://www.centre-inffo.fr/category/actualites-regions/feed` | I24 / I29 | Actualités régionales emploi, formation, insertion. |
| **Mon Activité Formation (MAF)** | `https://info.monactiviteformation.emploi.gouv.fr/actualites/rss/` | I23 | Déclarations d’activité, Qualiopi, BPF, France Compétences. Pro des OF. Si 404 : tester `/actualit%C3%A9s/rss/` ou la page Actualités pour un lien feed. |
| **L’Atelier du formateur** | `https://latelierduformateur.fr/feed` | I25 | Digital learning, IA, pédagogie, outils. Blog formateurs. |

Tous les flux Centre Inffo : https://www.centre-inffo.fr/centre-inffo/nos-flux-rss (Europe & international, Vidéos, etc.).

---

## 2. Flux paramétrables (génération d’URL)

| Source | Page de configuration | Indicateur suggéré | Commentaire |
|--------|------------------------|--------------------|-------------|
| **Bulletins officiels (Travail, Emploi, Formation pro)** | https://bulletins-officiels.social.gouv.fr/flux-rss | I23 | Rubrique « BO Travail – Emploi – Formation professionnelle » ; thématiques : Formation professionnelle, Emploi Insertion, Personnes handicap, etc. Génération d’URL de flux via le formulaire. |
| **DARES (Formation pro, Emploi, Métiers…)** | https://dares.travail-emploi.gouv.fr/test-flux-rss | I23 / I24 / I29 | Filtres par thème (Formation professionnelle, Emploi, Métiers, Chômage…) et par type (Publication, Actualité, Données). Vérifier sur la page si un lien RSS/Atom est proposé (balise `<link rel="alternate" …>`) pour l’URL filtrée. |

**Usage** : générer une ou plusieurs URLs ciblées (ex. BO Formation pro uniquement, DARES Formation pro) puis les enregistrer comme flux dans l’appli.

---

## 3. Sources pertinentes sans flux RSS identifié

| Source | URL / contact | Indicateur(s) | Commentaire |
|--------|----------------|---------------|-------------|
| **France Compétences** | https://www.francecompetences.fr/actualites/ | I23 / I24 | Actualités certification, financement, gouvernance. Newsletter uniquement ; pas de RSS documenté. |
| **ActuFormation (France Travail)** | https://actuformation.francetravail.org/les-actualites/ | I24 / I25 | Actualités à destination des organismes de formation. Pas de RSS trouvé. |
| **CEREQ** | https://www.cereq.fr – servicepresse@cereq.fr | I24 | Études, qualifications, emploi. Newsletter / pas de RSS. |
| **Vie publique** | https://www.vie-publique.fr/emploi-travail, /formation | I23 / I24 | Fiches, dossiers, actualités. Pas de flux RSS indiqué. |
| **Agefiph / FIPHFP** | agefiph.fr, fiphfp.fr | I26 / I29 | Handicap, insertion pro. Pas de RSS identifié. |
| **Ministère du Travail (actualités)** | https://travail-emploi.gouv.fr/actualites-presse-et-outils/actualites-et-breves | I23 | Filtres par thème (formation, etc.). Pas de RSS dédié trouvé. |

**Usage** : surveillance manuelle ou, à terme, scrapers dédiés / partenariats. Pour l’heure, **ne pas** les ajouter comme flux RSS dans l’appli tant qu’aucun flux n’est fourni ou détecté.

---

## 3b. Formateurs, experts Qualiopi, législation – sans flux RSS (mais utiles pour veille)

| Source | URL | Indicateur(s) | Commentaire |
|--------|-----|---------------|-------------|
| **Le Blog de la Formation** | https://leblogdelaformation.fr/ | I25 | Pédagogie, ingénierie, marketing formateurs. Vérifier `/feed` si thème le permet. |
| **Argalis** | https://argalis.fr/blog/ | I23 / I25 | Qualiopi, tarification, conformité, neurosciences. |
| **Double Voie** | https://www.doublevoie.fr/le-blog-de-double-voie/ | I25 | Pédagogie active, digital learning, Qualiopi. |
| **Certifopac (Qualiopi)** | https://certifopac.fr/qualiopi/actualites/ | I23 | Actualités Qualiopi, audits, fraude. Certificateur accrédité. |
| **Digi-Certif** | https://www.digi-certif.com/actualites/ | I23 / I24 / I25 | Veille Qualiopi, CPF, réformes. Newsletter hebdo. |
| **FormaPro** | https://www.formapro.com/articles | I23 / I24 / I25 | Veille légale, OPCO, Qualiopi, handicap. Compte + newsletter. |
| **VeilleFormation** | https://www.veilleformation.com/ | I23–I25 | Plateforme veille Qualiopi-compatible, 300+ sources. Abonnement. |
| **Formations-Conseils** | https://www.formations-conseils.com/ | I23 / I24 | Certification RS, France Compétences. |
| **EduSign (blog)** | https://edusign.com/blog/ | I25 | Formation, signature, émargement. |

Consulter ces sites régulièrement ou s’abonner à leurs newsletters pour une veille actualisée. Dès qu’un flux RSS est repéré (ex. `/feed`), l’ajouter au recensement et à `Config/veille-rss-feeds.json`.

---

## 4. Mapping indicateur → types de contenus

| Indicateur | Types de contenus à prioriser |
|------------|-------------------------------|
| **I23** – Veille légale et réglementaire | BO, décrets, circulaires, Service-public, **Centre Inffo Droit/Réforme**, **MAF**, France Compétences, travail-emploi.gouv.fr, **Certifopac, Digi-Certif, Argalis** |
| **I24** – Veille des emplois et métiers | DARES, France Travail, CEREQ, OPCO, certifications, branches, **Centre Inffo Quotidien/Régions**, **FormaPro** |
| **I25** – Veille pédagogique et technologique | **Centre Inffo Innovation**, **L’Atelier du formateur**, Le Blog de la Formation, Double Voie, FormaPro, EduSign, digital learning, outils |
| **I26** – Situation de handicap | Agefiph, FIPHFP, accessibilité, RQTH, aménagements, BO “handicap”, FormaPro (thème handicap) |
| **I27** – Disposition sous-traitance | Sous-traitance, externalisation, marchés publics (selon périmètre) |
| **I28** – Formation en situation de travail | Alternance, FEST, apprentissage, entreprise, tutorat, Centre Inffo Quotidien |
| **I29** – Insertion professionnelle | Emploi, accompagnement, France Travail, politiques d’insertion, DARES, **Centre Inffo Régions** |

---

## 5. Fichier de configuration des flux (à utiliser au seed / config)

Le fichier **`Config/veille-rss-feeds.json`** liste des flux **prêts à l’emploi** (URL confirmées). Il pourra servir de base pour un seed `RssFeed` ou pour charger les flux au démarrage de l’appli.

Contenu actuel : Service-public, data.gouv.fr, **Centre Inffo** (Quotidien, Droit, Réforme, Innovation, Régions), **Mon Activité Formation**, **L’Atelier du formateur**. France Travail, Bulletins officiels et DARES en commentaire ; ajouter les URLs une fois générées.

---

## 6. Synthèse

- **Avec flux RSS/Atom utilisables** : Service-public (pro + particuliers), data.gouv.fr (recent), **Centre Inffo** (Quotidien, Droit, Réforme, Innovation, Régions, etc.), **Mon Activité Formation**, **L’Atelier du formateur**, France Travail (flux offres). **À implémenter en priorité** dans l’onglet Veille et le dashboard pour une veille **actualisée** (formateurs, experts Qualiopi, législation).
- **Avec flux paramétrables** : Bulletins officiels (BO Travail/Emploi/Formation), DARES (formation pro, emploi, etc.). **À configurer** une fois les URLs générées.
- **Sans RSS** : France Compétences, ActuFormation, CEREQ, Vie publique, Agefiph/FIPHFP, travail-emploi.gouv.fr ; **formateurs / experts Qualiopi** : Le Blog de la Formation, Argalis, Double Voie, Certifopac, Digi-Certif, FormaPro, VeilleFormation, EduSign. À **surveiller manuellement** ou via newsletter ; ajouter en flux dès qu’un RSS est disponible.

Ce recensement pourra être complété (nouvelles sources, nouvelles URLs, indicateurs par défaut) au fur et à mesure de la veille et des évolutions des sites.
