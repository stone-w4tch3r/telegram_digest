import logging
from datetime import datetime
from uuid import UUID

from .channel_reader import ChannelReader
from .channels_repository import ChannelsRepository
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
        self.channels_repo = ChannelsRepository()

    async def generate_digest(
        self,
        settings: Settings,
        from_date: datetime,
        to_date: datetime,
    ) -> Digest:
        """Generate a digest containing summaries from all channels."""
        try:
            summary_generator = SummaryGenerator(settings.openai_api_key)
            channels = self.channels_repo.get_channels()
            all_summaries = []

            # Get posts and generate summaries for each channel
            for channel in channels:
                try:
                    posts = await self.channel_reader.get_channel_posts(
                        channel.id, from_date, to_date
                    )

                    # Generate summaries for this channel's posts
                    for post in posts:
                        summary = summary_generator.generate_summary(post)
                        all_summaries.append(summary)

                except Exception as e:
                    logger.error(f"Failed to process channel {channel.name}: {str(e)}")
                    continue

            # Create and save digest with all summaries
            digest = Digest(summaries=all_summaries)
            self.digests_repo.add_digest(digest)

            return digest

        except Exception as e:
            logger.error(f"Failed to generate digest: {str(e)}")
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
