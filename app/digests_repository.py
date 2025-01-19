import logging
from datetime import datetime
from typing import List, Optional
from uuid import UUID

from .database import Database
from .models import Digest, PostSummary

logger = logging.getLogger(__name__)


class DigestsRepository:
    def __init__(self):
        self.db = Database()
        self.logger = logging.getLogger(__name__)

    def add_digest(self, digest: Digest) -> None:
        """
        Add a new digest to the repository.
        """
        try:
            with self.db.transaction():  # New transaction context manager
                # Save the digest
                self.db.save(digest)

                # Save associated summaries
                for summary in digest.summaries:
                    self.db.save(summary)

            logger.info(f"Added new digest: {digest.id}")

        except Exception as e:
            logger.error(f"Failed to add digest: {str(e)}")
            raise

    def get_digest(self, digest_id: UUID) -> Optional[Digest]:
        """
        Get a specific digest by ID.
        Returns None if digest doesn't exist.
        """
        try:
            digest = self.db.retrieve(Digest, str(digest_id))
            if digest:
                # Load associated summaries
                summaries = self.db.query(PostSummary, digest_id=str(digest_id))
                digest.summaries = summaries
            return digest

        except Exception as e:
            logger.error(f"Failed to get digest {digest_id}: {str(e)}")
            raise

    def get_digests(self, from_date: datetime, to_date: datetime) -> List[Digest]:
        """
        Get all digests within the specified date range.
        """
        try:
            digests = self.db.query(
                Digest, created_date_gte=from_date, created_date_lte=to_date
            )

            # Load summaries for each digest
            for digest in digests:
                summaries = self.db.query(PostSummary, digest_id=str(digest.id))
                digest.summaries = summaries

            return digests

        except Exception as e:
            logger.error(f"Failed to get digests: {str(e)}")
            raise

    def delete_digest(self, digest_id: UUID) -> None:
        """
        Delete a digest and its associated summaries.
        Raises exception if digest doesn't exist.
        """
        try:
            digest = self.db.retrieve(Digest, str(digest_id))
            if not digest:
                raise ValueError(f"Digest with ID '{digest_id}' not found")

            # Delete associated summaries
            summaries = self.db.query(PostSummary, digest_id=str(digest_id))
            for summary in summaries:
                self.db.delete(PostSummary, str(summary.id))

            # Delete the digest
            self.db.delete(Digest, str(digest_id))
            logger.info(f"Deleted digest: {digest_id}")

        except Exception as e:
            logger.error(f"Failed to delete digest: {str(e)}")
            raise

    def get_channel_digests(
        self, channel_id: UUID, from_date: datetime, to_date: datetime
    ) -> List[Digest]:
        """
        Get all digests for a specific channel within the date range.
        """
        try:
            digests = self.db.query(
                Digest,
                channel_id=str(channel_id),
                created_date_gte=from_date,
                created_date_lte=to_date,
            )

            # Load summaries for each digest
            for digest in digests:
                summaries = self.db.query(PostSummary, digest_id=str(digest.id))
                digest.summaries = summaries

            return digests

        except Exception as e:
            logger.error(f"Failed to get channel digests: {str(e)}")
            raise
