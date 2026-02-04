import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== VÃ©rification des donnÃ©es pour le dashboard ===")
print("\n1. Formations par site:")
cur.execute("SELECT SiteId, COUNT(*) as cnt FROM Formations GROUP BY SiteId")
for r in cur.fetchall():
    print(f"   Site {r['SiteId']}: {r['cnt']} formations")

print("\n2. Sessions par site:")
cur.execute("SELECT SiteId, COUNT(*) as cnt FROM Sessions GROUP BY SiteId")
for r in cur.fetchall():
    print(f"   Site {r['SiteId']}: {r['cnt']} sessions")

print("\n3. Documents par site:")
cur.execute("SELECT SiteId, COUNT(*) as cnt FROM Documents GROUP BY SiteId")
for r in cur.fetchall():
    print(f"   Site {r['SiteId']}: {r['cnt']} documents")

print("\n4. Sessions sans documents:")
cur.execute("""
    SELECT s.Id, s.SiteId 
    FROM Sessions s 
    LEFT JOIN Documents d ON d.SessionId = s.Id 
    WHERE d.Id IS NULL
""")
sessions_sans_docs = cur.fetchall()
print(f"   Total: {len(sessions_sans_docs)} sessions sans documents")
for s in sessions_sans_docs[:5]:
    print(f"   - Session {s['Id']} (Site: {s['SiteId']})")

conn.close()
