import json
from pathlib import Path
from typing import Optional
import logging
from pydantic import ValidationError

from .models import Settings

logger = logging.getLogger(__name__)

class SettingsManager:
    def __init__(self, settings_file: str = "settings.json"):
        self.settings_file = settings_file
        self.settings_path = Path(settings_file)

    def load_settings(self) -> Settings:
        """
        Load settings from JSON file.
        If file doesn't exist or is invalid, return default settings.
        """
        try:
            if self.settings_path.exists():
                with open(self.settings_path, 'r') as f:
                    data = json.load(f)
                    return Settings(**data)
            else:
                logger.warning(
                    f"Settings file not found: {self.settings_file}"
                )
                return self._create_default_settings()

        except ValidationError as e:
            logger.error(f"Invalid settings format: {str(e)}")
            return self._create_default_settings()
        except Exception as e:
            logger.error(f"Failed to load settings: {str(e)}")
            return self._create_default_settings()

    def save_settings(self, settings: Settings) -> None:
        """Save settings to JSON file."""
        try:
            # Create directory if it doesn't exist
            self.settings_path.parent.mkdir(parents=True, exist_ok=True)

            # Save settings
            with open(self.settings_path, 'w') as f:
                json.dump(settings.dict(), f, indent=4)

            logger.info("Settings saved successfully")

        except Exception as e:
            logger.error(f"Failed to save settings: {str(e)}")
            raise

    def _create_default_settings(self) -> Settings:
        """Create default settings."""
        settings = Settings(
            openai_api_key="",
            email_from="",
            email_to="",
            email_password="",
            email_server="smtp.gmail.com",
            email_port=587,
            digest_schedule_hour=8,
            digest_schedule_minute=0
        )

        try:
            self.save_settings(settings)
        except Exception:
            pass

        return settings

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
