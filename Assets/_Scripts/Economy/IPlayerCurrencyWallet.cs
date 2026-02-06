using System;

namespace UltimateDungeon.Economy
{
    /// <summary>
    /// Read-only currency wallet for UI display.
    /// </summary>
    public interface IPlayerCurrencyWallet
    {
        int HeldCoins { get; }
        int BankedCoins { get; }

        event Action CurrencyChanged;
    }
}
