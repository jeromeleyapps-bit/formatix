import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== VÃ©rification des sites ===")
cur.execute("SELECT SiteId, Name, IsActive FROM Sites")
sites = cur.fetchall()
print(f"Nombre de sites: {len(sites)}")
for site in sites:
    print(f"  - {site['Name']} (ID: {site['SiteId']}, Actif: {site['IsActive']})")

conn.close()
