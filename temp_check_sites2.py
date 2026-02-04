import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== Structure de la table Sites ===")
cur.execute("PRAGMA table_info(Sites)")
for col in cur.fetchall():
    print(f"  {col['name']} ({col['type']})")

print("\n=== DonnÃ©es des sites ===")
cur.execute("SELECT * FROM Sites")
sites = cur.fetchall()
print(f"Nombre de sites: {len(sites)}")
for s in sites:
    print(f"  Site: {dict(s)}")

conn.close()
