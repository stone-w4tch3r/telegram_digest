from typing import List, Optional
from uuid import UUID
import json
import logging
from .models import Channel
from .database import Database

logger = logging.getLogger(__name__)

class ChannelsRepository:
    def __init__(self):
        self.db = Database()
        self.logger = logging.getLogger(__name__)

    def add_channel(self, channel: Channel) -> None:
        """
        Add a new channel to the repository.
        Raises exception if channel with same name already exists.
        """
        try:
            existing = self.db.query(
                Channel,
                name=channel.name
            )

            if existing:
                raise ValueError(f"Channel with name '{channel.name}' already exists")

            self.db.save(channel)
            logger.info(f"Added new channel: {channel.name}")

        except Exception as e:
            logger.error(f"Failed to add channel: {str(e)}")
            raise

    def remove_channel(self, channel_id: UUID) -> None:
        """
        Remove a channel from the repository.
        Raises exception if channel doesn't exist.
        """
        try:
            channel = self.db.retrieve(Channel, str(channel_id))
            if not channel:
                raise ValueError(f"Channel with ID '{channel_id}' not found")

            self.db.delete(Channel, str(channel_id))
            logger.info(f"Removed channel: {channel_id}")

        except Exception as e:
            logger.error(f"Failed to remove channel: {str(e)}")
            raise

    def get_channels(self) -> List[Channel]:
        """Get all channels from the repository."""
        try:
            channels = self.db.query(Channel)
            return channels

        except Exception as e:
            logger.error(f"Failed to get channels: {str(e)}")
            raise

    def get_channel(self, channel_id: UUID) -> Optional[Channel]:
        """Get a specific channel by ID."""
        try:
            channel = self.db.retrieve(Channel, str(channel_id))
            return channel

        except Exception as e:
            logger.error(f"Failed to get channel {channel_id}: {str(e)}")
            raise

    def update_channel(self, channel: Channel) -> None:
        """
        Update an existing channel.
        Raises exception if channel doesn't exist.
        """
        try:
            existing = self.db.retrieve(Channel, str(channel.id))
            if not existing:
                raise ValueError(f"Channel with ID '{channel.id}' not found")

            self.db.save(channel)
            logger.info(f"Updated channel: {channel.id}")

        except Exception as e:
            logger.error(f"Failed to update channel: {str(e)}")
            raise
