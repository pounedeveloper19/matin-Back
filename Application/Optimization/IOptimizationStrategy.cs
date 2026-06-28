namespace MatinPower.Server.Application.Optimization;

/// <summary>
/// Optimization strategy contract.
/// Implementations: GreedyOptimizationStrategy (Phase 3A), LinearProgrammingStrategy (Phase 3B).
/// </summary>
public interface IOptimizationStrategy
{
    OptimizationResult Optimize(EnergyOptimizationInput input);
}
