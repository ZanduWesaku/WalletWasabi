using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WalletWasabi.Helpers;

namespace WalletWasabi.Models.TransactionBuilding
{
	public class PaymentIntent
	{
		public IEnumerable<DestinationRequest> Requests { get; }
		public ChangeStrategy ChangeStrategy { get; }
		public Money TotalAmount { get; }
		public int Count { get; }

		public PaymentIntent(Script scriptPubKey, Money amount, string label = "") : this(scriptPubKey, MoneyRequest.Create(amount), label)
		{
		}

		public PaymentIntent(Script scriptPubKey, MoneyRequest amount, string label = "") : this(scriptPubKey.GetDestination(), amount, label)
		{
		}

		public PaymentIntent(IDestination destination, Money amount, string label = "") : this(destination, MoneyRequest.Create(amount), label)
		{
		}

		public PaymentIntent(IDestination destination, MoneyRequest amount, string label = "") : this(new DestinationRequest(destination, amount, label))
		{
		}

		public PaymentIntent(params DestinationRequest[] requests) : this(requests as IEnumerable<DestinationRequest>)
		{
		}

		public PaymentIntent(IEnumerable<DestinationRequest> requests)
		{
			Guard.NotNullOrEmpty(nameof(requests), requests);
			foreach (var request in requests)
			{
				Guard.NotNull(nameof(request), request);
			}

			var allSpendCount = requests.Count(x => x.Amount.Type == MoneyRequestType.AllRemaining);
			if (allSpendCount == 0)
			{
				ChangeStrategy = ChangeStrategy.Auto;
			}
			else if (allSpendCount == 1)
			{
				ChangeStrategy = ChangeStrategy.Custom;
			}
			else
			{
				throw new ArgumentException($"Only one request can contain an all remaining money request.");
			}

			Requests = requests;

			TotalAmount = requests.Where(x => x.Amount.Type == MoneyRequestType.Value).Sum(x => x.Amount.Amount);
			Count = requests.Count();
		}

		public DestinationRequest GetCustomChange()
		{
			if (ChangeStrategy != ChangeStrategy.Custom)
			{
				throw new InvalidOperationException($"{nameof(ChangeStrategy)} must be {ChangeStrategy.Custom}");
			}
			else
			{
				return Requests.Single(x => x.Amount.Type == MoneyRequestType.AllRemaining);
			}
		}
	}
}
