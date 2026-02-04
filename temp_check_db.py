import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== Ã‰tat actuel de la base de donnÃ©es ===")
cur.execute("SELECT COUNT(*) as cnt FROM AspNetUsers")
print(f"Utilisateurs: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM Formations")
print(f"Formations: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM Sessions")
print(f"Sessions: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM Clients")
print(f"Clients: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM Stagiaires")
print(f"Stagiaires: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM IndicateursQualiopi")
print(f"Indicateurs Qualiopi: {cur.fetchone()['cnt']}")

print("\n=== Utilisateurs ===")
cur.execute("SELECT Email, Role, SiteId FROM AspNetUsers")
for r in cur.fetchall():
    print(f"  - {r['Email']} (Role: {r['Role']}, Site: {r['SiteId'] or 'N/A'})")

print("\n=== Formations ===")
cur.execute("SELECT Titre, SiteId FROM Formations LIMIT 5")
for r in cur.fetchall():
    print(f"  - {r['Titre']} (Site: {r['SiteId']})")

conn.close()
