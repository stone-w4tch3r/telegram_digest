import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from typing import Optional
import logging
from .models import Digest, Settings

logger = logging.getLogger(__name__)

class EmailSender:
    def __init__(self):
        self.logger = logging.getLogger(__name__)

    def send_digest(self, digest: Digest, settings: Settings) -> bool:
        """
        Send digest email with a link to the digest page.
        Returns True if email was sent successfully.
        """
        try:
            # Create message
            msg = MIMEMultipart()
            msg['From'] = settings.email_from
            msg['To'] = settings.email_to
            msg['Subject'] = f"Your Telegram Digest for {digest.created_date.strftime('%Y-%m-%d')}"

            # Create email body with digest link
            body = self._create_email_body(digest)
            msg.attach(MIMEText(body, 'html'))

            # Setup SMTP connection
            with smtplib.SMTP(settings.email_server, settings.email_port) as server:
                server.starttls()
                server.login(settings.email_from, settings.email_password)
                server.send_message(msg)

            logger.info(f"Successfully sent digest email for digest ID: {digest.id}")
            return True

        except Exception as e:
            logger.error(f"Failed to send digest email: {str(e)}")
            raise Exception(f"Failed to send email: {str(e)}")

    def _create_email_body(self, digest: Digest) -> str:
        """Create HTML email body with digest link."""
        return f"""
        <html>
            <body>
                <h2>Your Daily Telegram Digest</h2>
                <p>Your digest for {digest.created_date.strftime('%Y-%m-%d')} is ready!</p>
                <p>This digest contains {len(digest.summaries)} summaries.</p>
                <p>
                    <a href="/digest/{digest.id}" style="
                        background-color: #4CAF50;
                        border: none;
                        color: white;
                        padding: 15px 32px;
                        text-align: center;
                        text-decoration: none;
                        display: inline-block;
                        font-size: 16px;
                        margin: 4px 2px;
                        cursor: pointer;
                    ">
                        View Digest
                    </a>
                </p>
                <p>
                    <small>
                        This is an automated message from your Telegram Digest service.
                    </small>
                </p>
            </body>
        </html>
        """
