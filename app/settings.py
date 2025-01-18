import json
import logging
from pathlib import Path

from .models import Settings

SETTINGS_FILE = "runtime/settings.json"

logger = logging.getLogger(__name__)


class SettingsManager:
    def __init__(self, settings_file: str = SETTINGS_FILE):
        self.settings_file = settings_file
        self.settings_path = Path(settings_file)

    def load_settings(self) -> Settings:
        """
        Load settings from JSON file.
        Raises FileNotFoundError if settings file doesn't exist.
        Raises ValidationError if settings are invalid.
        """
        try:
            if not self.settings_path.exists():
                raise FileNotFoundError(
                    f"Settings file not found: {self.settings_file}"
                )

            with open(self.settings_path, "r") as f:
                data = json.load(f)
                return Settings(**data)

        except json.JSONDecodeError as e:
            logger.error(f"Invalid JSON in settings file: {str(e)}")
            raise
        except Exception as e:
            logger.error(f"Failed to load settings: {str(e)}")
            raise

    def save_settings(self, settings: Settings) -> None:
        """Save settings to JSON file."""
        try:
            # Create directory if it doesn't exist
            self.settings_path.parent.mkdir(parents=True, exist_ok=True)

            # Save settings
            with open(self.settings_path, "w") as f:
                json.dump(settings.dict(), f, indent=4)

            logger.info("Settings saved successfully")

        except Exception as e:
            logger.error(f"Failed to save settings: {str(e)}")
            raise

    def update_settings(self, **kwargs) -> Settings:
        """
        Update specific settings values.
        Returns updated settings object.
        """
        try:
            current_settings = self.load_settings()
            updated_data = current_settings.dict()
            updated_data.update(kwargs)

            new_settings = Settings(**updated_data)
            self.save_settings(new_settings)

            logger.info("Settings updated successfully")
            return new_settings

        except Exception as e:
            logger.error(f"Failed to update settings: {str(e)}")
            raise
