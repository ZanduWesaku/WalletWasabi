using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using DynamicData;
using DynamicData.Binding;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Gui;
using WalletWasabi.Logging;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Home.History
{
	public partial class HistoryViewModel : ActivatableViewModel
	{
		private readonly SourceList<HistoryItemViewModel> _transactionSourceList;
		private readonly WalletViewModel _walletViewModel;
		private readonly IObservable<Unit> _updateTrigger;
		private readonly ObservableCollectionExtended<HistoryItemViewModel> _transactions;
		private readonly ObservableCollectionExtended<HistoryItemViewModel> _unfilteredTransactions;
		private readonly object _updateLock = new();

		[AutoNotify] private bool _showCoinJoin;
		[AutoNotify] private HistoryItemViewModel? _selectedItem;


		public HistoryViewModel(WalletViewModel walletViewModel, UiConfig uiConfig, IObservable<Unit> updateTrigger)
		{
			_walletViewModel = walletViewModel;
			_updateTrigger = updateTrigger;
			_showCoinJoin = uiConfig.ShowCoinJoinInHistory;
			_transactionSourceList = new SourceList<HistoryItemViewModel>();
			_transactions = new ObservableCollectionExtended<HistoryItemViewModel>();
			_unfilteredTransactions = new ObservableCollectionExtended<HistoryItemViewModel>();

			this.WhenAnyValue(x => x.ShowCoinJoin)
				.Subscribe(showCoinJoin => uiConfig.ShowCoinJoinInHistory = showCoinJoin);

			var sortDescription = DataGridSortDescription.FromPath(nameof(HistoryItemViewModel.OrderIndex), ListSortDirection.Descending);
			CollectionView = new DataGridCollectionView(Transactions);
			CollectionView.SortDescriptions.Add(sortDescription);

			var coinJoinFilter = this.WhenAnyValue(x => x.ShowCoinJoin)
				.Select(CoinJoinFilter);

			_transactionSourceList
				.Connect()
				.ObserveOn(RxApp.MainThreadScheduler)
				.Sort(SortExpressionComparer<HistoryItemViewModel>.Descending(x => x.OrderIndex))
				.Bind(_unfilteredTransactions)
				.Filter(coinJoinFilter)
				.Bind(_transactions)
				.Subscribe();
		}

		public DataGridCollectionView CollectionView { get; }

		public ObservableCollection<HistoryItemViewModel> UnfilteredTransactions => _unfilteredTransactions;

		public ObservableCollection<HistoryItemViewModel> Transactions => _transactions;

		protected override void OnActivated(CompositeDisposable disposables)
		{
			base.OnActivated(disposables);

			RxApp.MainThreadScheduler.Schedule(async () =>
			{
				await UpdateAsync();

				_transactionSourceList
					.Connect()
					.Throttle(TimeSpan.FromMilliseconds(100))
					.Skip(1)
					.OnItemAdded(_ =>
					{
						Console.WriteLine("Item added");
						var newPendingItem = Transactions.OrderByDescending(x => x.OrderIndex).FirstOrDefault(x => !x.IsConfirmed);
						SelectedItem = newPendingItem;
					})
					.Subscribe()
					.DisposeWith(disposables);
			});

			_updateTrigger
				.Subscribe(async _ => await UpdateAsync())
				.DisposeWith(disposables);

			disposables.Add(Disposable.Create(() => _transactionSourceList.Clear()));
		}

		private static Func<HistoryItemViewModel, bool> CoinJoinFilter(bool showCoinJoin)
		{
			return item =>
			{
				if (showCoinJoin)
				{
					return true;
				}

				return !item.IsCoinJoin;
			};
		}

		private async Task UpdateAsync()
		{
			try
			{
				var historyBuilder = new TransactionHistoryBuilder(_walletViewModel.Wallet);
				var txRecordList = await Task.Run(historyBuilder.BuildHistorySummary);

				lock (_updateLock)
				{
					_transactionSourceList.Clear();

					Money balance = Money.Zero;
					for (var i = 0; i < txRecordList.Count; i++)
					{
						var transactionSummary = txRecordList[i];
						balance += transactionSummary.Amount;
						_transactionSourceList.Add(new HistoryItemViewModel(i, transactionSummary, _walletViewModel, balance, _updateTrigger));
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(ex);
			}
		}
	}
}
