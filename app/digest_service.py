import logging
from datetime import datetime
from uuid import UUID

from .channel_reader import ChannelReader
from .digests_repository import DigestsRepository
from .email_sender import EmailSender
from .models import Digest, Settings
from .summary_generator import SummaryGenerator

logger = logging.getLogger(__name__)


class DigestService:
    def __init__(self) -> None:
        self.channel_reader = ChannelReader()
        self.digests_repo = DigestsRepository()
        self.email_sender = EmailSender()

    async def generate_digest(self, channel_id: UUID, settings: Settings) -> Digest:
        """Generate a digest for a specific channel."""
        try:
            summary_generator = SummaryGenerator(settings.openai_api_key)

            # Get posts
            posts = await self.channel_reader.get_channel_posts(
                channel_id, datetime.utcnow()
            )

            # Generate summaries
            summaries = []
            for post in posts:
                summary = summary_generator.generate_summary(post)
                summaries.append(summary)

            # Create and save digest
            digest = Digest(channel_id=channel_id, summaries=summaries)
            self.digests_repo.add_digest(digest)

            return digest

        except Exception as e:
            logger.error(
                f"Failed to generate digest for channel {channel_id}: {str(e)}"
            )
            raise

    def send_digest(self, digest_id: UUID, settings: Settings) -> None:
        """Send a digest via email."""
        try:
            digest = self.digests_repo.get_digest(digest_id)
            if not digest:
                raise ValueError("Digest not found")

            self.email_sender.send_digest(digest, settings)

        except Exception as e:
            logger.error(f"Failed to send digest {digest_id}: {str(e)}")
            raise
