import logging
from datetime import datetime, timedelta

from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.triggers.cron import CronTrigger

from .channel_reader import ChannelReader
from .channels_repository import ChannelsRepository
from .digest_service import DigestService
from .email_sender import EmailSender
from .models import Settings

logger = logging.getLogger(__name__)


class Scheduler:
    def __init__(self) -> None:
        self.scheduler = BackgroundScheduler()
        self.channels_repo = ChannelsRepository()
        self.digest_service = DigestService()
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
        """Generate and send digest containing summaries from all channels."""
        try:
            # TODO: 00:00 bugs
            digest_date_time = datetime.now().replace(
                hour=settings.digest_schedule_hour,
                minute=settings.digest_schedule_minute,
                second=0,
                microsecond=0,
            )
            # Generate single digest for all channels
            digest = await self.digest_service.generate_digest(
                settings,
                from_date=digest_date_time - timedelta(days=1),
                to_date=digest_date_time,
            )

            # Send email
            self.digest_service.send_digest(digest.id, settings)

            logger.info("Successfully generated and sent digest")

        except Exception as e:
            logger.error(f"Failed to generate and send digest: {str(e)}")
            raise
