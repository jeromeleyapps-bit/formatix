# Accès à distance pour l'inscription en ligne

Le **lien d'inscription** et le **QR code** générés par FormatiX pointent par défaut vers l’URL du serveur (souvent `http://localhost:5000`). Les personnes à distance ne peuvent pas y accéder. Ce document décrit les **solutions gratuites** pour rendre l’inscription accessible via internet, puis comment **configurer FormatiX** pour utiliser l’URL publique.

---

## 1. Configurer FormatiX : l’URL publique

FormatiX utilise une **URL de base** pour générer le lien d’inscription et le contenu du QR code.

- Dans `appsettings.json`, section `AppSettings`, ajoutez ou modifiez :

```json
"AppSettings": {
  "BaseUrlInscription": "https://votre-url-publique",
  ...
}
```

- **Si `BaseUrlInscription` est vide** : l’app utilise l’URL de la requête (`Scheme` + `Host`), donc localhost en dev.
- **Si `BaseUrlInscription` est renseigné** : c’est cette URL (sans slash final) qui est utilisée pour le lien et le QR.

**Exemples :**

- Tunnel Cloudflare : `"BaseUrlInscription": "https://xyz-abc-123.trycloudflare.com"`
- Tunnel ngrok : `"BaseUrlInscription": "https://abc123.ngrok-free.app"`
- Production : `"BaseUrlInscription": "https://formatix.mondomaine.fr"`

Après modification, redémarrez l’application.

---

## 2. Solutions gratuites pour exposer localhost

Voici des **outils gratuits** qui créent un tunnel entre votre machine et internet. Une fois le tunnel actif, vous récupérez une URL publique (ex. `https://xxx.trycloudflare.com`) à mettre dans `BaseUrlInscription`.

### 2.1 Cloudflare Quick Tunnels (TryCloudflare) — recommandé

- **Gratuit**, sans compte.
- **Une commande** : génération d’une URL publique instantanée.
- HTTPS, limitation ~200 requêtes simultanées (largement suffisant pour des inscriptions).

**Installation (Windows) :**

```powershell
winget install --id Cloudflare.cloudflared
```

Fermez puis rouvrez le terminal. Vérifiez :

```powershell
cloudflared --version
```

**Utilisation :**

1. Démarrez FormatiX (`dotnet run`). Par défaut, l’app écoute sur `http://localhost:5000` (ou le port configuré).
2. Dans un **autre** terminal :

```powershell
cloudflared tunnel --url http://localhost:5000
```

3. Cloudflare affiche une URL du type :
   ```
   Your quick Tunnel has been created! Visit it at:
   https://xyz-abc-123.trycloudflare.com
   ```
4. Copiez cette URL, mettez-la dans `AppSettings:BaseUrlInscription` (ex. `https://xyz-abc-123.trycloudflare.com`), redémarrez FormatiX.
5. Partagez le **lien d’inscription** ou le **QR code** (Sessions > Détails) ; ils pointeront vers cette URL.

**Note :** Si vous utilisez déjà un tunnel Cloudflare avec un `config.yaml` dans `~/.cloudflared`, les quick tunnels peuvent être désactivés. Renommez temporairement ce fichier si besoin.

---

### 2.2 LocalTunnel

- **Gratuit**, pas d’installation (via `npx`).
- Sous-domaine **aléatoire** en gratuit.

**Prérequis :** Node.js et npm installés.

**Utilisation :**

```bash
# FormatiX sur le port 5000
npx localtunnel --port 5000
```

Vous obtenez une URL du type `https://xxx.loca.lt`. Utilisez-la dans `BaseUrlInscription`.

---

### 2.3 ngrok (offre gratuite)

- **Gratuit** avec limites (sous-domaine aléatoire, etc.).
- Très répandu, simple à utiliser.

**Installation :** [https://ngrok.com/download](https://ngrok.com/download) ou `winget install ngrok`.

**Utilisation :**

```bash
ngrok http 5000
```

Utilisez l’URL HTTPS affichée (ex. `https://abc123.ngrok-free.app`) dans `BaseUrlInscription`.

---

### 2.4 LocalXpose

- **Gratuit**, tunnel sécurisé.
- Multiplateforme (Windows, macOS, Linux, Docker).

**Installation :** [https://localxpose.io](https://localxpose.io) — télécharger le binaire ou utiliser Docker.

**Utilisation :** Selon la doc du site, exposer le port où tourne FormatiX (ex. 5000), puis utiliser l’URL générée dans `BaseUrlInscription`.

---

## 3. Comparatif rapide

| Solution           | Compte requis | Installation      | URL fixe gratuite | Recommandation      |
|--------------------|---------------|-------------------|-------------------|---------------------|
| **Cloudflare**     | Non           | `winget` / manuel | Non (aléatoire)   | Oui, très simple    |
| **LocalTunnel**    | Non           | Aucune (npx)      | Non               | Oui si Node installé|
| **ngrok**          | Oui (gratuit) | Téléchargement    | Non (gratuit)     | Bonne alternative   |
| **LocalXpose**     | Selon usage   | Téléchargement    | À voir            | Possible            |

Pour **tester rapidement** ou **faire des formations** avec inscription à distance : **Cloudflare Quick Tunnels** ou **LocalTunnel** sont les plus pratiques.

---

## 4. Workflow recommandé (ex. Cloudflare)

1. **Installation** : `winget install --id Cloudflare.cloudflared`
2. **Lancer FormatiX** : `dotnet run` (port 5000 par défaut).
3. **Lancer le tunnel** : `cloudflared tunnel --url http://localhost:5000`
4. **Configurer** : copier l’URL `https://....trycloudflare.com` dans `AppSettings:BaseUrlInscription`, redémarrer FormatiX.
5. **Partager** : depuis Sessions > Détails d’une session ouverte aux inscriptions, copier le **lien d’inscription** ou utiliser le **QR code**. Les candidats à distance peuvent s’inscrire via cette URL.

---

## 5. En production

En **hébergement réel** (VPS, mutualisé, etc.) :

- L’app est déjà accessible via une URL publique (ex. `https://formatix.mondomaine.fr`).
- Renseignez cette URL dans `BaseUrlInscription`.
- Aucun tunnel n’est nécessaire.

Les tunnels (Cloudflare, ngrok, etc.) sont utiles pour le **développement** et les **tests** ou quand FormatiX tourne sur une machine sans IP publique (ex. ordinateur de formation).
