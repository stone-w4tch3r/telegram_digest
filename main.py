import logging
import sys
from datetime import datetime
from pathlib import Path

import uvicorn
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles

from app.api import router as api_router
from app.database import Database
from app.logger import LoggerSetup
from app.scheduler import Scheduler
from app.settings import SettingsManager
from app.web_ui import router as web_router

# Create main FastAPI application
app = FastAPI(
    title="Telegram Digest",
    description="A service that creates daily digests from Telegram channels",
    version="1.0.0",
)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


def setup_logging():
    """Setup application logging."""
    log_dir = Path("logs")
    log_dir.mkdir(exist_ok=True)

    log_file = log_dir / f"telegram_digest_{datetime.now().strftime('%Y%m%d')}.log"
    LoggerSetup.setup_logger(log_file=str(log_file), log_level=logging.INFO)


def setup_static_files():
    """Setup static files serving."""
    static_dir = Path("static")
    static_dir.mkdir(exist_ok=True)
    app.mount("/static", StaticFiles(directory="static"), name="static")


def setup_routes():
    """Setup application routes."""
    app.include_router(api_router, prefix="/api")
    app.include_router(web_router)


def setup_database():
    """Initialize database."""
    try:
        Database()
        logging.info("Database initialized successfully")
    except Exception as e:
        logging.error(f"Failed to initialize database: {str(e)}")
        sys.exit(1)


def setup_scheduler():
    """Setup task scheduler."""
    try:
        settings = SettingsManager().load_settings()
        scheduler = Scheduler()
        scheduler.start(settings)
        logging.info("Scheduler started successfully")
    except Exception as e:
        logging.error(f"Failed to start scheduler: {str(e)}")
        # Continue without scheduler


def initialize_application():
    """Initialize all application components."""
    setup_logging()
    logging.info("Starting Telegram Digest application")

    setup_static_files()
    setup_routes()
    setup_database()
    setup_scheduler()

    logging.info("Application initialization completed")


@app.on_event("startup")
async def startup_event():
    """Handle application startup."""
    initialize_application()


@app.on_event("shutdown")
async def shutdown_event():
    """Handle application shutdown."""
    logging.info("Shutting down Telegram Digest application")
    try:
        scheduler = Scheduler()
        scheduler.stop()
        logging.info("Scheduler stopped successfully")
    except Exception as e:
        logging.error(f"Error during shutdown: {str(e)}")


# Health check endpoint
@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {
        "status": "healthy",
        "timestamp": datetime.utcnow().isoformat(),
        "version": app.version,
    }


if __name__ == "__main__":
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True, log_level="info")
