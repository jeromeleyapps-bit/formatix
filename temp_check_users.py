import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== VÃ©rification des utilisateurs par site ===")
for site_id in ['SITE_01', 'SITE_02', 'SITE_03', 'SITE_04', 'SITE_05', 'SITE_06']:
    cur.execute("SELECT Email, Role FROM AspNetUsers WHERE SiteId = ?", (site_id,))
    users = cur.fetchall()
    if users:
        print(f"\n{site_id}:")
        for u in users:
            print(f"  - {u['Email']} (Role: {u['Role']})")

conn.close()
