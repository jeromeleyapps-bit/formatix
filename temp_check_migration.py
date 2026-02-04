import sqlite3
conn = sqlite3.connect('opagax.db')
conn.row_factory = sqlite3.Row
cur = conn.cursor()

print("=== VÃ©rification des donnÃ©es existantes ===")
cur.execute("SELECT COUNT(*) as cnt FROM Sessions")
print(f"Sessions: {cur.fetchone()['cnt']}")

# VÃ©rifier si la table Formateurs existe
try:
    cur.execute("SELECT COUNT(*) as cnt FROM Formateurs")
    print(f"Formateurs: {cur.fetchone()['cnt']}")
except:
    print("Table Formateurs n'existe pas encore")

conn.close()
