from pathlib import Path
from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict
_PACKAGE_ROOT = Path(__file__).resolve().parent.parent

class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file='.env', extra='ignore')
    rabbit_host: str = 'localhost'
    rabbit_port: int = 5672
    rabbit_user: str = 'guest'
    rabbit_password: str = 'guest'
    exchange: str = 'lending.events'
    origination_base_url: str = 'http://localhost:5002'
    portfolio_base_url: str = 'http://localhost:5003'
    internal_api_key: str = 'dev-internal-key-change-me'
    artifacts_dir: str = 'artifacts'
    scoring_pd_threshold: float = Field(default=0.22, ge=0.0, le=1.0)
    scoring_top_factors: int = Field(default=5, ge=1, le=20)
    ga_population_size: int = Field(default=80, ge=10)
    ga_generations: int = Field(default=120, ge=10)
    ga_crossover_rate: float = Field(default=0.88, ge=0.0, le=1.0)
    ga_mutation_rate: float = Field(default=0.012, ge=0.0, le=1.0)
    ga_elite_count: int = Field(default=2, ge=1)
    ga_seed: int = 42

    def resolved_artifacts_path(self) -> Path:
        p = Path(self.artifacts_dir)
        return p.resolve() if p.is_absolute() else (_PACKAGE_ROOT / p).resolve()
