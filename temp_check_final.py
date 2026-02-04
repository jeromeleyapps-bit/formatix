import sqlite3
import sys
try:
    conn = sqlite3.connect('opagax.db')
    conn.row_factory = sqlite3.Row
    cur = conn.cursor()

    print("=== Ã‰tat de la base de donnÃ©es aprÃ¨s rebuild ===")
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

    print("\n=== Utilisateurs ===")
    cur.execute("SELECT Email, Role, SiteId FROM AspNetUsers")
    for r in cur.fetchall():
        print(f"  - {r['Email']} (Role: {r['Role']}, Site: {r['SiteId'] or 'N/A'})")

    conn.close()
    print("\nâœ… Base de donnÃ©es crÃ©Ã©e SANS donnÃ©es de dÃ©monstration")
except Exception as e:
    print(f"âŒ Erreur: {e}")
    sys.exit(1)
