import os
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from typing import List

import aiosmtplib
from jinja2 import Environment, FileSystemLoader

from .database import Summary

SMTP_HOST = os.getenv("SMTP_HOST")
SMTP_PORT = int(os.getenv("SMTP_PORT", 587))
SMTP_USER = os.getenv("SMTP_USER")
SMTP_PASSWORD = os.getenv("SMTP_PASSWORD")
EMAIL_RECIPIENT = os.getenv("EMAIL_RECIPIENT")
BASE_URL = os.getenv("BASE_URL")

env = Environment(loader=FileSystemLoader("app/templates"))


async def send_email(summaries: List[Summary]):
    template = env.get_template("summary_email.html")
    html_content = template.render(summaries=summaries, base_url=BASE_URL)

    message = MIMEMultipart("alternative")
    message["From"] = SMTP_USER
    message["To"] = EMAIL_RECIPIENT
    message["Subject"] = "Daily Telegram Digest"

    part = MIMEText(html_content, "html")
    message.attach(part)

    await aiosmtplib.send(
        message,
        hostname=SMTP_HOST,
        port=SMTP_PORT,
        username=SMTP_USER,
        password=SMTP_PASSWORD,
        start_tls=True,
    )
