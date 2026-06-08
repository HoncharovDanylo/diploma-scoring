from __future__ import annotations
import unittest
from app.optimization.ga_selector import select_portfolio_ga

def _c(i: int, *, req: float, mn: float, profit: float, pd: str='0.050000') -> dict:
    sid = f'00000000-0000-0000-0000-{i:012d}'
    return {'applicationId': sid, 'requestedPrincipal': str(req), 'minPrincipal': str(mn), 'maxPrincipal': str(req), 'expectedProfit': str(profit), 'probabilityOfDefault': pd}

class TestContinuousGa(unittest.TestCase):

    def test_respects_budget(self) -> None:
        cands = [_c(1, req=8000, mn=1000, profit=400), _c(2, req=8000, mn=1000, profit=300)]
        rows, used, _ = select_portfolio_ga(cands, 9000, population_size=48, generations=40, crossover_rate=0.65, mutation_rate=0.12, elite_count=4, seed=42)
        self.assertLessEqual(used, 9000 + 0.02)
        self.assertGreater(len(rows), 0)
        for r in rows:
            self.assertGreaterEqual(float(r['allocatedPrincipal']), 1000)

    def test_can_allocate_partial(self) -> None:
        cands = [_c(1, req=10000, mn=500, profit=1000), _c(2, req=10000, mn=500, profit=900)]
        rows, used, profit = select_portfolio_ga(cands, 12000, population_size=64, generations=60, crossover_rate=0.65, mutation_rate=0.15, elite_count=4, seed=7)
        self.assertLessEqual(used, 12000 + 0.02)
        self.assertGreater(profit, 0)
        if len(rows) >= 2:
            self.assertLess(used + 0.01, 20000)

    def test_empty_candidates(self) -> None:
        rows, used, profit = select_portfolio_ga([], 1000, population_size=10, generations=5, crossover_rate=0.5, mutation_rate=0.1, elite_count=1, seed=1)
        self.assertEqual(rows, [])
        self.assertEqual(used, 0)
        self.assertEqual(profit, 0)
if __name__ == '__main__':
    unittest.main()
