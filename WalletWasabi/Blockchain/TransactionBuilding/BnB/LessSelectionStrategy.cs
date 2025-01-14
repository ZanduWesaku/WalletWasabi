namespace WalletWasabi.Blockchain.TransactionBuilding.BnB;

/// <summary>
/// Strategy that searches search-space and caches every found selection so that we have
/// a best selection that is as near as possible to the given target with minimization of
/// fee costs.
/// </summary>
public class LessSelectionStrategy : SelectionStrategy
{
	/// <summary>Payments are capped to be at most 25% lower than the original target.</summary>
	public const double MinPaymentThreshold = 0.75;

	/// <inheritdoc/>
	public LessSelectionStrategy(long target, long[] inputValues, long[] inputCosts)
		: base(target, inputValues, inputCosts, new CoinSelection(long.MinValue, long.MinValue))
	{
		MinimumTarget = (long)(target * MinPaymentThreshold);
	}

	/// <summary>Minimum acceptable target (inclusive).</summary>
	/// <seealso cref="SelectionStrategy.Target"/>
	public long MinimumTarget { get; }

	public override EvaluationResult Evaluate(long[] selection, int depth, long sum)
	{
		long totalCost = sum + CurrentInputCosts;

		if (sum > Target)
		{
			// Excessive funds, cut the branch.
			return EvaluationResult.SkipBranch;
		}

		long maximumSum = sum + RemainingAmounts[depth - 1];

		if (maximumSum < BestSelection.PaymentAmount || maximumSum < MinimumTarget)
		{
			// The remaining coins cannot sum up to our best solution, or it is less than minimum acceptable target value.
			return EvaluationResult.SkipBranch;
		}

		if (sum > BestSelection.PaymentAmount || (sum == BestSelection.PaymentAmount && totalCost < BestSelection.TotalCosts))
		{
			BestSelection.Update(sum, totalCost, selection[0..depth]);
		}

		if (depth == selection.Length)
		{
			// Leaf reached, no match
			return EvaluationResult.SkipBranch;
		}

		return EvaluationResult.Continue;
	}
}
