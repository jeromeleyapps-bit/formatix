import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== Ã‰tat de la base de donnÃ©es (sans donnÃ©es de dÃ©mo) ===")
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

cur.execute("SELECT COUNT(*) as cnt FROM Sites")
print(f"Sites: {cur.fetchone()['cnt']}")

print("\n=== Utilisateurs ===")
cur.execute("SELECT Email, Role, SiteId FROM AspNetUsers")
for r in cur.fetchall():
    print(f"  - {r['Email']} (Role: {r['Role']}, Site: {r['SiteId'] or 'N/A'})")

print("\n=== Sites ===")
cur.execute("SELECT SiteId, Name FROM Sites")
for r in cur.fetchall():
    print(f"  - {r['SiteId']}: {r['Name']}")

conn.close()
