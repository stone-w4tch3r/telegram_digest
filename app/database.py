import sqlite3
from contextlib import contextmanager
from typing import Type, List, Any, Dict, Optional
from datetime import datetime
import json
import logging
from uuid import UUID
from pathlib import Path

logger = logging.getLogger(__name__)

class Database:
    def __init__(self, db_path: str = "telegram_digest.db"):
        self.db_path = db_path
        self._init_db()

    @contextmanager
    def _get_connection(self):
        """Context manager for database connections."""
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        try:
            yield conn
        finally:
            conn.close()

    def _init_db(self):
        """Initialize database tables."""
        with self._get_connection() as conn:
            cursor = conn.cursor()

            # Create channels table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS channels (
                    id TEXT PRIMARY KEY,
                    name TEXT UNIQUE NOT NULL,
                    url TEXT NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )
            """)

            # Create digests table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS digests (
                    id TEXT PRIMARY KEY,
                    channel_id TEXT NOT NULL,
                    created_date TIMESTAMP NOT NULL,
                    FOREIGN KEY (channel_id) REFERENCES channels (id)
                )
            """)

            # Create post_summaries table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS post_summaries (
                    id TEXT PRIMARY KEY,
                    digest_id TEXT NOT NULL,
                    post_id TEXT NOT NULL,
                    summary TEXT NOT NULL,
                    usefulness INTEGER NOT NULL,
                    FOREIGN KEY (digest_id) REFERENCES digests (id)
                )
            """)

            # Create settings table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS settings (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                )
            """)

            conn.commit()

    def save(self, obj: Any) -> None:
        """Save an object to the database."""
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()

                # Convert object to dictionary
                data = obj.dict()
                table_name = obj.__class__.__name__.lower() + 's'

                # Prepare SQL statement
                fields = ', '.join(data.keys())
                placeholders = ', '.join(['?' for _ in data])
                values = tuple(
                    str(v) if isinstance(v, (UUID, datetime)) else v 
                    for v in data.values()
                )

                sql = f"""
                    INSERT OR REPLACE INTO {table_name} ({fields})
                    VALUES ({placeholders})
                """

                cursor.execute(sql, values)
                conn.commit()

                logger.debug(f"Saved {table_name[:-1]}: {obj.id}")

        except Exception as e:
            logger.error(f"Database save error: {str(e)}")
            raise

    def retrieve(self, cls: Type, obj_id: str) -> Optional[Any]:
        """Retrieve an object by ID."""
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()

                table_name = cls.__name__.lower() + 's'
                cursor.execute(
                    f"SELECT * FROM {table_name} WHERE id = ?",
                    (obj_id,)
                )

                row = cursor.fetchone()
                if row:
                    return cls(**dict(row))
                return None

        except Exception as e:
            logger.error(f"Database retrieve error: {str(e)}")
            raise

    def query(self, cls: Type, **kwargs) -> List[Any]:
        """Query objects with filters."""
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()

                table_name = cls.__name__.lower() + 's'

                # Build WHERE clause
                where_clauses = []
                values = []
                for key, value in kwargs.items():
                    if '_gte' in key:
                        field = key.replace('_gte', '')
                        where_clauses.append(f"{field} >= ?")
                    elif '_lte' in key:
                        field = key.replace('_lte', '')
                        where_clauses.append(f"{field} <= ?")
                    else:
                        where_clauses.append(f"{key} = ?")
                    values.append(str(value) if isinstance(value, (UUID, datetime)) else value)

                where_sql = " AND ".join(where_clauses) if where_clauses else "1=1"

                sql = f"SELECT * FROM {table_name} WHERE {where_sql}"
                cursor.execute(sql, tuple(values))

                return [cls(**dict(row)) for row in cursor.fetchall()]

        except Exception as e:
            logger.error(f"Database query error: {str(e)}")
            raise

    def delete(self, cls: Type, obj_id: str) -> None:
        """Delete an object by ID."""
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()

                table_name = cls.__name__.lower() + 's'
                cursor.execute(
                    f"DELETE FROM {table_name} WHERE id = ?",
                    (obj_id,)
                )
                conn.commit()

                logger.debug(f"Deleted {table_name[:-1]}: {obj_id}")

        except Exception as e:
            logger.error(f"Database delete error: {str(e)}")
            raise
