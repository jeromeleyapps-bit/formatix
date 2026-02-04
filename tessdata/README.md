# Dossier Tesseract Data

## Instructions

Ce dossier doit contenir les fichiers `.traineddata` pour Tesseract OCR.

### Fichiers Requis

1. **fra.traineddata** - Français (recommandé)
2. **eng.traineddata** - Anglais (optionnel)

### Téléchargement

Les fichiers peuvent être téléchargés depuis :
- **GitHub Tesseract** : https://github.com/tesseract-ocr/tessdata
- **Direct** : https://github.com/tesseract-ocr/tessdata/blob/main/fra.traineddata

### Installation Rapide

```bash
# Dans ce dossier (tessdata/)
curl -O https://github.com/tesseract-ocr/tessdata/raw/main/fra.traineddata
curl -O https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
```

### Note

L'application FormatiX utilisera automatiquement ces fichiers une fois placés ici.
Sans ces fichiers, l'OCR ne fonctionnera pas.

---

**FormatiX** - OCR avec Tesseract