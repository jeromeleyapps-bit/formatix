import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== VÃ©rification des sites ===")
cur.execute("SELECT SiteId, Name, Actif FROM Sites")
sites = cur.fetchall()
print(f"Nombre de sites: {len(sites)}")
for s in sites:
    print(f"  - {s['SiteId']}: {s['Name']} (Actif: {s['Actif']})")

print("\n=== Test des requÃªtes du dashboard ===")
# Test formations par site
cur.execute("SELECT SiteId, COUNT(*) as cnt FROM Formations GROUP BY SiteId")
formations = {r['SiteId']: r['cnt'] for r in cur.fetchall()}
print(f"Formations par site: {formations}")

# Test sessions par site  
cur.execute("SELECT SiteId, COUNT(*) as cnt FROM Sessions GROUP BY SiteId")
sessions = {r['SiteId']: r['cnt'] for r in cur.fetchall()}
print(f"Sessions par site: {sessions}")

conn.close()
