from fastapi import Request, Form
from fastapi.templating import Jinja2Templates
from fastapi.responses import HTMLResponse
from fastapi.staticfiles import StaticFiles
from typing import List
import httpx

from .models import Channel, Settings, DigestPreview, Digest
from .api import app

# Setup templates
templates = Jinja2Templates(directory="templates")
app.mount("/static", StaticFiles(directory="static"), name="static")

@app.get("/", response_class=HTMLResponse)
async def home(request: Request):
    """Render the home page."""
    async with httpx.AsyncClient() as client:
        response = await client.get(f"{request.base_url}api/channels")
        channels = response.json()

    return templates.TemplateResponse(
        "channels.html",
        {
            "request": request,
            "channels": channels,
            "active_page": "channels"
        }
    )

@app.get("/settings", response_class=HTMLResponse)
async def settings_page(request: Request):
    """Render the settings page."""
    async with httpx.AsyncClient() as client:
        response = await client.get(f"{request.base_url}api/settings")
        settings = response.json()

    return templates.TemplateResponse(
        "settings.html",
        {
            "request": request,
            "settings": settings,
            "active_page": "settings"
        }
    )

@app.get("/history", response_class=HTMLResponse)
async def digest_history(request: Request):
    """Render the digest history page."""
    async with httpx.AsyncClient() as client:
        response = await client.get(f"{request.base_url}api/digests")
        digests = response.json()

    return templates.TemplateResponse(
        "digest_history.html",
        {
            "request": request,
            "digests": digests,
            "active_page": "history"
        }
    )

@app.get("/digest/{digest_id}", response_class=HTMLResponse)
async def digest_page(request: Request, digest_id: str):
    """Render a specific digest page."""
    async with httpx.AsyncClient() as client:
        response = await client.get(f"{request.base_url}api/digests/{digest_id}")
        if response.status_code == 404:
            return templates.TemplateResponse(
                "error.html",
                {
                    "request": request,
                    "error_message": "Digest not found"
                }
            )
        digest = response.json()

    return templates.TemplateResponse(
        "digest_page.html",
        {
            "request": request,
            "digest": digest,
            "active_page": "digest"
        }
    )

@app.post("/channels/add")
async def add_channel_form(
    request: Request,
    channel_name: str = Form(...),
    channel_url: str = Form(...)
):
    """Handle channel addition form submission."""
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{request.base_url}api/channels",
            json={
                "name": channel_name,
                "url": channel_url
            }
        )

        if response.status_code != 200:
            return templates.TemplateResponse(
                "error.html",
                {
                    "request": request,
                    "error_message": "Failed to add channel"
                }
            )

    return RedirectResponse(url="/", status_code=303)

@app.post("/settings/update")
async def update_settings_form(
    request: Request,
    openai_api_key: str = Form(...),
    email_from: str = Form(...),
    email_to: str = Form(...),
    email_password: str = Form(...),
    email_server: str = Form(...),
    email_port: int = Form(...),
    digest_schedule_hour: int = Form(...),
    digest_schedule_minute: int = Form(...)
):
    """Handle settings update form submission."""
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{request.base_url}api/settings",
            json={
                "openai_api_key": openai_api_key,
                "email_from": email_from,
                "email_to": email_to,
                "email_password": email_password,
                "email_server": email_server,
                "email_port": email_port,
                "digest_schedule_hour": digest_schedule_hour,
                "digest_schedule_minute": digest_schedule_minute
            }
        )

        if response.status_code != 200:
            return templates.TemplateResponse(
                "error.html",
                {
                    "request": request,
                    "error_message": "Failed to update settings"
                }
            )

    return RedirectResponse(url="/settings", status_code=303)
