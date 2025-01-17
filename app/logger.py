import logging
import sys
from pathlib import Path
from logging.handlers import RotatingFileHandler
from typing import Optional

class LoggerSetup:
    @staticmethod
    def setup_logger(
        log_file: Optional[str] = None,
        log_level: int = logging.INFO
    ) -> None:
        """
        Setup application logging with console and file handlers.

        Args:
            log_file: Path to log file. If None, only console logging is setup.
            log_level: Logging level (default: INFO)
        """
        # Create logger
        logger = logging.getLogger()
        logger.setLevel(log_level)

        # Create formatters
        console_formatter = logging.Formatter(
            '%(levelname)s: %(message)s'
        )
        file_formatter = logging.Formatter(
            '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
        )

        # Create console handler
        console_handler = logging.StreamHandler(sys.stdout)
        console_handler.setFormatter(console_formatter)
        logger.addHandler(console_handler)

        # Create file handler if log_file is specified
        if log_file:
            log_path = Path(log_file)
            log_path.parent.mkdir(parents=True, exist_ok=True)

            file_handler = RotatingFileHandler(
                log_file,
                maxBytes=10485760,  # 10MB
                backupCount=5
            )
            file_handler.setFormatter(file_formatter)
            logger.addHandler(file_handler)

        # Set logging levels for some verbose libraries
        logging.getLogger("urllib3").setLevel(logging.WARNING)
        logging.getLogger("httpx").setLevel(logging.WARNING)
        logging.getLogger("apscheduler").setLevel(logging.WARNING)

class LoggerMixin:
    """Mixin to add logger to any class."""

    @property
    def logger(self):
        if not hasattr(self, '_logger'):
            self._logger = logging.getLogger(self.__class__.__name__)
        return self._logger
