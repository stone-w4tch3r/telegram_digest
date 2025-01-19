import logging

from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.triggers.cron import CronTrigger

from .channel_reader import ChannelReader
from .channels_repository import ChannelsRepository
from .digest_service import DigestService
from .digests_repository import DigestsRepository
from .email_sender import EmailSender
from .models import Settings

logger = logging.getLogger(__name__)


class Scheduler:
    def __init__(self) -> None:
        self.scheduler = BackgroundScheduler()
        self.channels_repo = ChannelsRepository()
        self.digests_repo = DigestsRepository()
        self.channel_reader = ChannelReader()
        self.email_sender = EmailSender()

    def start(self, settings: Settings) -> None:
        """Start the scheduler with the given settings."""
        try:
            # Remove existing jobs
            self.scheduler.remove_all_jobs()

            # Schedule digest generation
            self.scheduler.add_job(
                self._generate_and_send_digests,
                CronTrigger(
                    hour=settings.digest_schedule_hour,
                    minute=settings.digest_schedule_minute,
                ),
                args=[settings],
                id="digest_job",
                name="Generate and send digests",
                replace_existing=True,
            )

            if not self.scheduler.running:
                self.scheduler.start()
                logger.info("Scheduler started successfully")

        except Exception as e:
            logger.error(f"Failed to start scheduler: {str(e)}")
            raise

    def stop(self) -> None:
        """Stop the scheduler."""
        try:
            if self.scheduler.running:
                self.scheduler.shutdown()
                logger.info("Scheduler stopped successfully")
        except Exception as e:
            logger.error(f"Failed to stop scheduler: {str(e)}")
            raise

    async def _generate_and_send_digests(self, settings: Settings) -> None:
        """Generate and send digests for all channels."""
        try:
            channels = self.channels_repo.get_channels()
            digest_service = DigestService()

            for channel in channels:
                try:
                    # Generate digest
                    digest = await digest_service.generate_digest(channel.id, settings)

                    # Send email
                    digest_service.send_digest(digest.id, settings)

                    logger.info(
                        f"Successfully generated and sent digest for channel: {channel.name}"
                    )

                except Exception as e:
                    logger.error(f"Failed to process channel {channel.name}: {str(e)}")
                    continue

        except Exception as e:
            logger.error(f"Failed to generate and send digests: {str(e)}")
            raise
