using System;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Navigation;

namespace WalletWasabi.Fluent.ViewModels.Wallets
{
	[NavigationMetaData(Title="Wallet Settings")]
	public partial class WalletSettingsViewModel : RoutableViewModel
	{
		[AutoNotify] private bool _preferPsbtWorkflow;

		public WalletSettingsViewModel(WalletViewModelBase walletViewModelBase)
		{
			var wallet = walletViewModelBase.Wallet;
			_preferPsbtWorkflow = wallet.KeyManager.PreferPsbtWorkflow;
			IsHardwareWallet = wallet.KeyManager.IsHardwareWallet;

			SetupCancel(enableCancel: false, enableCancelOnEscape: true, enableCancelOnPressed: true);

			NextCommand = CancelCommand;

			this.WhenAnyValue(x => x.PreferPsbtWorkflow)
				.Subscribe(value =>
				{
					wallet.KeyManager.PreferPsbtWorkflow = value;
					wallet.KeyManager.ToFile();
					walletViewModelBase.RaisePropertyChanged(nameof(walletViewModelBase.PreferPsbtWorkflow));
				});
		}

		public bool IsHardwareWallet { get; }
	}
}
