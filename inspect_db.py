import sqlite3

def inspect():
    conn = sqlite3.connect('western.db')
    cur = conn.cursor()
    
    # List tables
    cur.execute("SELECT name FROM sqlite_master WHERE type='table';")
    tables = [t[0] for t in cur.fetchall()]
    print("Tables:", tables)
    
    for table in tables:
        cur.execute(f"SELECT COUNT(*) FROM {table};")
        count = cur.fetchone()[0]
        print(f"\n======================================")
        print(f"Table {table}: {count} records")
        print(f"======================================")
        
        # Get column names
        cur.execute(f"PRAGMA table_info({table});")
        columns = [col[1] for col in cur.fetchall()]
        print("Columns:", columns)
        
        if count > 0:
            cur.execute(f"SELECT * FROM {table} LIMIT 10;")
            rows = cur.fetchall()
            print(f"Sample rows (truncated strings > 100 chars):")
            for r in rows:
                truncated_row = []
                for val in r:
                    if isinstance(val, str) and len(val) > 100:
                        truncated_row.append(val[:100] + "... [TRUNCATED]")
                    else:
                        truncated_row.append(val)
                print(f"  {truncated_row}")
                
inspect()
