import logging
import sqlite3
from contextlib import contextmanager
from datetime import datetime
from pathlib import Path
from threading import Lock
from typing import Any, Generator, List, Optional, Type, TypeVar
from uuid import UUID

logger = logging.getLogger(__name__)

DATABASE_FILE = "runtime/telegram_digest.db"

T = TypeVar("T")


class Database:
    VALID_TABLES = {
        "Digest": "digests",
        "PostSummary": "post_summaries",
        "Channel": "channels",
    }

    _instance: Optional["Database"] = None
    _lock = Lock()

    def __init__(self) -> None:
        self.connection_pool: List[sqlite3.Connection] = []
        self.max_connections = 5
        self._setup_database()
        self._init_schema()

    def _setup_database(self) -> None:
        """Setup database with proper permissions and schema"""
        db_path = Path(DATABASE_FILE)
        db_path.parent.mkdir(parents=True, exist_ok=True)

        # Set proper file permissions
        if db_path.exists():
            db_path.chmod(0o600)

    @contextmanager
    def transaction(self) -> Generator[sqlite3.Connection, None, None]:
        """Transaction context manager"""
        with self._get_connection() as conn:
            try:
                yield conn
                conn.commit()
            except Exception:
                conn.rollback()
                raise

    def _init_schema(self) -> None:
        """Initialize database tables."""
        with self._get_connection() as conn:
            cursor = conn.cursor()

            # Create channels table
            cursor.execute(
                """
                CREATE TABLE IF NOT EXISTS channels (
                    id TEXT PRIMARY KEY,
                    name TEXT UNIQUE NOT NULL,
                    url TEXT NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )
            """
            )

            # Create digests table
            cursor.execute(
                """
                CREATE TABLE IF NOT EXISTS digests (
                    id TEXT PRIMARY KEY,
                    channel_id TEXT NOT NULL,
                    created_date TIMESTAMP NOT NULL,
                    FOREIGN KEY (channel_id) REFERENCES channels (id)
                )
            """
            )

            # Create post_summaries table
            cursor.execute(
                """
                CREATE TABLE IF NOT EXISTS post_summaries (
                    id TEXT PRIMARY KEY,
                    digest_id TEXT NOT NULL,
                    post_id TEXT NOT NULL,
                    summary TEXT NOT NULL,
                    usefulness INTEGER NOT NULL,
                    FOREIGN KEY (digest_id) REFERENCES digests (id)
                        ON DELETE CASCADE
                )
            """
            )

            # Create settings table
            cursor.execute(
                """
                CREATE TABLE IF NOT EXISTS settings (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                )
            """
            )

            conn.commit()

    def save(self, obj: Any) -> None:
        """Save an object to the database."""
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()

                # Convert object to dictionary
                data = obj.dict()
                table_name = obj.__class__.__name__.lower() + "s"

                # Prepare SQL statement
                fields = ", ".join(data.keys())
                placeholders = ", ".join(["?" for _ in data])
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

                table_name = cls.__name__.lower() + "s"
                cursor.execute(f"SELECT * FROM {table_name} WHERE id = ?", (obj_id,))

                row = cursor.fetchone()
                if row:
                    return cls(**dict(row))
                return None

        except Exception as e:
            logger.error(f"Database retrieve error: {str(e)}")
            raise

    def _get_table_name(self, model_class: Type) -> str:
        """Safely get table name from model class"""
        if model_class.__name__ not in self.VALID_TABLES:
            raise ValueError(f"Invalid model class: {model_class.__name__}")
        return self.VALID_TABLES[model_class.__name__]

    def query(
        self,
        model_class: Type[T],
        **conditions: str | int | float | bool | UUID | datetime,
    ) -> List[T]:
        """Query database for objects of specified model class with given conditions.

        Args:
            model_class: The model class to query for
            **conditions: Field-value pairs to filter by. Supported types are:
                        str, int, float, bool, UUID, and datetime

        Returns:
            List of model instances matching the query conditions
        """
        table_name = self._get_table_name(model_class)
        query = f"SELECT * FROM {table_name} WHERE 1=1"
        params = []

        for key, value in conditions.items():
            query += f" AND {key} = ?"
            # Convert UUID and datetime to strings for SQLite
            params.append(str(value) if isinstance(value, (UUID, datetime)) else value)

        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(query, params)
            rows = cursor.fetchall()

            # Convert rows to model instances
            return [model_class(**dict(row)) for row in rows]

    def delete(self, cls: Type, obj_id: str) -> None:
        """Delete an object by ID."""
        try:
            with self._get_connection() as conn:
                cursor = conn.cursor()

                table_name = cls.__name__.lower() + "s"
                cursor.execute(f"DELETE FROM {table_name} WHERE id = ?", (obj_id,))
                conn.commit()

                logger.debug(f"Deleted {table_name[:-1]}: {obj_id}")

        except Exception as e:
            logger.error(f"Database delete error: {str(e)}")
            raise

    @contextmanager
    def _get_connection(self) -> Generator[sqlite3.Connection, None, None]:
        """Safe connection management with pooling"""
        connection = None
        try:
            connection = self._get_connection_from_pool()
            yield connection
        finally:
            if connection:
                self._return_connection_to_pool(connection)

    def _get_connection_from_pool(self) -> sqlite3.Connection:
        """Get a connection from the pool or create a new one if pool is empty"""
        with self._lock:
            if self.connection_pool:
                return self.connection_pool.pop()

            # Create new connection if pool is empty
            connection = sqlite3.connect(str(DATABASE_FILE))
            connection.row_factory = sqlite3.Row
            return connection

    def _return_connection_to_pool(self, connection: sqlite3.Connection) -> None:
        """Return a connection to the pool if space available, otherwise close it"""
        with self._lock:
            if len(self.connection_pool) < self.max_connections:
                self.connection_pool.append(connection)
            else:
                connection.close()
