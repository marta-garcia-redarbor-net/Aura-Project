using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IPriorityScoringService
{
    PriorityScore Score(EvaluationContext context);
}
