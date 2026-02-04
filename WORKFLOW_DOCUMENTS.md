# Workflow d'Import de Documents avec OCR

## Vue d'ensemble

L'import de documents PDF dans FormatiX déclenche un processus automatique en plusieurs étapes pour extraire, analyser et lier les informations.

## Étapes du Processus

### 1. **Upload du Document**
- Le document PDF est sauvegardé dans `wwwroot/uploads/`
- Une entrée est créée dans la table `Documents` avec :
  - Type de document (Convention, Emargement, Evaluation, etc.)
  - Chemin du fichier
  - Statut : "En attente"

### 2. **Extraction OCR (Optical Character Recognition)**
- **Tesseract OCR** extrait le texte du PDF
- Le texte extrait est stocké dans `Documents.Donnees` (format JSON)
- **Problème actuel** : L'OCR retourne 0 caractères car ImageMagick ne génère pas correctement les images depuis le PDF

### 3. **Analyse IA (si OCR réussi)**
- **Ollama** analyse le texte OCR extrait
- Identifie automatiquement :
  - Le type de document (convention, feuille d'émargement, etc.)
  - Les critères Qualiopi correspondants
  - Les informations clés (dates, noms, formations)
- **Actuellement bloqué** : Pas d'analyse car OCR retourne 0 caractères

### 4. **Liaison Automatique à une Session**
- Recherche automatique d'une session correspondante en analysant :
  - Le titre de la formation dans le texte OCR
  - Les dates présentes dans le document
- Si une session unique correspond, le document est automatiquement lié
- **Actuellement bloqué** : Pas de linking car pas de texte OCR

### 5. **Création Automatique de Preuves Qualiopi**
- Pour chaque critère Qualiopi identifié par l'IA :
  - Création d'une `PreuveQualiopi`
  - Liaison à la session trouvée
  - Référence au document uploadé
- Les preuves apparaissent dans la page Qualiopi de la session
- **Actuellement bloqué** : Pas de preuves car pas d'analyse IA

## Résultat Attendu

Après un upload réussi :
1. ✅ Document visible dans la liste des documents
2. ✅ Texte OCR extrait et stocké
3. ✅ Document automatiquement lié à une session (si correspondance trouvée)
4. ✅ Preuves Qualiopi créées automatiquement dans la page Qualiopi de la session
5. ✅ Indicateurs Qualiopi mis à jour avec les nouvelles preuves

## Problème Actuel

**Symptôme** : Le document est uploadé et apparaît dans la liste, mais :
- ❌ Aucun texte n'est extrait (OCR = 0 caractères)
- ❌ Pas de liaison automatique à une session
- ❌ Pas de preuves Qualiopi créées

**Cause** : ImageMagick ne génère pas correctement les images depuis le PDF, donc Tesseract ne peut pas extraire de texte.

**Solution en cours** : Correction de la commande ImageMagick pour générer correctement les images PNG depuis le PDF.

## Utilisation

1. Aller dans **Documents** → **Importer un document**
2. Sélectionner le type de document (Convention, Emargement, etc.)
3. Optionnellement, sélectionner une session manuellement
4. Uploader le PDF
5. Le système traite automatiquement le document et crée les preuves Qualiopi si une session correspond

## Vérification

Pour vérifier si le processus fonctionne :
1. Ouvrir un document uploadé → Vérifier que le texte OCR est présent
2. Vérifier que le document est lié à une session
3. Aller dans **Qualiopi** → Vérifier que les preuves ont été créées automatiquement
