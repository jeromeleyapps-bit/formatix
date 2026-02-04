import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== VÃ©rification des donnÃ©es par site ===")
for site_id in ['SITE_01', 'SITE_02', 'SITE_03', 'SITE_04', 'SITE_05', 'SITE_06']:
    print(f"\n{site_id}:")
    cur.execute("SELECT COUNT(*) as cnt FROM Formations WHERE SiteId = ?", (site_id,))
    print(f"  Formations: {cur.fetchone()['cnt']}")
    cur.execute("SELECT COUNT(*) as cnt FROM Sessions WHERE SiteId = ?", (site_id,))
    print(f"  Sessions: {cur.fetchone()['cnt']}")
    cur.execute("SELECT COUNT(*) as cnt FROM Clients WHERE SiteId = ?", (site_id,))
    print(f"  Clients: {cur.fetchone()['cnt']}")
    cur.execute("SELECT COUNT(*) as cnt FROM Stagiaires WHERE SiteId = ?", (site_id,))
    print(f"  Stagiaires: {cur.fetchone()['cnt']}")
    cur.execute("SELECT COUNT(*) as cnt FROM Documents WHERE SiteId = ?", (site_id,))
    print(f"  Documents: {cur.fetchone()['cnt']}")
    cur.execute("SELECT COUNT(*) as cnt FROM PreuvesQualiopi WHERE SiteId = ?", (site_id,))
    print(f"  Preuves: {cur.fetchone()['cnt']}")
    cur.execute("SELECT COUNT(*) as cnt FROM Utilisateurs WHERE SiteId = ?", (site_id,))
    print(f"  Utilisateurs: {cur.fetchone()['cnt']}")

conn.close()
