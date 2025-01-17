from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.triggers.cron import CronTrigger
from datetime import datetime
import logging
from typing import List
from uuid import UUID

from .models import Settings, Channel, Digest
from .channels_repository import ChannelsRepository
from .digests_repository import DigestsRepository
from .channel_reader import ChannelReader
from .summary_generator import SummaryGenerator
from .email_service import EmailSender

logger = logging.getLogger(__name__)

class Scheduler:
    def __init__(self):
        self.scheduler = BackgroundScheduler()
        self.channels_repo = ChannelsRepository()
        self.digests_repo = DigestsRepository()
        self.channel_reader = ChannelReader()
        self.email_sender = EmailSender()

    def start(self, settings: Settings):
        """Start the scheduler with the given settings."""
        try:
            # Remove existing jobs
            self.scheduler.remove_all_jobs()

            # Schedule digest generation
            self.scheduler.add_job(
                self._generate_and_send_digests,
                CronTrigger(
                    hour=settings.digest_schedule_hour,
                    minute=settings.digest_schedule_minute
                ),
                args=[settings],
                id='digest_job',
                name='Generate and send digests',
                replace_existing=True
            )

            if not self.scheduler.running:
                self.scheduler.start()
                logger.info("Scheduler started successfully")

        except Exception as e:
            logger.error(f"Failed to start scheduler: {str(e)}")
            raise

    def stop(self):
        """Stop the scheduler."""
        try:
            if self.scheduler.running:
                self.scheduler.shutdown()
                logger.info("Scheduler stopped successfully")
        except Exception as e:
            logger.error(f"Failed to stop scheduler: {str(e)}")
            raise

    async def _generate_and_send_digests(self, settings: Settings):
        """Generate and send digests for all channels."""
        try:
            channels = self.channels_repo.get_channels()
            summary_generator = SummaryGenerator(settings.openai_api_key)

            for channel in channels:
                try:
                    # Generate digest
                    posts = self.channel_reader.get_channel_posts(
                        channel.id,
                        datetime.utcnow()
                    )

                    summaries = []
                    for post in posts:
                        summary = summary_generator.generate_summary(post)
                        summaries.append(summary)

                    digest = Digest(
                        channel_id=channel.id,
                        summaries=summaries
                    )

                    # Save digest
                    self.digests_repo.add_digest(digest)

                    # Send email
                    self.email_sender.send_digest(digest, settings)

                    logger.info(f"Successfully generated and sent digest for channel: {channel.name}")

                except Exception as e:
                    logger.error(f"Failed to process channel {channel.name}: {str(e)}")
                    continue

        except Exception as e:
            logger.error(f"Failed to generate and send digests: {str(e)}")
            raise
