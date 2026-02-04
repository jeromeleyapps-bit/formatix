import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== VÃ©rification des indicateurs Qualiopi par site ===")
for site_id in ['SITE_01', 'SITE_02', 'SITE_03', 'SITE_04', 'SITE_05', 'SITE_06']:
    cur.execute("SELECT COUNT(*) as cnt FROM IndicateursQualiopi WHERE SiteId = ?", (site_id,))
    count = cur.fetchone()['cnt']
    if count > 0:
        print(f"{site_id}: {count} indicateurs Qualiopi")

print("\n=== Total indicateurs par site ===")
cur.execute("SELECT SiteId, COUNT(*) as cnt FROM IndicateursQualiopi GROUP BY SiteId")
for r in cur.fetchall():
    print(f"  {r['SiteId']}: {r['cnt']} indicateurs")

conn.close()
