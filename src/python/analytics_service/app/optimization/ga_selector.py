from __future__ import annotations
from typing import Any
import numpy as np

def _repair_feasible(x: np.ndarray, min_p: np.ndarray, max_p: np.ndarray, budget: float) -> np.ndarray:
    x = np.clip(np.asarray(x, dtype=np.float64), 0.0, max_p)
    for _ in range(250):
        total = float(x.sum())
        if total <= budget + 1e-09:
            mask = (x > 1e-12) & (x + 1e-12 < min_p)
            x[mask] = 0.0
            if float(x.sum()) <= budget + 1e-09:
                return x
            continue
        if total < 1e-12:
            return x
        x = x * (budget / total)
        x = np.clip(x, 0.0, max_p)
    return np.clip(x, 0.0, max_p)

def _marginal_g(profit_full: float, cap: float) -> float:
    if cap <= 1e-12:
        return 0.0
    return float(profit_full) / float(cap)

def _finalize_amounts(x: np.ndarray, min_p: np.ndarray, max_p: np.ndarray, budget: float) -> np.ndarray:
    x = np.clip(x, 0.0, max_p)
    x = np.array([round(float(v), 2) for v in x], dtype=np.float64)
    mask = (x > 0) & (x + 1e-09 < min_p)
    x[mask] = 0.0
    total = float(x.sum())
    if total > budget + 1e-09 and total > 1e-12:
        x = np.array([round(float(v * budget / total), 2) for v in x], dtype=np.float64)
        mask = (x > 0) & (x + 1e-09 < min_p)
        x[mask] = 0.0
    guard = 0
    while float(x.sum()) > budget + 0.001 and guard < 10000:
        idx = int(np.argmax(x))
        if x[idx] <= 0:
            break
        x[idx] = round(max(0.0, float(x[idx]) - 0.01), 2)
        guard += 1
    return x

def _greedy_seed(g: np.ndarray, min_p: np.ndarray, max_p: np.ndarray, budget: float, rng: np.random.Generator) -> np.ndarray:
    n = len(g)
    x = np.zeros(n, dtype=np.float64)
    order = np.argsort(-g)
    rem = float(budget)
    for i in order:
        lo, hi = (float(min_p[i]), float(max_p[i]))
        if hi < 1e-12:
            continue
        take = min(hi, rem)
        if take + 1e-12 >= lo:
            x[i] = take if rng.random() > 0.25 else float(rng.uniform(lo, take))
            rem = float(budget - x.sum())
        elif rem + 1e-12 >= lo:
            x[i] = lo
            rem = float(budget - x.sum())
        if rem < 1e-12:
            break
    return _repair_feasible(x, min_p, max_p, budget)

def _random_seed(min_p: np.ndarray, max_p: np.ndarray, budget: float, rng: np.random.Generator) -> np.ndarray:
    x = rng.uniform(0.0, 1.0, size=len(min_p)) * max_p
    return _repair_feasible(x, min_p, max_p, budget)

def _fitness(x: np.ndarray, g: np.ndarray, min_p: np.ndarray, max_p: np.ndarray, budget: float) -> float:
    x = _repair_feasible(x, min_p, max_p, budget)
    return float((x * g).sum())

def select_portfolio_ga(candidates: list[dict[str, Any]], budget_cap: float, *, population_size: int, generations: int, crossover_rate: float, mutation_rate: float, elite_count: int, seed: int) -> tuple[list[dict[str, Any]], float, float]:
    n = len(candidates)
    if n == 0 or budget_cap <= 0:
        return ([], 0.0, 0.0)
    rng = np.random.default_rng(seed)
    max_p = np.array([float(c.get('maxPrincipal', c.get('requestedPrincipal', 0)) or c.get('requestedPrincipal', 0) or 0) for c in candidates], dtype=np.float64)
    min_p = np.array([float(c.get('minPrincipal', 0) or 0) for c in candidates], dtype=np.float64)
    min_p = np.minimum(min_p, max_p)
    profits_full = np.array([float(c.get('expectedProfit', 0) or 0) for c in candidates], dtype=np.float64)
    g = np.array([_marginal_g(profits_full[i], max_p[i]) for i in range(n)], dtype=np.float64)
    pop: list[np.ndarray] = []
    half = max(1, population_size // 2)
    for _ in range(half):
        pop.append(_greedy_seed(g, min_p, max_p, budget_cap, rng))
    while len(pop) < population_size:
        pop.append(_random_seed(min_p, max_p, budget_cap, rng))
    pop_arr = np.stack(pop)

    def scores_for(pa: np.ndarray) -> np.ndarray:
        return np.array([_fitness(pa[i], g, min_p, max_p, budget_cap) for i in range(len(pa))])
    fit = scores_for(pop_arr)
    best_idx = int(np.argmax(fit))
    best_x = _repair_feasible(pop_arr[best_idx].copy(), min_p, max_p, budget_cap)
    best_score = float((best_x * g).sum())
    elite_n = max(1, min(elite_count, population_size // 2))
    for _ in range(generations):
        elite_idx = np.argsort(-fit)[:elite_n]
        new_pop: list[np.ndarray] = [pop_arr[i].copy() for i in elite_idx]
        while len(new_pop) < population_size:
            a = int(rng.choice(len(pop_arr)))
            b = int(rng.choice(len(pop_arr)))
            p1, p2 = (pop_arr[a], pop_arr[b])
            if rng.random() < crossover_rate:
                child = np.where(rng.random(n) < 0.5, p1, p2).astype(np.float64)
            else:
                child = p1.copy()
            mut = rng.random(n) < mutation_rate
            noise = rng.normal(0.0, 0.12, n) * np.maximum(max_p, 1e-06)
            child = np.where(mut, child + noise, child)
            new_pop.append(child)
        pop_arr = np.stack(new_pop)
        fit = scores_for(pop_arr)
        gen_best = int(np.argmax(fit))
        cand = _repair_feasible(pop_arr[gen_best].copy(), min_p, max_p, budget_cap)
        cand_score = float((cand * g).sum())
        if cand_score > best_score + 1e-09:
            best_x = cand
            best_score = cand_score
    best_x = _finalize_amounts(best_x, min_p, max_p, budget_cap)
    used = float(best_x.sum())
    profit = float((best_x * g).sum())
    rows: list[dict[str, Any]] = []
    for i in range(n):
        amt = float(best_x[i])
        if amt < float(min_p[i]) - 1e-09 or amt < 1e-09:
            continue
        c = candidates[i]
        exp_profit = profits_full[i] * (amt / max_p[i]) if max_p[i] > 1e-12 else 0.0
        rows.append({'applicationId': c['applicationId'], 'allocatedPrincipal': round(amt, 2), 'expectedProfitAllocated': round(exp_profit, 2), 'probabilityOfDefault': c.get('probabilityOfDefault')})
    return (rows, used, profit)
