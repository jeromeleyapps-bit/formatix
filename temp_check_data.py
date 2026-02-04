import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== VÃ©rification des donnÃ©es dans la base ===")
cur.execute("SELECT COUNT(*) as cnt FROM Formations")
print(f"Formations: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM Sessions")
print(f"Sessions: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM Stagiaires")
print(f"Stagiaires: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM Clients")
print(f"Clients: {cur.fetchone()['cnt']}")

print("\n=== Quelques formations ===")
cur.execute("SELECT Titre, SiteId FROM Formations LIMIT 5")
for r in cur.fetchall():
    print(f"  - {r['Titre']} (Site: {r['SiteId']})")

conn.close()
