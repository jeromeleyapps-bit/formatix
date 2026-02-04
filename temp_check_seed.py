import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== VÃ©rification des donnÃ©es de seed ===")
cur.execute("SELECT COUNT(*) as cnt FROM AspNetUsers")
print(f"Utilisateurs: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM Formations")
print(f"Formations: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM IndicateursQualiopi")
print(f"Indicateurs Qualiopi: {cur.fetchone()['cnt']}")

cur.execute("SELECT COUNT(*) as cnt FROM Sites")
print(f"Sites: {cur.fetchone()['cnt']}")

cur.execute("SELECT Email, Role FROM AspNetUsers LIMIT 5")
print("\nUtilisateurs crÃ©Ã©s:")
for r in cur.fetchall():
    print(f"  - {r['Email']} ({r['Role']})")

conn.close()
